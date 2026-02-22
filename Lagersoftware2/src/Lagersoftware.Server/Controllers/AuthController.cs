using Lagersoftware.Server.Data;
using Lagersoftware.Server.Services;
using Lagersoftware.Shared.DTOs;
using Lagersoftware.Shared.Enums;
using Lagersoftware.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagersoftware.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, TokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Ungültige Anmeldedaten" });

        var token = _tokenService.GenerateToken(user);
        return Ok(new LoginResponseDto
        {
            Token = token,
            Username = user.Username,
            Role = user.Role.ToString(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
    }

    [HttpPost("register")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username && !u.IsDeleted))
            return Conflict(new { message = "Benutzername bereits vergeben" });

        var role = Enum.TryParse<UserRole>(request.Role, true, out var r) ? r : UserRole.User;
        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new UserDto { Id = user.Id, Username = user.Username, Role = user.Role.ToString(), CreatedAt = user.CreatedAt });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _db.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto { Id = u.Id, Username = u.Username, Role = u.Role.ToString(), CreatedAt = u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null || user.IsDeleted) return NotFound();
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("support-reset")]
    public async Task<IActionResult> SupportReset([FromBody] SupportResetRequestDto request)
    {
        var supportPassword = _config["SupportPassword"] ?? "support123!";
        if (request.SupportPassword != supportPassword)
            return Unauthorized(new { message = "Falsches Support-Passwort" });

        var users = await _db.Users.Where(u => !u.IsDeleted).ToListAsync();
        foreach (var user in users)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            user.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{users.Count} Passwörter zurückgesetzt" });
    }
}
