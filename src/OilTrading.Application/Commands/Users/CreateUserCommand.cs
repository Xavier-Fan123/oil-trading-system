using MediatR;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.Users;

public class CreateUserCommand : IRequest<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}