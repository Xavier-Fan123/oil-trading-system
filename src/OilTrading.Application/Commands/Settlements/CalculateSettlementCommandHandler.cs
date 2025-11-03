using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command Handler for calculating settlements
/// Routes to appropriate service based on settlement type
/// </summary>
public class CalculateSettlementCommandHandler : IRequestHandler<CalculateSettlementCommand, Unit>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public CalculateSettlementCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<Unit> Handle(CalculateSettlementCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.IsPurchaseSettlement)
            {
                await _purchaseSettlementService.CalculateSettlementAsync(
                    request.SettlementId,
                    request.CalculationQuantityMT,
                    request.CalculationQuantityBBL,
                    request.BenchmarkAmount,
                    request.AdjustmentAmount,
                    request.CalculationNote,
                    request.UpdatedBy,
                    cancellationToken);
            }
            else
            {
                await _salesSettlementService.CalculateSettlementAsync(
                    request.SettlementId,
                    request.CalculationQuantityMT,
                    request.CalculationQuantityBBL,
                    request.BenchmarkAmount,
                    request.AdjustmentAmount,
                    request.CalculationNote,
                    request.UpdatedBy,
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
