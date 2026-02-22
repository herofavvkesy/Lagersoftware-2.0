using Lagersoftware.Server.Data;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Server.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _db;
    public SyncController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<SyncResponseDto>> Sync([FromBody] SyncRequestDto request)
    {
        var syncedAt = DateTime.UtcNow;
        int conflictsResolved = 0;

        // Process client product changes
        foreach (var clientProduct in request.ChangedProducts)
        {
            var serverProduct = await _db.Products.FindAsync(clientProduct.Id);
            if (serverProduct == null) continue;

            if (clientProduct.UpdatedAt > serverProduct.UpdatedAt)
            {
                serverProduct.Name = clientProduct.Name;
                serverProduct.Description = clientProduct.Description;
                serverProduct.Barcode = clientProduct.Barcode;
                serverProduct.Quantity = clientProduct.Quantity;
                serverProduct.MinQuantity = clientProduct.MinQuantity;
                serverProduct.Price = clientProduct.Price;
                serverProduct.CategoryId = clientProduct.CategoryId;
                serverProduct.StorageLocationId = clientProduct.StorageLocationId;
                serverProduct.UpdatedAt = clientProduct.UpdatedAt;
            }
            else
            {
                conflictsResolved++;
            }
        }

        // Process client category changes
        foreach (var clientCat in request.ChangedCategories)
        {
            var serverCat = await _db.Categories.FindAsync(clientCat.Id);
            if (serverCat == null) continue;
            if (clientCat.UpdatedAt > serverCat.UpdatedAt)
            {
                serverCat.Name = clientCat.Name;
                serverCat.Description = clientCat.Description;
                serverCat.UpdatedAt = clientCat.UpdatedAt;
            }
            else conflictsResolved++;
        }

        // Process client storage location changes
        foreach (var clientLoc in request.ChangedStorageLocations)
        {
            var serverLoc = await _db.StorageLocations.FindAsync(clientLoc.Id);
            if (serverLoc == null) continue;
            if (clientLoc.UpdatedAt > serverLoc.UpdatedAt)
            {
                serverLoc.Name = clientLoc.Name;
                serverLoc.Description = clientLoc.Description;
                serverLoc.UpdatedAt = clientLoc.UpdatedAt;
            }
            else conflictsResolved++;
        }

        // Add new movements from client
        foreach (var movement in request.NewMovements)
        {
            if (!await _db.StockMovements.AnyAsync(m => m.Id == movement.Id))
            {
                var product = await _db.Products.FindAsync(movement.ProductId);
                if (product != null)
                {
                    var newMovement = new StockMovement
                    {
                        ProductId = movement.ProductId,
                        MovementType = movement.MovementType,
                        Quantity = movement.Quantity,
                        QuantityBefore = movement.QuantityBefore,
                        QuantityAfter = movement.QuantityAfter,
                        Note = movement.Note,
                        UserId = movement.Username,
                        Username = movement.Username,
                        CreatedAt = movement.CreatedAt,
                        UpdatedAt = movement.CreatedAt
                    };
                    _db.StockMovements.Add(newMovement);
                }
            }
        }

        await _db.SaveChangesAsync();

        // Return server changes since last sync
        var updatedProducts = await _db.Products
            .Where(p => p.UpdatedAt > request.LastSyncAt)
            .Include(p => p.Category)
            .Include(p => p.StorageLocation)
            .Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, Description = p.Description, Barcode = p.Barcode,
                Quantity = p.Quantity, MinQuantity = p.MinQuantity, Price = p.Price,
                CategoryId = p.CategoryId, CategoryName = p.Category != null ? p.Category.Name : null,
                StorageLocationId = p.StorageLocationId, StorageLocationName = p.StorageLocation != null ? p.StorageLocation.Name : null,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        var updatedCategories = await _db.Categories
            .Where(c => c.UpdatedAt > request.LastSyncAt)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, UpdatedAt = c.UpdatedAt })
            .ToListAsync();

        var updatedLocations = await _db.StorageLocations
            .Where(l => l.UpdatedAt > request.LastSyncAt)
            .Select(l => new StorageLocationDto { Id = l.Id, Name = l.Name, Description = l.Description, UpdatedAt = l.UpdatedAt })
            .ToListAsync();

        var newMovements = await _db.StockMovements
            .Where(m => m.CreatedAt > request.LastSyncAt)
            .Include(m => m.Product)
            .Select(m => new StockMovementDto
            {
                Id = m.Id, ProductId = m.ProductId, ProductName = m.Product != null ? m.Product.Name : string.Empty,
                MovementType = m.MovementType, MovementTypeName = m.MovementType.ToString(),
                Quantity = m.Quantity, QuantityBefore = m.QuantityBefore, QuantityAfter = m.QuantityAfter,
                Note = m.Note, Username = m.Username, CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(new SyncResponseDto
        {
            SyncedAt = syncedAt,
            UpdatedProducts = updatedProducts,
            UpdatedCategories = updatedCategories,
            UpdatedStorageLocations = updatedLocations,
            NewMovements = newMovements,
            ConflictsResolved = conflictsResolved
        });
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);

        var totalProducts = await _db.Products.CountAsync(p => !p.IsDeleted);
        var lowStock = await _db.Products.CountAsync(p => !p.IsDeleted && p.Quantity <= p.MinQuantity);
        var totalCategories = await _db.Categories.CountAsync(c => !c.IsDeleted);
        var totalLocations = await _db.StorageLocations.CountAsync(l => !l.IsDeleted);
        var movementsToday = await _db.StockMovements.CountAsync(m => m.CreatedAt >= todayStart);
        var movementsWeek = await _db.StockMovements.CountAsync(m => m.CreatedAt >= weekStart);

        var recentMovements = await _db.StockMovements
            .Include(m => m.Product)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .Select(m => new StockMovementDto
            {
                Id = m.Id, ProductId = m.ProductId, ProductName = m.Product != null ? m.Product.Name : string.Empty,
                MovementType = m.MovementType, MovementTypeName = m.MovementType.ToString(),
                Quantity = m.Quantity, QuantityBefore = m.QuantityBefore, QuantityAfter = m.QuantityAfter,
                Note = m.Note, Username = m.Username, CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(new DashboardDto
        {
            TotalProducts = totalProducts,
            LowStockProducts = lowStock,
            TotalCategories = totalCategories,
            TotalStorageLocations = totalLocations,
            TotalMovementsToday = movementsToday,
            TotalMovementsThisWeek = movementsWeek,
            RecentMovements = recentMovements
        });
    }

    [HttpGet("speicherdaten")]
    public async Task<ActionResult<SpeicherDatenDto>> ExportSpeicherDaten()
    {
        var products = await _db.Products
            .Include(p => p.Category).Include(p => p.StorageLocation)
            .Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, Description = p.Description, Barcode = p.Barcode,
                Quantity = p.Quantity, MinQuantity = p.MinQuantity, Price = p.Price,
                CategoryId = p.CategoryId, CategoryName = p.Category != null ? p.Category.Name : null,
                StorageLocationId = p.StorageLocationId, StorageLocationName = p.StorageLocation != null ? p.StorageLocation.Name : null,
                UpdatedAt = p.UpdatedAt
            }).ToListAsync();

        var categories = await _db.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, UpdatedAt = c.UpdatedAt })
            .ToListAsync();

        var locations = await _db.StorageLocations
            .Select(l => new StorageLocationDto { Id = l.Id, Name = l.Name, Description = l.Description, UpdatedAt = l.UpdatedAt })
            .ToListAsync();

        var movements = await _db.StockMovements
            .Include(m => m.Product)
            .Select(m => new StockMovementDto
            {
                Id = m.Id, ProductId = m.ProductId, ProductName = m.Product != null ? m.Product.Name : string.Empty,
                MovementType = m.MovementType, MovementTypeName = m.MovementType.ToString(),
                Quantity = m.Quantity, QuantityBefore = m.QuantityBefore, QuantityAfter = m.QuantityAfter,
                Note = m.Note, Username = m.Username, CreatedAt = m.CreatedAt
            }).ToListAsync();

        var users = await _db.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto { Id = u.Id, Username = u.Username, Role = u.Role.ToString(), CreatedAt = u.CreatedAt })
            .ToListAsync();

        return Ok(new SpeicherDatenDto
        {
            ExportedAt = DateTime.UtcNow,
            Products = products,
            Categories = categories,
            StorageLocations = locations,
            StockMovements = movements,
            Users = users
        });
    }

    [HttpPost("speicherdaten")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ImportSpeicherDaten([FromBody] SpeicherDatenDto data)
    {
        // Import categories
        foreach (var catDto in data.Categories)
        {
            var existing = await _db.Categories.FindAsync(catDto.Id);
            if (existing == null)
                _db.Categories.Add(new Category { Id = catDto.Id, Name = catDto.Name, Description = catDto.Description, UpdatedAt = catDto.UpdatedAt, CreatedAt = catDto.UpdatedAt });
            else if (catDto.UpdatedAt > existing.UpdatedAt)
            { existing.Name = catDto.Name; existing.Description = catDto.Description; existing.UpdatedAt = catDto.UpdatedAt; }
        }

        // Import storage locations
        foreach (var locDto in data.StorageLocations)
        {
            var existing = await _db.StorageLocations.FindAsync(locDto.Id);
            if (existing == null)
                _db.StorageLocations.Add(new StorageLocation { Id = locDto.Id, Name = locDto.Name, Description = locDto.Description, UpdatedAt = locDto.UpdatedAt, CreatedAt = locDto.UpdatedAt });
            else if (locDto.UpdatedAt > existing.UpdatedAt)
            { existing.Name = locDto.Name; existing.Description = locDto.Description; existing.UpdatedAt = locDto.UpdatedAt; }
        }

        await _db.SaveChangesAsync();

        // Import products
        foreach (var prodDto in data.Products)
        {
            var existing = await _db.Products.FindAsync(prodDto.Id);
            if (existing == null)
                _db.Products.Add(new Product { Id = prodDto.Id, Name = prodDto.Name, Description = prodDto.Description, Barcode = prodDto.Barcode, Quantity = prodDto.Quantity, MinQuantity = prodDto.MinQuantity, Price = prodDto.Price, CategoryId = prodDto.CategoryId, StorageLocationId = prodDto.StorageLocationId, UpdatedAt = prodDto.UpdatedAt, CreatedAt = prodDto.UpdatedAt });
            else if (prodDto.UpdatedAt > existing.UpdatedAt)
            { existing.Name = prodDto.Name; existing.Description = prodDto.Description; existing.Barcode = prodDto.Barcode; existing.Quantity = prodDto.Quantity; existing.MinQuantity = prodDto.MinQuantity; existing.Price = prodDto.Price; existing.CategoryId = prodDto.CategoryId; existing.StorageLocationId = prodDto.StorageLocationId; existing.UpdatedAt = prodDto.UpdatedAt; }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Import erfolgreich" });
    }
}
