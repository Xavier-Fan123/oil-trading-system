using MediatR;

namespace OilTrading.Application.Commands.Users;

public class DeleteUserCommand : IRequest
{
    public Guid Id { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}