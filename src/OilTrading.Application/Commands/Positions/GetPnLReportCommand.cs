using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Positions;

public class GetPnLReportCommand : IRequest<PnLReportDto>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? AsOfDate { get; set; }
    public string[]? ProductTypes { get; set; }
    public string[]? Counterparties { get; set; }
    public string[]? ContractTypes { get; set; } // "Purchase", "Sales", "Physical", "Paper"
    public PnLReportGroupBy GroupBy { get; set; } = PnLReportGroupBy.Product;
    public bool IncludeRealized { get; set; } = true;
    public bool IncludeUnrealized { get; set; } = true;
    public bool IncludeDetailBreakdown { get; set; } = false;
}

public enum PnLReportGroupBy
{
    Product,
    Counterparty,
    ContractType,
    Month,
    Trader
}

public class PnLReportDto
{
    public DateTime ReportDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public PnLReportSummaryDto Summary { get; set; } = new();
    public IEnumerable<PnLGroupDto> Groups { get; set; } = new List<PnLGroupDto>();
    public IEnumerable<PnLDto> Details { get; set; } = new List<PnLDto>();
    public PnLReportGroupBy GroupedBy { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public TimeSpan GenerationTime { get; set; }
}

public class PnLReportSummaryDto
{
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalRealizedPnL { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal LargestGain { get; set; }
    public decimal LargestLoss { get; set; }
    public int TotalContracts { get; set; }
    public int ProfitableContracts { get; set; }
    public int LossContracts { get; set; }
    public decimal AveragePnL { get; set; }
    public string Currency { get; set; } = "USD";
}

public class PnLGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty;
    public decimal TotalPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public int ContractCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentMarketPrice { get; set; }
    public string Currency { get; set; } = "USD";
}