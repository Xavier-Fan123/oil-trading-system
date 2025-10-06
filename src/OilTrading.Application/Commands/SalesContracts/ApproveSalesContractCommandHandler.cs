using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class ApproveSalesContractCommandHandler : IRequestHandler<ApproveSalesContractCommand>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ILogger<ApproveSalesContractCommandHandler> _logger;

    public ApproveSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        ILogger<ApproveSalesContractCommandHandler> logger)
    {
        _salesContractRepository = salesContractRepository;
        _logger = logger;
    }

    public async Task Handle(ApproveSalesContractCommand request, CancellationToken cancellationToken)
    {
        var salesContract = await _salesContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (salesContract == null)
        {
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");
        }

        // Check if contract is in a state that can be approved
        if (salesContract.Status != ContractStatus.PendingApproval && salesContract.Status != ContractStatus.Draft)
        {
            throw new InvalidOperationException($"Sales contract with ID {request.Id} cannot be approved from {salesContract.Status} status");
        }

        // Activate the contract (this will handle status transition and validation)
        salesContract.Activate();

        await _salesContractRepository.UpdateAsync(salesContract, cancellationToken);

        _logger.LogInformation("Sales contract {ContractId} approved by {ApprovedBy}", 
            request.Id, request.ApprovedBy);
    }
}