using MediatR;

namespace OilTrading.Application.Commands.Settlements;

/// <summary>
/// CQRS Command to calculate settlement amounts
/// Calculates benchmark amount, adjustment amount, cargo value, and total settlement amount
/// Works for both purchase and sales settlements via generic handler
/// </summary>
public class CalculateSettlementCommand : IRequest<Unit>
{
    public Guid SettlementId { get; set; }
    public bool IsPurchaseSettlement { get; set; } = true;
    public decimal CalculationQuantityMT { get; set; }
    public decimal CalculationQuantityBBL { get; set; }
    public decimal BenchmarkAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string CalculationNote { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = "System";
}
