using MediatR;

namespace OilTrading.Application.Commands.Users;

public class ChangePasswordCommand : IRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}