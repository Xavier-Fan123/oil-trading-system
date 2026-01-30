using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using Microsoft.Extensions.Logging;

namespace OilTrading.Application.Queries.MarketData;

public class GetPriceHistoryQueryHandler : IRequestHandler<GetPriceHistoryQuery, IEnumerable<MarketPriceDto>>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IProductCodeResolverService _productCodeResolver;
    private readonly ILogger<GetPriceHistoryQueryHandler> _logger;

    public GetPriceHistoryQueryHandler(
        IMarketDataRepository marketDataRepository,
        IProductCodeResolverService productCodeResolver,
        ILogger<GetPriceHistoryQueryHandler> logger)
    {
        _marketDataRepository = marketDataRepository;
        _productCodeResolver = productCodeResolver;
        _logger = logger;
    }

    public async Task<IEnumerable<MarketPriceDto>> Handle(GetPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        // Professional product code resolution with market type awareness
        var productCodesToQuery = new List<string>();

        // Try to resolve database code to API code(s)
        var dbCode = _productCodeResolver.ResolveToDBCode(request.ProductCode);
        if (dbCode != null)
        {
            // Frontend sent API code (e.g., "BUNKER_SPORE") - resolve to database code
            _logger.LogInformation("Resolved API code '{ApiCode}' to database code '{DbCode}'", request.ProductCode, dbCode);

            // Query using database code
            productCodesToQuery.Add(dbCode);

            // Also query with original API code for backward compatibility
            productCodesToQuery.Add(request.ProductCode);
        }
        else
        {
            // Frontend sent database code (e.g., "BRENT", "HFO380")
            // Use prefix matching to find all related API codes
            var relatedCodes = _productCodeResolver.ResolveWithPrefixMatching(request.ProductCode);

            if (relatedCodes.Any())
            {
                _logger.LogInformation("Resolved database code '{DbCode}' to {Count} API codes: {ApiCodes}",
                    request.ProductCode, relatedCodes.Count(), string.Join(", ", relatedCodes));
                productCodesToQuery.AddRange(relatedCodes);
            }

            // Also add the original code
            productCodesToQuery.Add(request.ProductCode);
        }

        // Query database for all related product codes
        var allPrices = new List<MarketPrice>();
        foreach (var productCode in productCodesToQuery.Distinct())
        {
            // Try exact match first
            var prices = await _marketDataRepository.GetByProductAsync(
                productCode,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            allPrices.AddRange(prices);

            // ADDED: Try prefix match for legacy data with embedded contract months
            // Example: "SG380" matches legacy records stored as "SG380 2511", "SG380 2512"
            // Add space after productCode to avoid matching "SG380TS" when searching for "SG380"
            var prefixPrices = await _marketDataRepository.GetByProductPrefixAsync(
                productCode + " ",
                request.StartDate,
                request.EndDate,
                cancellationToken);
            allPrices.AddRange(prefixPrices);
        }

        // Remove duplicates based on Id
        var uniquePrices = allPrices.DistinctBy(p => p.Id);

        // CRITICAL FIX: Filter out derivative products (Time Spreads, Brent spreads, etc.)
        // when querying for base futures settlement prices
        // Example: When querying "SG380", exclude "SG380_TS", "SG380_BRT"
        // Example: When querying "GO_10PPM", exclude "GO_10PPM_TS"
        var filteredByProductType = uniquePrices.Where(p =>
        {
            // Always filter out derivative product suffixes, regardless of the query product code
            // Check if the current price record is a derivative product
            if (p.ProductCode.EndsWith("_TS") ||          // Time Spreads
                p.ProductCode.EndsWith("_BRT") ||         // Brent spreads
                p.ProductCode.Contains("_SPREAD") ||      // All spreads
                p.ProductCode.EndsWith("_EFS"))           // Exchange for Swaps
            {
                // But only filter if user is NOT explicitly requesting the derivative
                // Check if any of the query codes exactly match this derivative
                bool explicitlyRequested = productCodesToQuery.Any(queryCode =>
                    queryCode.Equals(p.ProductCode, StringComparison.OrdinalIgnoreCase));

                if (!explicitlyRequested)
                {
                    _logger.LogDebug("Filtering out derivative product: {ProductCode} (base query: {BaseProducts})",
                        p.ProductCode, string.Join(", ", productCodesToQuery));
                    return false;
                }
            }
            return true;
        }).ToList();

        // Normalize ContractMonth for legacy data with embedded contract months in ProductCode
        // Example: "SG380 2511" in ProductCode → extract "2511" and convert to "202511" in ContractMonth
        var normalizedPrices = filteredByProductType.Select(p =>
        {
            if (string.IsNullOrEmpty(p.ContractMonth) && p.ProductCode.Contains(" "))
            {
                // Try to parse "PRODUCT YYMM" format from ProductCode
                var match = System.Text.RegularExpressions.Regex.Match(p.ProductCode, @"\s+(\d{4})$");
                if (match.Success)
                {
                    var yymm = match.Groups[1].Value;

                    // Validate and convert YYMM to YYYYMM format
                    if (int.TryParse(yymm.Substring(0, 2), out var year) &&
                        int.TryParse(yymm.Substring(2, 2), out var month) &&
                        month >= 1 && month <= 12)
                    {
                        var fullYear = 2000 + year;
                        p.ContractMonth = $"{fullYear:0000}{month:00}";

                        _logger.LogDebug("Normalized ContractMonth for legacy data: ProductCode='{ProductCode}' -> ContractMonth='{ContractMonth}'",
                            p.ProductCode, p.ContractMonth);
                    }
                }
            }
            return p;
        }).ToList();

        // Apply optional filters for PriceType, ContractMonth, and Region
        var filtered = normalizedPrices.AsEnumerable();

        // Filter by PriceType if specified
        if (!string.IsNullOrEmpty(request.PriceType) &&
            Enum.TryParse<MarketPriceType>(request.PriceType, out var priceTypeEnum))
        {
            filtered = filtered.Where(p => p.PriceType == priceTypeEnum);
        }

        // Filter by ContractMonth if specified
        if (!string.IsNullOrEmpty(request.ContractMonth))
        {
            filtered = filtered.Where(p => p.ContractMonth == request.ContractMonth);
        }

        // Filter by Region if specified (spot prices only)
        // Graceful degradation: Also return records with NULL region (old data compatibility)
        if (!string.IsNullOrEmpty(request.Region))
        {
            filtered = filtered.Where(p =>
                p.Region == request.Region ||      // Exact match
                string.IsNullOrEmpty(p.Region)     // OR old data without region
            );
        }

        return filtered.Select(p => new MarketPriceDto
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
            ImportedBy = p.ImportedBy,
            Region = p.Region  // Include region in response
        });
    }
}