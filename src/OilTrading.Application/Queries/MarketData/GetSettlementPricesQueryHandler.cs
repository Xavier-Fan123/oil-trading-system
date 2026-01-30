using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using AutoMapper;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Handler for GetSettlementPricesQuery
/// Retrieves market prices for settlement calculation from the repository
/// </summary>
public class GetSettlementPricesQueryHandler : IRequestHandler<GetSettlementPricesQuery, IEnumerable<MarketPriceDto>>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<GetSettlementPricesQueryHandler> _logger;
    private readonly IMapper _mapper;

    public GetSettlementPricesQueryHandler(
        IMarketDataRepository marketDataRepository,
        ILogger<GetSettlementPricesQueryHandler> logger,
        IMapper mapper)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<MarketPriceDto>> Handle(GetSettlementPricesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving settlement prices: ProductCode={ProductCode}, ContractMonth={ContractMonth}, DateRange={StartDate}-{EndDate}, PriceType={PriceType}",
            request.ProductCode, request.ContractMonth, request.StartDate.Date, request.EndDate.Date, request.PriceType);

        try
        {
            var prices = await _marketDataRepository.GetByProductCodeAndContractMonthRangeAsync(
                request.ProductCode,
                request.ContractMonth,
                request.StartDate,
                request.EndDate,
                request.PriceType,
                cancellationToken);

            var result = prices.ToList();

            _logger.LogInformation(
                "Retrieved {Count} settlement prices for ProductCode={ProductCode}, ContractMonth={ContractMonth}",
                result.Count, request.ProductCode, request.ContractMonth);

            return _mapper.Map<IEnumerable<MarketPriceDto>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving settlement prices for ProductCode={ProductCode}, ContractMonth={ContractMonth}",
                request.ProductCode, request.ContractMonth);
            throw;
        }
    }
}
