using MediatR;
using OilTrading.Core.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace OilTrading.Application.Commands.Users;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{request.UserId}' not found");
        }

        // Verify current password
        var currentPasswordHash = HashPassword(request.CurrentPassword);
        if (user.PasswordHash != currentPasswordHash)
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Update to new password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.SetUpdatedBy(request.UpdatedBy);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string HashPassword(string password)
    {
        // Simple hash for demo - in production use proper hashing like BCrypt
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}