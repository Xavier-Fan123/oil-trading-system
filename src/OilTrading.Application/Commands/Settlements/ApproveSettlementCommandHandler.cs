using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command Handler for approving settlements
/// Routes to appropriate service based on settlement type
/// </summary>
public class ApproveSettlementCommandHandler : IRequestHandler<ApproveSettlementCommand, Unit>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public ApproveSettlementCommandHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<Unit> Handle(ApproveSettlementCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.IsPurchaseSettlement)
            {
                await _purchaseSettlementService.ApproveSettlementAsync(
                    request.SettlementId,
                    request.ApprovedBy,
                    cancellationToken);
            }
            else
            {
                await _salesSettlementService.ApproveSettlementAsync(
                    request.SettlementId,
                    request.ApprovedBy,
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
