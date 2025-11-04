using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// Command to remove a charge from a settlement
/// </summary>
public class RemoveChargeCommand : IRequest<Unit>
{
    public Guid SettlementId { get; set; }
    public Guid ChargeId { get; set; }
    public string RemovedBy { get; set; } = "System";
    public bool IsPurchaseSettlement { get; set; } = true;
}

/// <summary>
/// Handler for RemoveChargeCommand
/// </summary>
public class RemoveChargeCommandHandler : IRequestHandler<RemoveChargeCommand, Unit>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public RemoveChargeCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<Unit> Handle(RemoveChargeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Route to appropriate service based on settlement type
            if (request.IsPurchaseSettlement)
            {
                await _purchaseSettlementService.RemoveChargeAsync(
                    request.SettlementId,
                    request.ChargeId,
                    request.RemovedBy,
                    cancellationToken);
            }
            else
            {
                await _salesSettlementService.RemoveChargeAsync(
                    request.SettlementId,
                    request.ChargeId,
                    request.RemovedBy,
                    cancellationToken);
            }

            return Unit.Value;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
