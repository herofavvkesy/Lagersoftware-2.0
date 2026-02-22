using Lagersoftware.Server.Data;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Server.Controllers;

[ApiController]
[Route("api/storagelocations")]
[Authorize]
public class StorageLocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StorageLocationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<StorageLocationDto>>> GetStorageLocations()
    {
        var locations = await _db.StorageLocations
            .Where(l => !l.IsDeleted)
            .Select(l => new StorageLocationDto { Id = l.Id, Name = l.Name, Description = l.Description, UpdatedAt = l.UpdatedAt })
            .ToListAsync();
        return Ok(locations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StorageLocationDto>> GetStorageLocation(int id)
    {
        var l = await _db.StorageLocations.FindAsync(id);
        if (l == null || l.IsDeleted) return NotFound();
        return Ok(new StorageLocationDto { Id = l.Id, Name = l.Name, Description = l.Description, UpdatedAt = l.UpdatedAt });
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<StorageLocationDto>> CreateStorageLocation([FromBody] CreateStorageLocationDto dto)
    {
        var location = new StorageLocation { Name = dto.Name, Description = dto.Description, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.StorageLocations.Add(location);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetStorageLocation), new { id = location.Id }, new StorageLocationDto { Id = location.Id, Name = location.Name, Description = location.Description, UpdatedAt = location.UpdatedAt });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<StorageLocationDto>> UpdateStorageLocation(int id, [FromBody] CreateStorageLocationDto dto)
    {
        var location = await _db.StorageLocations.FindAsync(id);
        if (location == null || location.IsDeleted) return NotFound();
        location.Name = dto.Name;
        location.Description = dto.Description;
        location.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new StorageLocationDto { Id = location.Id, Name = location.Name, Description = location.Description, UpdatedAt = location.UpdatedAt });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteStorageLocation(int id)
    {
        var location = await _db.StorageLocations.FindAsync(id);
        if (location == null || location.IsDeleted) return NotFound();
        location.IsDeleted = true;
        location.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
