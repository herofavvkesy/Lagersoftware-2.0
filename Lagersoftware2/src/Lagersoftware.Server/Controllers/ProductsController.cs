using Lagersoftware.Server.Data;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Server.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var products = await _db.Products
            .Where(p => !p.IsDeleted)
            .Include(p => p.Category)
            .Include(p => p.StorageLocation)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var p = await _db.Products.Include(p => p.Category).Include(p => p.StorageLocation)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode)
    {
        var p = await _db.Products.Include(p => p.Category).Include(p => p.StorageLocation)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Barcode = dto.Barcode,
            Quantity = dto.Quantity,
            MinQuantity = dto.MinQuantity,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            StorageLocationId = dto.StorageLocationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || product.IsDeleted) return NotFound();

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Barcode = dto.Barcode;
        product.Quantity = dto.Quantity;
        product.MinQuantity = dto.MinQuantity;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;
        product.StorageLocationId = dto.StorageLocationId;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToDto(product));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null || product.IsDeleted) return NotFound();
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Barcode = p.Barcode,
        Quantity = p.Quantity,
        MinQuantity = p.MinQuantity,
        Price = p.Price,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        StorageLocationId = p.StorageLocationId,
        StorageLocationName = p.StorageLocation?.Name,
        UpdatedAt = p.UpdatedAt
    };
}
