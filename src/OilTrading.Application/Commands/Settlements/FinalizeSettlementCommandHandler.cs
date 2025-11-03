using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command Handler for finalizing settlements
/// Routes to appropriate service based on settlement type
/// </summary>
public class FinalizeSettlementCommandHandler : IRequestHandler<FinalizeSettlementCommand, Unit>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public FinalizeSettlementCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
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

            return Unit.Value;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
