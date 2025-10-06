using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Positions;

public class GetPositionSnapshotCommand : IRequest<PositionSnapshotDto>
{
    public DateTime? AsOfDate { get; set; }
    public string[]? ProductTypes { get; set; }
    public string[]? Counterparties { get; set; }
    public bool IncludeBreakdown { get; set; } = true;
    public bool IncludePnL { get; set; } = true;
    public bool IncludeExposure { get; set; } = true;
}

public class PositionSnapshotDto
{
    public DateTime SnapshotDate { get; set; }
    public PositionSummaryDto Summary { get; set; } = new();
    public IEnumerable<NetPositionDto> Positions { get; set; } = new List<NetPositionDto>();
    public IEnumerable<PnLDto> PnLDetails { get; set; } = new List<PnLDto>();
    public IEnumerable<ExposureDto> ProductExposure { get; set; } = new List<ExposureDto>();
    public IEnumerable<ExposureDto> CounterpartyExposure { get; set; } = new List<ExposureDto>();
    public PositionLimitsDto Limits { get; set; } = new();
    public string GeneratedBy { get; set; } = string.Empty;
    public TimeSpan GenerationTime { get; set; }
}

public class PositionLimitsDto
{
    public bool WithinLimits { get; set; }
    public decimal TotalExposureLimit { get; set; } = 50_000_000m;
    public decimal MaxProductExposureLimit { get; set; } = 10_000_000m;
    public decimal CurrentTotalExposure { get; set; }
    public decimal CurrentMaxProductExposure { get; set; }
    public List<LimitBreachDto> Breaches { get; set; } = new();
}

public class LimitBreachDto
{
    public string LimitType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Current { get; set; }
    public decimal Excess { get; set; }
    public string Severity { get; set; } = string.Empty; // "Warning", "Critical"
}