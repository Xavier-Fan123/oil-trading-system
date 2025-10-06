using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.Users;
using OilTrading.Application.Queries.Users;
using OilTrading.Application.DTOs;
using OilTrading.Application.Common;
using OilTrading.Core.Entities;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private string GetCurrentUserName()
    {
        try
        {
            return User?.Identity?.Name ?? 
                   HttpContext?.User?.Identity?.Name ?? 
                   "System";
        }
        catch
        {
            return "System";
        }
    }

    /// <summary>
    /// Gets a paginated list of users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var command = new CreateUserCommand
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Password = dto.Password,
            Role = dto.Role,
            CreatedBy = GetCurrentUserName()
        };

        var userId = await _mediator.Send(command);
        
        _logger.LogInformation("User {UserId} created successfully", userId);
        
        return CreatedAtAction(nameof(GetUser), new { id = userId }, userId);
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var command = new UpdateUserCommand
        {
            Id = id,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role,
            IsActive = dto.IsActive,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("User {UserId} updated successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var command = new DeleteUserCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("User {UserId} deleted successfully", id);
        
        return NoContent();
    }

    /// <summary>
    /// Changes user password
    /// </summary>
    [HttpPost("{id:guid}/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        var command = new ChangePasswordCommand
        {
            UserId = id,
            CurrentPassword = dto.CurrentPassword,
            NewPassword = dto.NewPassword,
            UpdatedBy = GetCurrentUserName()
        };

        await _mediator.Send(command);
        
        _logger.LogInformation("Password changed for user {UserId}", id);
        
        return NoContent();
    }

    /// <summary>
    /// Gets users by role
    /// </summary>
    [HttpGet("by-role/{role}")]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersByRole(UserRole role)
    {
        var query = new GetUsersQuery { Role = role, IsActive = true };
        var result = await _mediator.Send(query);
        return Ok(result.Items);
    }

    /// <summary>
    /// Gets all active traders
    /// </summary>
    [HttpGet("traders")]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTraders()
    {
        var query = new GetUsersQuery { Role = UserRole.Trader, IsActive = true };
        var result = await _mediator.Send(query);
        return Ok(result.Items);
    }
}