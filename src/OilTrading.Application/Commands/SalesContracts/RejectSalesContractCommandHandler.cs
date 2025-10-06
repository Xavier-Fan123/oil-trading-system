using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.ValueObjects;
using OilTrading.Core.Entities;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.SalesContracts;

public class RejectSalesContractCommandHandler : IRequestHandler<RejectSalesContractCommand>
{
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ILogger<RejectSalesContractCommandHandler> _logger;

    public RejectSalesContractCommandHandler(
        ISalesContractRepository salesContractRepository,
        ILogger<RejectSalesContractCommandHandler> logger)
    {
        _salesContractRepository = salesContractRepository;
        _logger = logger;
    }

    public async Task Handle(RejectSalesContractCommand request, CancellationToken cancellationToken)
    {
        var salesContract = await _salesContractRepository.GetByIdAsync(request.Id, cancellationToken);
        if (salesContract == null)
        {
            throw new NotFoundException($"Sales contract with ID {request.Id} not found");
        }

        // Use the domain method to handle rejection
        var rejectionReason = string.IsNullOrEmpty(request.Comments) 
            ? request.Reason 
            : $"{request.Reason} - {request.Comments}";
            
        salesContract.Reject(rejectionReason);

        await _salesContractRepository.UpdateAsync(salesContract, cancellationToken);

        _logger.LogInformation("Sales contract {ContractId} rejected by {RejectedBy}. Reason: {Reason}", 
            request.Id, request.RejectedBy, request.Reason);
    }
}