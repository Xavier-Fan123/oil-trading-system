using MediatR;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Exceptions;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// CQRS Query Handler for retrieving a settlement by ID
/// Routes to appropriate service based on settlement type
/// </summary>
public class GetSettlementByIdQueryHandler : IRequestHandler<GetSettlementByIdQuery, SettlementDto?>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public GetSettlementByIdQueryHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<SettlementDto?> Handle(GetSettlementByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.IsPurchaseSettlement)
        {
            var settlement = await _purchaseSettlementService.GetSettlementAsync(request.SettlementId, cancellationToken);
            return settlement == null ? null : MapToDto(settlement);
        }
        else
        {
            var settlement = await _salesSettlementService.GetSettlementAsync(request.SettlementId, cancellationToken);
            return settlement == null ? null : MapToDto(settlement);
        }
    }

    private static SettlementDto MapToDto(dynamic settlement)
    {
        return new SettlementDto
        {
            Id = settlement.Id,
            ContractId = settlement.PurchaseContractId ?? settlement.SalesContractId,
            ContractNumber = settlement.ContractNumber,
            ExternalContractNumber = settlement.ExternalContractNumber,
            DocumentNumber = settlement.DocumentNumber,
            DocumentType = settlement.DocumentType,
            DocumentDate = settlement.DocumentDate,
            ActualQuantityMT = settlement.ActualQuantityMT,
            ActualQuantityBBL = settlement.ActualQuantityBBL,
            CalculationQuantityMT = settlement.CalculationQuantityMT,
            CalculationQuantityBBL = settlement.CalculationQuantityBBL,
            BenchmarkPrice = settlement.BenchmarkPrice,
            BenchmarkAmount = settlement.BenchmarkAmount,
            AdjustmentAmount = settlement.AdjustmentAmount,
            CargoValue = settlement.CargoValue,
            TotalCharges = settlement.TotalCharges,
            TotalSettlementAmount = settlement.TotalSettlementAmount,
            Status = settlement.Status,
            IsFinalized = settlement.IsFinalized,
            CreatedDate = settlement.CreatedDate,
            CreatedBy = settlement.CreatedBy,
            LastModifiedDate = settlement.LastModifiedDate,
            LastModifiedBy = settlement.LastModifiedBy,
            ChargeCount = settlement.Charges?.Count ?? 0
        };
    }
}
