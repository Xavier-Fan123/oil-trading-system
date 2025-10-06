using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.MarketData;

public class GetLatestPricesQueryHandler : IRequestHandler<GetLatestPricesQuery, LatestPricesDto>
{
    private readonly IMarketDataRepository _marketDataRepository;

    public GetLatestPricesQueryHandler(IMarketDataRepository marketDataRepository)
    {
        _marketDataRepository = marketDataRepository;
    }

    public async Task<LatestPricesDto> Handle(GetLatestPricesQuery request, CancellationToken cancellationToken)
    {
        var latestPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
        
        var result = new LatestPricesDto
        {
            LastUpdateDate = latestPrices.Any() ? latestPrices.Max(p => p.PriceDate) : DateTime.MinValue,
            SpotPrices = latestPrices
                .Where(p => p.PriceType == MarketPriceType.Spot)
                .Select(p => new ProductPriceDto
                {
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    PreviousPrice = null, // For now, set to null until historical data is properly optimized
                    Change = null, // For now, set to null until historical data is properly optimized
                    ChangePercent = null, // For now, set to null until historical data is properly optimized
                    PriceDate = p.PriceDate
                })
                .ToList(),
            FuturesPrices = latestPrices
                .Where(p => p.PriceType == MarketPriceType.FuturesSettlement)
                .Select(p => new FuturesPriceDto
                {
                    ProductType = p.ProductCode,
                    ContractMonth = p.ContractMonth ?? "",
                    SettlementPrice = p.Price,
                    PreviousSettlement = null, // For now, set to null until historical data is properly optimized
                    Change = null, // For now, set to null until historical data is properly optimized
                    SettlementDate = p.PriceDate
                })
                .ToList()
        };
        
        return result;
    }
}