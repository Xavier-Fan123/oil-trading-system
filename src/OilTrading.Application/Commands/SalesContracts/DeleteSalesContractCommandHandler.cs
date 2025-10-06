using MediatR;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class DeleteSalesContractCommandHandler : IRequestHandler<DeleteSalesContractCommand>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork)
    {
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSalesContractCommand request, CancellationToken cancellationToken)
    {
        var salesContract = await _salesContractRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (salesContract == null)
        {
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");
        }

        // Business rule: Only draft contracts can be deleted
        if (salesContract.Status != Core.Entities.ContractStatus.Draft)
        {
            throw new BusinessRuleException("Only draft sales contracts can be deleted");
        }

        // Perform soft delete by setting IsDeleted flag if the entity supports it
        // Or hard delete if business rules allow
        await _salesContractRepository.DeleteAsync(salesContract);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}