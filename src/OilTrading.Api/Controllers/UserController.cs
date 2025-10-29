using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/legacy-users")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(ApplicationDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 180, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "id" })]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null || !user.IsActive)
        {
            return NotFound();
        }

        return user;
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserDto dto)
    {
        var user = new User
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = HashPassword(dto.Password), // Simple placeholder
            Role = dto.Role,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.Role = dto.Role;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = false; // Soft delete
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private string HashPassword(string password)
    {
        // Simple hash for demo - in production use proper hashing like BCrypt
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

public class UpdateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}