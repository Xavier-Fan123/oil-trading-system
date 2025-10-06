using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Commands.MarketData;

public class DeleteMarketDataCommand : IRequest<DeleteMarketDataResultDto>
{
    public DeleteMarketDataType DeleteType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ProductCode { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public enum DeleteMarketDataType
{
    All,
    ByDateRange,
    ByProduct
}