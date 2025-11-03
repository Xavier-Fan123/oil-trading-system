using MediatR;
using OilTrading.Application.Services;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command Handler for creating sales settlements
/// Coordinates with SalesSettlementService for business logic
/// </summary>
public class CreateSalesSettlementCommandHandler : IRequestHandler<CreateSalesSettlementCommand, Guid>
{
    private readonly SalesSettlementService _settlementService;
    private readonly IRepository<OilTrading.Core.Entities.SalesContract> _contractRepository;

    public CreateSalesSettlementCommandHandler(
        SalesSettlementService settlementService,
        IRepository<OilTrading.Core.Entities.SalesContract> contractRepository)
    {
        _settlementService = settlementService ?? throw new ArgumentNullException(nameof(settlementService));
        _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
    }

    public async Task<Guid> Handle(CreateSalesSettlementCommand request, CancellationToken cancellationToken)
    {
        // Validate contract exists
        var contract = await _contractRepository.GetByIdAsync(request.SalesContractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Sales contract with ID {request.SalesContractId} not found");

        // Create settlement through service
        var settlement = await _settlementService.CreateSettlementAsync(
            request.SalesContractId,
            request.ExternalContractNumber,
            request.DocumentNumber,
            request.DocumentType,
            request.DocumentDate,
            request.CreatedBy,
            cancellationToken);

        return settlement.Id;
    }
}
