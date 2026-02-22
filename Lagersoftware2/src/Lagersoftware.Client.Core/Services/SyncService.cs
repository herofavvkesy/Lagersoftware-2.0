using Lagersoftware.Client.Core.Data;
using Lagersoftware.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Client.Core.Services;

public class SyncService
{
    private readonly LocalDbContext _localDb;
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private DateTime _lastSyncAt = DateTime.MinValue;
    private System.Timers.Timer? _syncTimer;

    public event EventHandler<SyncEventArgs>? SyncCompleted;
    public event EventHandler<bool>? OnlineStatusChanged;

    public bool IsOnline { get; private set; }
    public DateTime LastSyncAt => _lastSyncAt;

    public SyncService(LocalDbContext localDb, ApiService apiService, SettingsService settingsService)
    {
        _localDb = localDb;
        _apiService = apiService;
        _settingsService = settingsService;
    }

    public void StartAutoSync()
    {
        var intervalMs = _settingsService.Settings.SyncIntervalMinutes * 60 * 1000;
        _syncTimer = new System.Timers.Timer(intervalMs);
        _syncTimer.Elapsed += async (s, e) => await TrySyncAsync();
        _syncTimer.AutoReset = true;
        _syncTimer.Start();
    }

    public void StopAutoSync()
    {
        _syncTimer?.Stop();
        _syncTimer?.Dispose();
        _syncTimer = null;
    }

    public async Task<SyncResult> TrySyncAsync()
    {
        var reachable = await _apiService.IsServerReachableAsync();
        if (reachable != IsOnline)
        {
            IsOnline = reachable;
            OnlineStatusChanged?.Invoke(this, IsOnline);
        }

        if (!IsOnline) return new SyncResult { Success = false, Message = "Server nicht erreichbar" };

        try
        {
            // Gather local changes since last sync
            var changedProducts = await _localDb.Products
                .Where(p => p.UpdatedAt > _lastSyncAt)
                .Include(p => p.Category)
                .Include(p => p.StorageLocation)
                .Select(p => new ProductDto
                {
                    Id = p.Id, Name = p.Name, Description = p.Description, Barcode = p.Barcode,
                    Quantity = p.Quantity, MinQuantity = p.MinQuantity, Price = p.Price,
                    CategoryId = p.CategoryId, StorageLocationId = p.StorageLocationId, UpdatedAt = p.UpdatedAt
                }).ToListAsync();

            var changedCategories = await _localDb.Categories
                .Where(c => c.UpdatedAt > _lastSyncAt)
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, UpdatedAt = c.UpdatedAt })
                .ToListAsync();

            var changedLocations = await _localDb.StorageLocations
                .Where(l => l.UpdatedAt > _lastSyncAt)
                .Select(l => new StorageLocationDto { Id = l.Id, Name = l.Name, Description = l.Description, UpdatedAt = l.UpdatedAt })
                .ToListAsync();

            var newMovements = await _localDb.StockMovements
                .Where(m => m.CreatedAt > _lastSyncAt)
                .Include(m => m.Product)
                .Select(m => new StockMovementDto
                {
                    Id = m.Id, ProductId = m.ProductId, ProductName = m.Product != null ? m.Product.Name : string.Empty,
                    MovementType = m.MovementType, Quantity = m.Quantity,
                    QuantityBefore = m.QuantityBefore, QuantityAfter = m.QuantityAfter,
                    Note = m.Note, Username = m.Username, CreatedAt = m.CreatedAt
                }).ToListAsync();

            var request = new SyncRequestDto
            {
                LastSyncAt = _lastSyncAt,
                ChangedProducts = changedProducts,
                ChangedCategories = changedCategories,
                ChangedStorageLocations = changedLocations,
                NewMovements = newMovements
            };

            var response = await _apiService.SyncAsync(request);
            if (response == null) return new SyncResult { Success = false, Message = "Sync fehlgeschlagen" };

            // Apply server changes to local DB
            await ApplyServerChangesAsync(response);
            _lastSyncAt = response.SyncedAt;

