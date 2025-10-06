using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.MarketData;

public class GetPriceHistoryQueryHandler : IRequestHandler<GetPriceHistoryQuery, IEnumerable<MarketPriceDto>>
{
    private readonly IMarketDataRepository _marketDataRepository;

    public GetPriceHistoryQueryHandler(IMarketDataRepository marketDataRepository)
    {
        _marketDataRepository = marketDataRepository;
    }

    public async Task<IEnumerable<MarketPriceDto>> Handle(GetPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var prices = await _marketDataRepository.GetByProductAsync(
            request.ProductCode, 
            request.StartDate, 
            request.EndDate, 
            cancellationToken);

        return prices.Select(p => new MarketPriceDto
        {
            Id = p.Id,
            PriceDate = p.PriceDate,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            PriceType = p.PriceType.ToString(),
            Price = p.Price,
            Currency = p.Currency,
            ContractMonth = p.ContractMonth,
            DataSource = p.DataSource,
            IsSettlement = p.IsSettlement,
            ImportedAt = p.ImportedAt,
            ImportedBy = p.ImportedBy
        });
    }
}