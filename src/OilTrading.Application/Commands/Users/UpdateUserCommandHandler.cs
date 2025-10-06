using MediatR;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Commands.Users;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{request.Id}' not found");
        }

        // Check if email is being changed and if new email already exists
        if (user.Email != request.Email.ToLower())
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != request.Id)
            {
                throw new InvalidOperationException($"User with email '{request.Email}' already exists");
            }
        }

        user.Email = request.Email.ToLower();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.SetUpdatedBy(request.UpdatedBy);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}