            var result = new SyncResult
            {
                Success = true,
                Message = $"Sync erfolgreich. {response.ConflictsResolved} Konflikte gelöst.",
                SyncedAt = response.SyncedAt,
                ConflictsResolved = response.ConflictsResolved
            };
            SyncCompleted?.Invoke(this, new SyncEventArgs(result));
            return result;
        }
        catch (Exception ex)
        {
            return new SyncResult { Success = false, Message = $"Sync-Fehler: {ex.Message}" };
        }
    }

    private async Task ApplyServerChangesAsync(SyncResponseDto response)
    {
        foreach (var prodDto in response.UpdatedProducts)
        {
            var existing = await _localDb.Products.FindAsync(prodDto.Id);
            if (existing == null)
            {
                _localDb.Products.Add(new Shared.Models.Product
                {
                    Id = prodDto.Id, Name = prodDto.Name, Description = prodDto.Description,
                    Barcode = prodDto.Barcode, Quantity = prodDto.Quantity, MinQuantity = prodDto.MinQuantity,
                    Price = prodDto.Price, CategoryId = prodDto.CategoryId, StorageLocationId = prodDto.StorageLocationId,
                    UpdatedAt = prodDto.UpdatedAt, CreatedAt = prodDto.UpdatedAt
                });
            }
            else if (prodDto.UpdatedAt > existing.UpdatedAt)
            {
                existing.Name = prodDto.Name; existing.Description = prodDto.Description;
                existing.Barcode = prodDto.Barcode; existing.Quantity = prodDto.Quantity;
                existing.MinQuantity = prodDto.MinQuantity; existing.Price = prodDto.Price;
                existing.CategoryId = prodDto.CategoryId; existing.StorageLocationId = prodDto.StorageLocationId;
                existing.UpdatedAt = prodDto.UpdatedAt;
            }
        }

        foreach (var catDto in response.UpdatedCategories)
        {
            var existing = await _localDb.Categories.FindAsync(catDto.Id);
            if (existing == null)
                _localDb.Categories.Add(new Shared.Models.Category { Id = catDto.Id, Name = catDto.Name, Description = catDto.Description, UpdatedAt = catDto.UpdatedAt, CreatedAt = catDto.UpdatedAt });
            else if (catDto.UpdatedAt > existing.UpdatedAt)
            { existing.Name = catDto.Name; existing.Description = catDto.Description; existing.UpdatedAt = catDto.UpdatedAt; }
        }

        foreach (var locDto in response.UpdatedStorageLocations)
        {
            var existing = await _localDb.StorageLocations.FindAsync(locDto.Id);
            if (existing == null)
                _localDb.StorageLocations.Add(new Shared.Models.StorageLocation { Id = locDto.Id, Name = locDto.Name, Description = locDto.Description, UpdatedAt = locDto.UpdatedAt, CreatedAt = locDto.UpdatedAt });
            else if (locDto.UpdatedAt > existing.UpdatedAt)
            { existing.Name = locDto.Name; existing.Description = locDto.Description; existing.UpdatedAt = locDto.UpdatedAt; }
        }

        foreach (var movDto in response.NewMovements)
        {
            if (!await _localDb.StockMovements.AnyAsync(m => m.Id == movDto.Id))
            {
                _localDb.StockMovements.Add(new Shared.Models.StockMovement
                {
                    Id = movDto.Id, ProductId = movDto.ProductId, MovementType = movDto.MovementType,
                    Quantity = movDto.Quantity, QuantityBefore = movDto.QuantityBefore, QuantityAfter = movDto.QuantityAfter,
                    Note = movDto.Note, Username = movDto.Username, CreatedAt = movDto.CreatedAt, UpdatedAt = movDto.CreatedAt
                });
            }
        }

        await _localDb.SaveChangesAsync();
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
    public int ConflictsResolved { get; set; }
}

public class SyncEventArgs : EventArgs
{
    public SyncResult Result { get; }
    public SyncEventArgs(SyncResult result) => Result = result;
}
