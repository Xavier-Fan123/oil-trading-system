using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;
using OilTrading.Application.Commands.Settlements;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// Query to get all charges for a settlement
/// </summary>
public class GetSettlementChargesQuery : IRequest<List<SettlementChargeDto>>
{
    public Guid SettlementId { get; set; }
    public bool IsPurchaseSettlement { get; set; } = true;
}

/// <summary>
/// Handler for GetSettlementChargesQuery
/// </summary>
public class GetSettlementChargesQueryHandler : IRequestHandler<GetSettlementChargesQuery, List<SettlementChargeDto>>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public GetSettlementChargesQueryHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<List<SettlementChargeDto>> Handle(GetSettlementChargesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Route to appropriate service based on settlement type
            var chargesDto = request.IsPurchaseSettlement
                ? await _purchaseSettlementService.GetChargesAsync(request.SettlementId, cancellationToken)
                : await _salesSettlementService.GetChargesAsync(request.SettlementId, cancellationToken);

            return chargesDto;
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }
}
