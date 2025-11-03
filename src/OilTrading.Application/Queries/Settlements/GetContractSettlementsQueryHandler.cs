using MediatR;
using OilTrading.Application.Services;

namespace OilTrading.Application.Queries.Settlements;

/// <summary>
/// CQRS Query Handler for retrieving all settlements for a contract
/// Supports one-to-many relationship
/// </summary>
public class GetContractSettlementsQueryHandler : IRequestHandler<GetContractSettlementsQuery, List<SettlementDto>>
{
    private readonly PurchaseSettlementService _purchaseSettlementService;
    private readonly SalesSettlementService _salesSettlementService;

    public GetContractSettlementsQueryHandler(
        PurchaseSettlementService purchaseSettlementService,
        SalesSettlementService salesSettlementService)
    {
        _purchaseSettlementService = purchaseSettlementService ?? throw new ArgumentNullException(nameof(purchaseSettlementService));
        _salesSettlementService = salesSettlementService ?? throw new ArgumentNullException(nameof(salesSettlementService));
    }

    public async Task<List<SettlementDto>> Handle(GetContractSettlementsQuery request, CancellationToken cancellationToken)
    {
        if (request.IsPurchaseSettlement)
        {
            var settlements = await _purchaseSettlementService.GetContractSettlementsAsync(request.ContractId, cancellationToken);
            return settlements.Select(MapToDto).ToList();
        }
        else
        {
            var settlements = await _salesSettlementService.GetContractSettlementsAsync(request.ContractId, cancellationToken);
            return settlements.Select(MapToDto).ToList();
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
