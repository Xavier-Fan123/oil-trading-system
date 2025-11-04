using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.EventHandlers;
using OilTrading.Core.Events;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command Handler for finalizing settlements
/// Routes to appropriate service based on settlement type
/// Also handles domain event processing when settlement is finalized
/// </summary>
public class FinalizeSettlementCommandHandler : IRequestHandler<FinalizeSettlementCommand, Unit>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;
    private readonly ContractSettlementFinalizedEventHandler _settlementEventHandler;

    public FinalizeSettlementCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService,
        ContractSettlementFinalizedEventHandler settlementEventHandler)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
        _settlementEventHandler = settlementEventHandler ?? throw new ArgumentNullException(nameof(settlementEventHandler));
    }

    public async Task<Unit> Handle(FinalizeSettlementCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.IsPurchaseSettlement)
            {
                await _purchaseSettlementService.FinalizeSettlementAsync(
                    request.SettlementId,
                    request.FinalizedBy,
                    cancellationToken);
            }
            else
            {
                await _salesSettlementService.FinalizeSettlementAsync(
                    request.SettlementId,
                    request.FinalizedBy,
                    cancellationToken);
            }

            // Handle domain event: ContractSettlementFinalizedEvent
            // This updates the contract's payment status and possibly completes it
            var settlementFinalizedEvent = new ContractSettlementFinalizedEvent(
                request.SettlementId,
                0,  // Total amount - will be fetched from settlement repository in event handler
                DateTime.UtcNow);

            await _settlementEventHandler.HandleSettlementFinalizedAsync(
                settlementFinalizedEvent,
                cancellationToken);

            return Unit.Value;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
