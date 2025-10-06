using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.PaperContracts;

public class GetPnLSummaryQueryHandler : IRequestHandler<GetPnLSummaryQuery, PnLSummaryDto>
{
    private readonly IPaperContractRepository _repository;

    public GetPnLSummaryQueryHandler(IPaperContractRepository repository)
    {
        _repository = repository;
    }

    public async Task<PnLSummaryDto> Handle(GetPnLSummaryQuery request, CancellationToken cancellationToken)
    {
        // Get all positions (for this simple implementation)
        var openPositions = await _repository.GetOpenPositionsAsync(cancellationToken);
        
        // In a real implementation, you would:
        // 1. Query closed positions within the date range
        // 2. Calculate daily P&L from historical data
        // 3. Group by product types
        
        var summary = new PnLSummaryDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OpenPositions = openPositions.Count(),
            ClosedPositions = 0, // Would query closed positions in date range
            TotalUnrealizedPnL = openPositions.Sum(p => p.UnrealizedPnL ?? 0),
            TotalRealizedPnL = 0, // Would sum realized P&L from closed positions
            NetPnL = openPositions.Sum(p => p.UnrealizedPnL ?? 0)
        };

        // Product breakdown
        summary.ProductBreakdown = openPositions
            .GroupBy(p => p.ProductType)
            .Select(g => new ProductPnLDto
            {
                ProductType = g.Key,
                UnrealizedPnL = g.Sum(p => p.UnrealizedPnL ?? 0),
                RealizedPnL = 0, // Would include closed positions
                NetPnL = g.Sum(p => p.UnrealizedPnL ?? 0),
                PositionCount = g.Count()
            })
            .ToList();

        // Daily P&L (placeholder - in real implementation would be calculated from historical data)
        summary.DailyPnL = new List<DailyPnLDto>();

        return summary;
    }
}