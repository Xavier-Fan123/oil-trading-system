using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.Positions;

public class RecalculatePositionsCommand : IRequest<PositionRecalculationResultDto>
{
    public bool ForceRecalculation { get; set; } = false;
    public string[]? ProductTypes { get; set; }
    public DateTime? AsOfDate { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class PositionRecalculationResultDto
{
    public int TotalPositionsRecalculated { get; set; }
    public DateTime RecalculationTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}