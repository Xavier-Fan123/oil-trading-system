using MediatR;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Commands.Users;

public class UpdateUserCommand : IRequest
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string UpdatedBy { get; set; } = string.Empty;
}