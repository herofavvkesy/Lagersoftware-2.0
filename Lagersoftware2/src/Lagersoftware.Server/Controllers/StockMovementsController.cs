using System.Security.Claims;
using Lagersoftware.Server.Data;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Enums;
using Lagersoftware.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Server.Controllers;

[ApiController]
[Route("api/stockmovements")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StockMovementsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<StockMovementDto>>> GetMovements([FromQuery] int? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.StockMovements.Include(m => m.Product).AsQueryable();
        if (productId.HasValue) query = query.Where(m => m.ProductId == productId);

        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ToDto(m))
            .ToListAsync();
        return Ok(movements);
    }

    [HttpPost]
    public async Task<ActionResult<StockMovementDto>> CreateMovement([FromBody] CreateStockMovementDto dto)
    {
        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product == null || product.IsDeleted) return NotFound(new { message = "Produkt nicht gefunden" });

        var quantityBefore = product.Quantity;
        int quantityAfter;

        switch (dto.MovementType)
        {
            case MovementType.Wareneingang:
                quantityAfter = quantityBefore + dto.Quantity;
                break;
            case MovementType.Warenausgang:
                if (quantityBefore < dto.Quantity)
                    return BadRequest(new { message = "Nicht genug Bestand vorhanden" });
                quantityAfter = quantityBefore - dto.Quantity;
                break;
            case MovementType.Bestandskorrektur:
                quantityAfter = dto.Quantity;
                break;
            default:
                return BadRequest(new { message = "Unbekannter Bewegungstyp" });
        }

        product.Quantity = quantityAfter;
        product.UpdatedAt = DateTime.UtcNow;

        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

        var movement = new StockMovement
        {
            ProductId = dto.ProductId,
            MovementType = dto.MovementType,
            Quantity = dto.MovementType == MovementType.Bestandskorrektur ? quantityAfter - quantityBefore : dto.Quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            Note = dto.Note,
            UserId = userId,
            Username = username,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMovements), new { id = movement.Id }, ToDto(movement));
    }

    private static StockMovementDto ToDto(StockMovement m) => new()
    {
        Id = m.Id,
        ProductId = m.ProductId,
        ProductName = m.Product?.Name ?? string.Empty,
        MovementType = m.MovementType,
        MovementTypeName = m.MovementType.ToString(),
        Quantity = m.Quantity,
        QuantityBefore = m.QuantityBefore,
        QuantityAfter = m.QuantityAfter,
        Note = m.Note,
        Username = m.Username,
        CreatedAt = m.CreatedAt
    };
}
