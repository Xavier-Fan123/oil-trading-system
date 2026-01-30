using MediatR;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using System.Text.RegularExpressions;

namespace OilTrading.Application.Queries.MarketData;

/// <summary>
/// Professional Oil Trading Handler - Extracts contract months from futures product codes
/// and links futures contracts with spot prices using industry-standard mappings
/// </summary>
public class GetLatestPricesQueryHandler : IRequestHandler<GetLatestPricesQuery, LatestPricesDto>
{
    private readonly IMarketDataRepository _marketDataRepository;

    public GetLatestPricesQueryHandler(IMarketDataRepository marketDataRepository)
    {
        _marketDataRepository = marketDataRepository;
    }

    /// <summary>
    /// Extracts contract month from futures product code using oil trading standards
    /// Examples: BRT_FUT_202511 → 202511 (Nov 2025), GO_10PPM_202512 → 202512 (Dec 2025)
    /// </summary>
    private string? ExtractContractMonthFromProductCode(string productCode)
    {
        if (string.IsNullOrEmpty(productCode))
            return null;

        // Oil industry standard: Contract months are encoded as YYYYMM at end of product code
        // Pattern: Look for 6 consecutive digits at the end (e.g., 202511, 202512, 202601)
        var match = Regex.Match(productCode, @"(\d{6})$");

        if (match.Success)
        {
            var yyyymm = match.Groups[1].Value;

            // Validate it's a real month (01-12)
            if (yyyymm.Length == 6 &&
                int.TryParse(yyyymm.Substring(4, 2), out var month) &&
                month >= 1 && month <= 12)
            {
                // Format as MMMYY (e.g., "NOV25", "DEC25") for professional display
                return FormatContractMonth(yyyymm);
            }
        }

        return null;
    }

    /// <summary>
    /// Converts YYYYMM format to professional MMM-YY format (e.g., 202511 → NOV-25)
    /// </summary>
    private string FormatContractMonth(string yyyymm)
    {
        if (yyyymm.Length != 6 || !int.TryParse(yyyymm, out int dateNum))
            return yyyymm;

        int year = dateNum / 100;
        int month = dateNum % 100;

        string[] monthNames = { "", "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
                              "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        if (month < 1 || month > 12)
            return yyyymm;

        // Format: MMM-YY (e.g., NOV-25)
        string lastTwoYears = (year % 100).ToString("D2");
        return $"{monthNames[month]}{lastTwoYears}";
    }

    /// <summary>
    /// Maps futures product codes to their base spot product code
    /// Professional oil trading requires understanding which futures correspond to which spot products
    /// Examples: BRT_FUT_* → BRENT_CRUDE, GO_10PPM_* → GO_10PPM, MF_0.5_* → MARINE_FUEL_05
    /// </summary>
    private string MapFuturesToSpotProduct(string futuresProductCode)
    {
        if (string.IsNullOrEmpty(futuresProductCode))
            return futuresProductCode;

        // Remove contract month suffix to get base product code
        var baseCode = Regex.Replace(futuresProductCode, @"_\d{6}$", "");

        // Professional oil trading mapping
        return baseCode switch
        {
            // Brent Crude and Swaps
            "BRT_FUT" => "BRENT_CRUDE",
            "BRT_SWP" => "BRENT_CRUDE",
            "BRT_1ST" => "BRENT_1ST",

            // Gasoil and Distillates
            "GO/_180" => "GASOIL_FUTURES",
            "GO/_380" => "GASOIL_FUTURES",
            "GO_10PPM" => "GO_10PPM",
            "GO_10PPM_TS" => "GO_10PPM",
            "GO_BRT" => "GASOIL_FUTURES",

            // Marine Fuels and Spreads
            "MF_0.5" => "MARINE_FUEL_05",
            "MF0.5_TS" => "MARINE_FUEL_05",
            "MF_0.5_BRT" => "MARINE_FUEL_05",

            // Gasoline
            "92R_BRT" => "GASOLINE_92",
            "92R_TS" => "GASOLINE_92",

            // Singapore Fuel Oil Futures - Use identity mapping (SG380 is already correct)
            // DO NOT map to MOPS (spot prices) - these are futures contracts
            "SG180" => "SG180",
            "SG380" => "SG380",
            "SG05" => "SG05",

            // MOP Spreads
            "MOPJ_BRT" => "MOPJ",
            "MOPJ_TS" => "MOPJ",

            // Default: return the base code as-is
            _ => baseCode
        };
    }

    public async Task<LatestPricesDto> Handle(GetLatestPricesQuery request, CancellationToken cancellationToken)
    {
        var latestPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);

        // TIER 1: Get spot prices (straightforward - already have product code)
        var spotPrices = latestPrices
            .Where(p => p.PriceType == MarketPriceType.Spot)
            .Select(p => new ProductPriceDto
            {
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                Price = p.Price,
                PreviousPrice = null,
                Change = null,
                ChangePercent = null,
                PriceDate = p.PriceDate,
                Region = p.Region  // "Singapore", "Dubai" for spot prices
            })
            .ToList();

        // TIER 2: Get futures prices with NEW ARCHITECTURE
        // Spot and Futures now share ProductCode, differentiated by ContractMonth field
        var futuresPrices = latestPrices
            .Where(p => p.PriceType == MarketPriceType.FuturesSettlement || p.PriceType == MarketPriceType.FuturesClose)
            .Select(p => new FuturesPriceDto
            {
                ProductCode = p.ProductCode,        // Base product code (e.g., "BRENT_CRUDE")
                ProductName = p.ProductName,        // Product name (e.g., "Brent Crude Oil")
                ContractMonth = p.ContractMonth ?? "", // Contract month from database field (e.g., "2025-08")
                Price = p.Price,                    // Settlement or Close price
                PreviousSettlement = null,          // TODO: Calculate from previous day's price
                Change = null,                      // TODO: Calculate price change
                PriceDate = p.PriceDate,            // Price date
                Region = p.Region                   // Usually null for futures (exchange-traded)
            })
            .ToList();

        var result = new LatestPricesDto
        {
            LastUpdateDate = latestPrices.Any() ? latestPrices.Max(p => p.PriceDate) : DateTime.MinValue,
            SpotPrices = spotPrices,
            FuturesPrices = futuresPrices
        };

        return result;
    }
}