using MediatR;
using OilTrading.Application.Services;
using OilTrading.Core.Repositories;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command Handler for creating purchase settlements
/// Coordinates with PurchaseSettlementService for business logic
/// </summary>
public class CreatePurchaseSettlementCommandHandler : IRequestHandler<CreatePurchaseSettlementCommand, Guid>
{
    private readonly PurchaseSettlementService _settlementService;
    private readonly IRepository<OilTrading.Core.Entities.PurchaseContract> _contractRepository;

    public CreatePurchaseSettlementCommandHandler(
        PurchaseSettlementService settlementService,
        IRepository<OilTrading.Core.Entities.PurchaseContract> contractRepository)
    {
        _settlementService = settlementService ?? throw new ArgumentNullException(nameof(settlementService));
        _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
    }

    public async Task<Guid> Handle(CreatePurchaseSettlementCommand request, CancellationToken cancellationToken)
    {
        // Validate contract exists
        var contract = await _contractRepository.GetByIdAsync(request.PurchaseContractId, cancellationToken);
        if (contract == null)
            throw new NotFoundException($"Purchase contract with ID {request.PurchaseContractId} not found");

        // Create settlement through service
        var settlement = await _settlementService.CreateSettlementAsync(
            request.PurchaseContractId,
            request.ExternalContractNumber,
            request.DocumentNumber,
            request.DocumentType,
            request.DocumentDate,
            request.CreatedBy,
            cancellationToken);

        return settlement.Id;
    }
}
