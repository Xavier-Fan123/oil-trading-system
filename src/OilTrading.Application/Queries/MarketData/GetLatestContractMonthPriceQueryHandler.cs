using MediatR;
using Microsoft.Extensions.Logging;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using AutoMapper;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Handler for GetLatestContractMonthPriceQuery
/// Retrieves the most recent price for a product/contract month combination
/// </summary>
public class GetLatestContractMonthPriceQueryHandler : IRequestHandler<GetLatestContractMonthPriceQuery, MarketPriceDto?>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<GetLatestContractMonthPriceQueryHandler> _logger;
    private readonly IMapper _mapper;

    public GetLatestContractMonthPriceQueryHandler(
        IMarketDataRepository marketDataRepository,
        ILogger<GetLatestContractMonthPriceQueryHandler> logger,
        IMapper mapper)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<MarketPriceDto?> Handle(GetLatestContractMonthPriceQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving latest contract month price: ProductCode={ProductCode}, ContractMonth={ContractMonth}, PriceType={PriceType}",
            request.ProductCode, request.ContractMonth, request.PriceType);

        try
        {
            var price = await _marketDataRepository.GetLatestByProductCodeAndContractMonthAsync(
                request.ProductCode,
                request.ContractMonth,
                request.PriceType,
                cancellationToken);

            if (price != null)
            {
                _logger.LogInformation(
                    "Latest contract month price found: ProductCode={ProductCode}, ContractMonth={ContractMonth}, Price={Price}, Date={Date}",
                    request.ProductCode, request.ContractMonth, price.Price, price.PriceDate);
            }
            else
            {
                _logger.LogWarning(
                    "No price found for ProductCode={ProductCode}, ContractMonth={ContractMonth}",
                    request.ProductCode, request.ContractMonth);
            }

            return price == null ? null : _mapper.Map<MarketPriceDto>(price);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving latest price for ProductCode={ProductCode}, ContractMonth={ContractMonth}",
                request.ProductCode, request.ContractMonth);
            throw;
        }
    }
}
