using Lagersoftware.Server.Data;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Server.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _db.Categories
            .Where(c => !c.IsDeleted)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, UpdatedAt = c.UpdatedAt })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null || c.IsDeleted) return NotFound();
        return Ok(new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, UpdatedAt = c.UpdatedAt });
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var category = new Category { Name = dto.Name, Description = dto.Description, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, UpdatedAt = category.UpdatedAt });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] CreateCategoryDto dto)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null || category.IsDeleted) return NotFound();
        category.Name = dto.Name;
        category.Description = dto.Description;
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description, UpdatedAt = category.UpdatedAt });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null || category.IsDeleted) return NotFound();
        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
