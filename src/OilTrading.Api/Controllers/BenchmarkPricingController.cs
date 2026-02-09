using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;
using OilTrading.Application.Services;

namespace OilTrading.Api.Controllers;

/// <summary>
/// API controller for benchmark pricing calculations.
/// Supports date range average, contract month pricing, and spot + premium calculations.
/// </summary>
[ApiController]
[Route("api/benchmark-pricing")]
public class BenchmarkPricingController : ControllerBase
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<BenchmarkPricingController> _logger;

    public BenchmarkPricingController(
        IMarketDataRepository marketDataRepository,
        ILogger<BenchmarkPricingController> logger)
    {
        _marketDataRepository = marketDataRepository;
        _logger = logger;
    }

    /// <summary>
    /// Calculate average price over a date range.
    /// </summary>
    /// <param name="productCode">Product code (e.g., "SG380", "MF 0.5")</param>
    /// <param name="startDate">Start date of the pricing period</param>
    /// <param name="endDate">End date of the pricing period</param>
    /// <param name="contractMonth">Optional contract month for futures (e.g., "Apr26")</param>
    /// <param name="priceType">Optional price type filter ("Spot" or "Settlement")</param>
    [HttpGet("date-range-average")]
    [ProducesResponseType(typeof(BenchmarkPriceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDateRangeAverage(
        [FromQuery] string productCode,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? contractMonth = null,
        [FromQuery] string? priceType = null)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (startDate > endDate)
            return BadRequest(new { error = "Start date must be before or equal to end date" });

        _logger.LogInformation(
            "Calculating date range average for {ProductCode} from {StartDate} to {EndDate}",
            productCode, startDate, endDate);

        try
        {
            var prices = await _marketDataRepository.GetByProductAsync(
                productCode, startDate, endDate, CancellationToken.None);

            // Filter by contract month if specified
            if (!string.IsNullOrEmpty(contractMonth))
            {
                prices = prices.Where(p =>
                    p.ContractMonth?.Equals(contractMonth, StringComparison.OrdinalIgnoreCase) == true ||
                    p.ContractSpecificationId?.Contains(contractMonth, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Filter by price type if specified
            if (!string.IsNullOrEmpty(priceType))
            {
                if (priceType.Equals("Spot", StringComparison.OrdinalIgnoreCase))
                {
                    prices = prices.Where(p => p.PriceType == MarketPriceType.Spot);
                }
                else if (priceType.Equals("Settlement", StringComparison.OrdinalIgnoreCase))
                {
                    prices = prices.Where(p => p.PriceType == MarketPriceType.FuturesSettlement);
                }
            }

            var priceList = prices.ToList();

            if (!priceList.Any())
            {
                return NotFound(new { error = $"No prices found for {productCode} in the specified date range" });
            }

            var result = new BenchmarkPriceResultDto
            {
                Method = "DateRangeAverage",
                ProductCode = productCode,
                ContractMonth = contractMonth,
                StartDate = startDate,
                EndDate = endDate,
                CalculatedPrice = priceList.Average(p => p.Price),
                DataPoints = priceList.Count,
                MinPrice = priceList.Min(p => p.Price),
                MaxPrice = priceList.Max(p => p.Price),
                Currency = priceList.First().Currency,
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating date range average");
            return StatusCode(500, new { error = "Error calculating price average" });
        }
    }

    /// <summary>
    /// Get price for a specific futures contract month.
    /// </summary>
    /// <param name="productCode">Product code (e.g., "SG380")</param>
    /// <param name="contractMonth">Contract month (e.g., "Apr26")</param>
    /// <param name="priceDate">Optional specific date (defaults to latest available)</param>
    [HttpGet("contract-month-price")]
    [ProducesResponseType(typeof(BenchmarkPriceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractMonthPrice(
        [FromQuery] string productCode,
        [FromQuery] string contractMonth,
        [FromQuery] DateTime? priceDate = null)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (string.IsNullOrWhiteSpace(contractMonth))
            return BadRequest(new { error = "Contract month is required" });

        _logger.LogInformation(
            "Getting contract month price for {ProductCode} {ContractMonth}",
            productCode, contractMonth);

        try
        {
            // Build contract specification ID pattern (e.g., "SG380 Apr26")
            var contractSpecId = $"{productCode} {contractMonth}";

            // Get prices within a reasonable date range
            var endDate = priceDate ?? DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-30); // Look back 30 days for data

            var prices = await _marketDataRepository.GetByProductAsync(
                productCode, startDate, endDate, CancellationToken.None);

            // Filter for the specific contract month
            var contractPrices = prices
                .Where(p => p.ContractMonth?.Equals(contractMonth, StringComparison.OrdinalIgnoreCase) == true ||
                           p.ContractSpecificationId?.Equals(contractSpecId, StringComparison.OrdinalIgnoreCase) == true)
                .Where(p => p.PriceType == MarketPriceType.FuturesSettlement)
                .OrderByDescending(p => p.PriceDate)
                .ToList();

            if (!contractPrices.Any())
            {
                return NotFound(new { error = $"No price found for {productCode} {contractMonth}" });
            }

            var latestPrice = priceDate.HasValue
                ? contractPrices.FirstOrDefault(p => p.PriceDate.Date == priceDate.Value.Date) ?? contractPrices.First()
                : contractPrices.First();

            var result = new BenchmarkPriceResultDto
            {
                Method = "ContractMonth",
                ProductCode = productCode,
                ContractMonth = contractMonth,
                PriceDate = latestPrice.PriceDate,
                CalculatedPrice = latestPrice.Price,
                DataPoints = 1,
                Currency = latestPrice.Currency,
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contract month price");
            return StatusCode(500, new { error = "Error retrieving contract month price" });
        }
    }

    /// <summary>
    /// Calculate spot price plus premium.
    /// </summary>
    /// <param name="productCode">Product code (e.g., "SG380")</param>
    /// <param name="premium">Premium amount</param>
    /// <param name="isPercentage">If true, premium is a percentage; if false, it's a fixed amount</param>
    /// <param name="priceDate">Optional specific date (defaults to latest available)</param>
    [HttpGet("spot-plus-premium")]
    [ProducesResponseType(typeof(BenchmarkPriceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpotPlusPremium(
        [FromQuery] string productCode,
        [FromQuery] decimal premium,
        [FromQuery] bool isPercentage = false,
        [FromQuery] DateTime? priceDate = null)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        _logger.LogInformation(
            "Calculating spot + premium for {ProductCode}, premium: {Premium}{PremiumType}",
            productCode, premium, isPercentage ? "%" : " fixed");

        try
        {
            // Get spot prices
            var endDate = priceDate ?? DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-7); // Look back 7 days for spot data

            var prices = await _marketDataRepository.GetByProductAsync(
                productCode, startDate, endDate, CancellationToken.None);

            var spotPrices = prices
                .Where(p => p.PriceType == MarketPriceType.Spot)
                .OrderByDescending(p => p.PriceDate)
                .ToList();

            if (!spotPrices.Any())
            {
                return NotFound(new { error = $"No spot price found for {productCode}" });
            }

            var latestSpot = priceDate.HasValue
                ? spotPrices.FirstOrDefault(p => p.PriceDate.Date == priceDate.Value.Date) ?? spotPrices.First()
                : spotPrices.First();

            var calculatedPrice = isPercentage
                ? latestSpot.Price * (1 + premium / 100)
                : latestSpot.Price + premium;

            var result = new BenchmarkPriceResultDto
            {
                Method = "SpotPlusPremium",
                ProductCode = productCode,
                PriceDate = latestSpot.PriceDate,
                SpotPrice = latestSpot.Price,
                Premium = premium,
                IsPremiumPercentage = isPercentage,
                CalculatedPrice = calculatedPrice,
                DataPoints = 1,
                Currency = latestSpot.Currency,
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating spot + premium");
            return StatusCode(500, new { error = "Error calculating spot + premium price" });
        }
    }

    /// <summary>
    /// Get basis analysis (Settlement - Spot) for a product.
    /// </summary>
    /// <param name="productCode">Product code (e.g., "SG380")</param>
    /// <param name="contractMonth">Contract month (e.g., "Apr26")</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    [HttpGet("basis-analysis")]
    [ProducesResponseType(typeof(BasisAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBasisAnalysis(
        [FromQuery] string productCode,
        [FromQuery] string contractMonth,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return BadRequest(new { error = "Product code is required" });

        if (string.IsNullOrWhiteSpace(contractMonth))
            return BadRequest(new { error = "Contract month is required" });

        _logger.LogInformation(
            "Calculating basis analysis for {ProductCode} {ContractMonth}",
            productCode, contractMonth);

        try
        {
            var prices = await _marketDataRepository.GetByProductAsync(
                productCode, startDate, endDate, CancellationToken.None);

            var priceList = prices.ToList();

            // Get spot prices by date
            var spotByDate = priceList
                .Where(p => p.PriceType == MarketPriceType.Spot)
                .GroupBy(p => p.PriceDate.Date)
                .ToDictionary(g => g.Key, g => g.First().Price);

            // Get futures settlement prices by date
            var futuresByDate = priceList
                .Where(p => p.PriceType == MarketPriceType.FuturesSettlement &&
                           (p.ContractMonth?.Equals(contractMonth, StringComparison.OrdinalIgnoreCase) == true ||
                            p.ContractSpecificationId?.Contains(contractMonth, StringComparison.OrdinalIgnoreCase) == true))
                .GroupBy(p => p.PriceDate.Date)
                .ToDictionary(g => g.Key, g => g.First().Price);

            // Calculate basis for each date where both prices exist
            var basisData = new List<BasisDataPointDto>();
            foreach (var date in futuresByDate.Keys.OrderBy(d => d))
            {
                if (spotByDate.TryGetValue(date, out var spotPrice))
                {
                    var settlementPrice = futuresByDate[date];
                    basisData.Add(new BasisDataPointDto
                    {
                        Date = date,
                        SettlementPrice = settlementPrice,
                        SpotPrice = spotPrice,
                        Basis = settlementPrice - spotPrice,
                        BasisPercent = (settlementPrice - spotPrice) / spotPrice * 100
                    });
                }
            }

            if (!basisData.Any())
            {
                return Ok(new BasisAnalysisResultDto
                {
                    ProductCode = productCode,
                    ContractMonth = contractMonth,
                    StartDate = startDate,
                    EndDate = endDate,
                    DataPoints = 0,
                    BasisHistory = new List<BasisDataPointDto>()
                });
            }

            var result = new BasisAnalysisResultDto
            {
                ProductCode = productCode,
                ContractMonth = contractMonth,
                StartDate = startDate,
                EndDate = endDate,
                CurrentBasis = basisData.Last().Basis,
                AverageBasis = basisData.Average(b => b.Basis),
                MinBasis = basisData.Min(b => b.Basis),
                MaxBasis = basisData.Max(b => b.Basis),
                DataPoints = basisData.Count,
                BasisHistory = basisData,
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating basis analysis");
            return StatusCode(500, new { error = "Error calculating basis analysis" });
        }
    }

    /// <summary>
    /// Get available contract months for a product.
    /// </summary>
    [HttpGet("available-contract-months/{productCode}")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableContractMonths(string productCode)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-400);

            var prices = await _marketDataRepository.GetByProductAsync(
                productCode, startDate, endDate, CancellationToken.None);

            var contractMonths = prices
                .Where(p => !string.IsNullOrEmpty(p.ContractMonth))
                .Select(p => p.ContractMonth!)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            return Ok(contractMonths);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available contract months");
            return StatusCode(500, new { error = "Error retrieving contract months" });
        }
    }

    /// <summary>
    /// Get list of available X-group product codes.
    /// </summary>
    [HttpGet("product-codes")]
    [ProducesResponseType(typeof(IEnumerable<ProductCodeDto>), StatusCodes.Status200OK)]
    public IActionResult GetProductCodes()
    {
        var products = XGroupDataParser.GetKnownProductCodes()
            .Select(code => new ProductCodeDto
            {
                Code = code,
                DisplayName = XGroupDataParser.GetProductDisplayName(code)
            })
            .ToList();

        return Ok(products);
    }
}

#region DTOs

public class BenchmarkPriceResultDto
{
    public string Method { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ContractMonth { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? PriceDate { get; set; }
    public decimal? SpotPrice { get; set; }
    public decimal? Premium { get; set; }
    public bool? IsPremiumPercentage { get; set; }
    public decimal CalculatedPrice { get; set; }
    public int DataPoints { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CalculatedAt { get; set; }
}

public class BasisAnalysisResultDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ContractMonth { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? CurrentBasis { get; set; }
    public decimal? AverageBasis { get; set; }
    public decimal? MinBasis { get; set; }
    public decimal? MaxBasis { get; set; }
    public int DataPoints { get; set; }
    public List<BasisDataPointDto> BasisHistory { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

public class BasisDataPointDto
{
    public DateTime Date { get; set; }
    public decimal SettlementPrice { get; set; }
    public decimal SpotPrice { get; set; }
    public decimal Basis { get; set; }
    public decimal BasisPercent { get; set; }
}

public class ProductCodeDto
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

#endregion
