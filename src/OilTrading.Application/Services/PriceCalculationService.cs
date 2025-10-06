using OilTrading.Core.Repositories;
using OilTrading.Core.Common;
using OilTrading.Core.ValueObjects;
using OilTrading.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace OilTrading.Application.Services;

public class PriceCalculationService : IPriceCalculationService
{
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBasisCalculationService _basisCalculationService;
    private readonly IPriceInterpolationService _priceInterpolationService;
    private readonly ILogger<PriceCalculationService> _logger;

    public PriceCalculationService(
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IMarketDataRepository marketDataRepository,
        IUnitOfWork unitOfWork,
        IBasisCalculationService basisCalculationService,
        IPriceInterpolationService priceInterpolationService,
        ILogger<PriceCalculationService> logger)
    {
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _marketDataRepository = marketDataRepository;
        _unitOfWork = unitOfWork;
        _basisCalculationService = basisCalculationService;
        _priceInterpolationService = priceInterpolationService;
        _logger = logger;
    }

    public async Task<decimal> CalculateContractPriceAsync(Guid contractId)
    {
        // Try purchase contract first
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(contractId);
        if (purchaseContract != null)
        {
            return await CalculatePurchaseContractPriceAsync(purchaseContract);
        }

        // Try sales contract
        var salesContract = await _salesContractRepository.GetByIdAsync(contractId);
        if (salesContract != null)
        {
            return await CalculateSalesContractPriceAsync(salesContract);
        }

        throw new NotFoundException($"Contract with ID {contractId} not found");
    }

    public async Task<decimal> CalculatePeriodAveragePriceAsync(string benchmarkName, DateTime startDate, DateTime endDate)
    {
        var dailyPrices = await GetDailyPricesAsync(benchmarkName, startDate, endDate);
        
        if (dailyPrices.Length == 0)
            throw new InvalidOperationException($"No price data found for {benchmarkName} between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}");

        return dailyPrices.Average();
    }

    public async Task<bool> FinalizePriceAsync(Guid contractId, string finalizedBy)
    {
        // Try purchase contract first
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(contractId);
        if (purchaseContract != null)
        {
            if (purchaseContract.IsPriceFinalized)
                return false; // Already finalized

            var finalPrice = await CalculatePurchaseContractPriceAsync(purchaseContract);
            var finalContractValue = Money.Dollar(finalPrice * purchaseContract.ContractQuantity.Value);
            
            purchaseContract.FinalizePrice(finalContractValue, finalizedBy);
            await _purchaseContractRepository.UpdateAsync(purchaseContract);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Try sales contract
        var salesContract = await _salesContractRepository.GetByIdAsync(contractId);
        if (salesContract != null)
        {
            if (salesContract.IsPriceFinalized)
                return false; // Already finalized

            var finalPrice = await CalculateSalesContractPriceAsync(salesContract);
            var finalContractValue = Money.Dollar(finalPrice * salesContract.ContractQuantity.Value);
            
            // Sales contracts don't have FinalizePrice method in the current domain model
            // This would need to be added to the SalesContract entity
            // For now, just update the contract value
            salesContract.UpdatePricing(salesContract.PriceFormula!, finalContractValue);
            await _salesContractRepository.UpdateAsync(salesContract);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        throw new NotFoundException($"Contract with ID {contractId} not found");
    }

    public Task<int> CalculateBusinessDaysAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date must be before end date");

        var businessDays = 0;
        var current = startDate.Date;

        while (current <= endDate.Date)
        {
            // Skip weekends (Saturday = 6, Sunday = 0)
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                // In a real implementation, you would also check for holidays here
                businessDays++;
            }
            current = current.AddDays(1);
        }

        return Task.FromResult(businessDays);
    }

    public async Task<decimal[]> GetDailyPricesAsync(string benchmarkName, DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Getting daily prices for {BenchmarkName} from {StartDate} to {EndDate}", 
            benchmarkName, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

        try
        {
            // Convert benchmark name to product code format used in database
            var productCode = ConvertBenchmarkToProductCode(benchmarkName);
            
            // Get historical prices from the uploaded market data
            var marketPrices = await _marketDataRepository.GetHistoricalPricesAsync(
                productCode, startDate, endDate);

            if (!marketPrices.Any())
            {
                _logger.LogWarning("No price data found for {ProductCode} between {StartDate} and {EndDate}. Using interpolation.", 
                    productCode, startDate, endDate);
                
                // Use interpolation service to get prices
                return await _priceInterpolationService.GetInterpolatedPricesAsync(
                    productCode, startDate, endDate);
            }

            // Convert to business days only and fill missing dates
            var businessDayPrices = new List<decimal>();
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                // Skip weekends
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && 
                    currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    var priceForDate = marketPrices.FirstOrDefault(p => p.PriceDate.Date == currentDate);
                    
                    if (priceForDate != null)
                    {
                        businessDayPrices.Add(priceForDate.Price);
                        _logger.LogDebug("Found price {Price} for {ProductCode} on {Date}", 
                            priceForDate.Price, productCode, currentDate.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        // Use interpolation for missing dates
                        var interpolatedPrice = await _priceInterpolationService.GetPriceForDateAsync(
                            productCode, currentDate);
                        businessDayPrices.Add(interpolatedPrice);
                        _logger.LogDebug("Used interpolated price {Price} for {ProductCode} on {Date}", 
                            interpolatedPrice, productCode, currentDate.ToString("yyyy-MM-dd"));
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            _logger.LogInformation("Retrieved {Count} business day prices for {ProductCode}", 
                businessDayPrices.Count, productCode);

            return businessDayPrices.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily prices for {BenchmarkName}. Falling back to last known prices.", 
                benchmarkName);
            
            // Fallback: get the latest available price and use it for all dates
            return await GetFallbackPricesAsync(benchmarkName, startDate, endDate);
        }
    }

    private string ConvertBenchmarkToProductCode(string benchmarkName)
    {
        // Convert common benchmark names to product codes used in your system
        return benchmarkName.ToUpper().Trim() switch
        {
            "BRENT" => "ICE_BRENT",
            "WTI" => "NYMEX_WTI", 
            "MOPS FO 380" => "MOPS_380",
            "MOPS MGO" => "MOPS_MGO",
            "MOPS FO 180" => "MOPS_180",
            "GASOIL" => "ICE_GASOIL",
            _ => benchmarkName.ToUpper().Replace(" ", "_")
        };
    }

    private async Task<decimal[]> GetFallbackPricesAsync(string benchmarkName, DateTime startDate, DateTime endDate)
    {
        try
        {
            var productCode = ConvertBenchmarkToProductCode(benchmarkName);
            var latestPrice = await _marketDataRepository.GetLatestPriceAsync(productCode, endDate);
            
            var fallbackPrice = latestPrice?.Price ?? GetDefaultPriceForBenchmark(benchmarkName);
            var businessDays = await CalculateBusinessDaysAsync(startDate, endDate);
            
            _logger.LogWarning("Using fallback price {Price} for {BusinessDays} business days for {BenchmarkName}", 
                fallbackPrice, businessDays, benchmarkName);
            
            return Enumerable.Repeat(fallbackPrice, businessDays).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback price calculation for {BenchmarkName}", benchmarkName);
            
            // Last resort: use default prices
            var defaultPrice = GetDefaultPriceForBenchmark(benchmarkName);
            var businessDays = await CalculateBusinessDaysAsync(startDate, endDate);
            return Enumerable.Repeat(defaultPrice, businessDays).ToArray();
        }
    }

    private static decimal GetDefaultPriceForBenchmark(string benchmarkName)
    {
        return benchmarkName.ToUpper().Trim() switch
        {
            "BRENT" => 80.0m,
            "WTI" => 78.0m,
            "MOPS FO 380" => 420.0m,
            "MOPS MGO" => 650.0m,
            "MOPS FO 180" => 380.0m,
            "GASOIL" => 85.0m,
            _ => 100.0m
        };
    }

    private async Task<decimal> CalculatePurchaseContractPriceAsync(Core.Entities.PurchaseContract contract)
    {
        if (contract.PriceFormula == null)
            throw new InvalidOperationException("Contract has no pricing formula");

        if (contract.PriceFormula.IsFixedPrice)
            return contract.PriceFormula.BasePrice?.Amount ?? 0;

        if (!contract.PricingPeriodStart.HasValue || !contract.PricingPeriodEnd.HasValue)
            throw new InvalidOperationException("Pricing period is required for floating price contracts");

        // Parse the formula to extract benchmark and adjustments
        var formula = contract.PriceFormula.Formula;
        
        // Simple parsing - in reality, you'd use the regex pattern from PriceFormula
        if (formula.Contains("AVG(") && formula.Contains(")"))
        {
            var benchmarkStart = formula.IndexOf("AVG(") + 4;
            var benchmarkEnd = formula.IndexOf(")", benchmarkStart);
            var benchmarkName = formula.Substring(benchmarkStart, benchmarkEnd - benchmarkStart);
            
            var avgPrice = await CalculatePeriodAveragePriceAsync(
                benchmarkName, 
                contract.PricingPeriodStart.Value, 
                contract.PricingPeriodEnd.Value);

            // Apply premium/discount
            var premium = contract.Premium?.Amount ?? 0;
            var discount = contract.Discount?.Amount ?? 0;
            
            return avgPrice + premium - discount;
        }

        throw new NotSupportedException($"Pricing formula not supported: {formula}");
    }

    private async Task<decimal> CalculateSalesContractPriceAsync(Core.Entities.SalesContract contract)
    {
        if (contract.PriceFormula == null)
            throw new InvalidOperationException("Contract has no pricing formula");

        if (contract.PriceFormula.IsFixedPrice)
            return contract.PriceFormula.BasePrice?.Amount ?? 0;

        if (!contract.PricingPeriodStart.HasValue || !contract.PricingPeriodEnd.HasValue)
            throw new InvalidOperationException("Pricing period is required for floating price contracts");

        // Similar logic to purchase contract
        var formula = contract.PriceFormula.Formula;
        
        if (formula.Contains("AVG(") && formula.Contains(")"))
        {
            var benchmarkStart = formula.IndexOf("AVG(") + 4;
            var benchmarkEnd = formula.IndexOf(")", benchmarkStart);
            var benchmarkName = formula.Substring(benchmarkStart, benchmarkEnd - benchmarkStart);
            
            var avgPrice = await CalculatePeriodAveragePriceAsync(
                benchmarkName, 
                contract.PricingPeriodStart.Value, 
                contract.PricingPeriodEnd.Value);

            // Apply premium/discount
            var premium = contract.Premium?.Amount ?? 0;
            var discount = contract.Discount?.Amount ?? 0;
            
            return avgPrice + premium - discount;
        }

        throw new NotSupportedException($"Pricing formula not supported: {formula}");
    }

    public async Task<PriceCalculationResult> CalculateContractPriceWithDetailsAsync(Guid contractId)
    {
        _logger.LogInformation("Calculating detailed contract price for contract {ContractId}", contractId);

        // Try purchase contract first
        var purchaseContract = await _purchaseContractRepository.GetByIdAsync(contractId);
        if (purchaseContract != null)
        {
            return await CalculatePurchaseContractPriceWithDetailsAsync(purchaseContract);
        }

        // Try sales contract
        var salesContract = await _salesContractRepository.GetByIdAsync(contractId);
        if (salesContract != null)
        {
            return await CalculateSalesContractPriceWithDetailsAsync(salesContract);
        }

        throw new NotFoundException($"Contract with ID {contractId} not found");
    }

    public async Task<decimal> CalculateBasisAdjustedPriceAsync(decimal futuresPrice, string productType, DateTime valuationDate, string futuresContract)
    {
        _logger.LogInformation("Calculating basis-adjusted price for {ProductType} futures {FuturesContract}", 
            productType, futuresContract);

        return await _basisCalculationService.CalculateBasisAdjustedPriceAsync(futuresPrice, productType, valuationDate, futuresContract);
    }

    public async Task<decimal> CalculateSpreadAdjustedPriceAsync(string baseBenchmark, string spreadBenchmark, DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Calculating spread-adjusted price between {BaseBenchmark} and {SpreadBenchmark}", 
            baseBenchmark, spreadBenchmark);

        var basePrices = await GetDailyPricesAsync(baseBenchmark, startDate, endDate);
        var spreadPrices = await GetDailyPricesAsync(spreadBenchmark, startDate, endDate);

        if (basePrices.Length == 0 || spreadPrices.Length == 0)
        {
            throw new InvalidOperationException($"Insufficient price data for spread calculation between {baseBenchmark} and {spreadBenchmark}");
        }

        var minLength = Math.Min(basePrices.Length, spreadPrices.Length);
        var spreadAdjustedPrices = new List<decimal>();

        for (int i = 0; i < minLength; i++)
        {
            var spread = basePrices[i] - spreadPrices[i];
            spreadAdjustedPrices.Add(basePrices[i] + spread);
        }

        return spreadAdjustedPrices.Average();
    }

    private async Task<PriceCalculationResult> CalculatePurchaseContractPriceWithDetailsAsync(Core.Entities.PurchaseContract contract)
    {
        var result = new PriceCalculationResult
        {
            CalculationDate = DateTime.UtcNow,
            BenchmarkUsed = contract.PriceFormula?.IndexName ?? "Fixed"
        };

        if (contract.PriceFormula == null)
        {
            throw new InvalidOperationException("Contract has no pricing formula");
        }

        if (contract.PriceFormula.IsFixedPrice)
        {
            result.FinalPrice = contract.PriceFormula.BasePrice?.Amount ?? 0;
            result.CalculationMethod = "Fixed Price";
            result.CalculationDetails["FixedPrice"] = result.FinalPrice;
            return result;
        }

        if (!contract.PricingPeriodStart.HasValue || !contract.PricingPeriodEnd.HasValue)
        {
            throw new InvalidOperationException("Pricing period is required for floating price contracts");
        }

        var formula = contract.PriceFormula.Formula;
        result.CalculationMethod = contract.PriceFormula.Method.ToString();

        if (formula.Contains("AVG(") && formula.Contains(")"))
        {
            var benchmarkStart = formula.IndexOf("AVG(") + 4;
            var benchmarkEnd = formula.IndexOf(")", benchmarkStart);
            var benchmarkName = formula.Substring(benchmarkStart, benchmarkEnd - benchmarkStart);

            // Get daily prices for detailed calculation
            result.DailyPrices = await GetDailyPricesAsync(benchmarkName, 
                contract.PricingPeriodStart.Value, 
                contract.PricingPeriodEnd.Value);

            result.BusinessDaysUsed = result.DailyPrices.Length;
            var avgPrice = result.DailyPrices.Average();

            // Apply premium/discount
            var premium = contract.Premium?.Amount ?? 0;
            var discount = contract.Discount?.Amount ?? 0;

            result.FinalPrice = avgPrice + premium - discount;

            // Store calculation details
            result.CalculationDetails["BenchmarkAverage"] = avgPrice;
            result.CalculationDetails["Premium"] = premium;
            result.CalculationDetails["Discount"] = discount;
            result.CalculationDetails["PricingPeriodStart"] = contract.PricingPeriodStart.Value;
            result.CalculationDetails["PricingPeriodEnd"] = contract.PricingPeriodEnd.Value;

            // Check if basis adjustment is needed
            if (formula.Contains("BASIS") || formula.Contains("FUTURES"))
            {
                try
                {
                    var futuresContract = ExtractFuturesContract(formula);
                    if (!string.IsNullOrEmpty(futuresContract))
                    {
                        var basis = await _basisCalculationService.CalculateBasisAsync(
                            benchmarkName, contract.PricingPeriodEnd.Value, futuresContract);
                        
                        result.FinalPrice += basis;
                        result.CalculationDetails["BasisAdjustment"] = basis;
                        result.CalculationDetails["FuturesContract"] = futuresContract;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not apply basis adjustment for contract {ContractId}", contract.Id);
                }
            }

            return result;
        }

        throw new NotSupportedException($"Pricing formula not supported: {formula}");
    }

    private async Task<PriceCalculationResult> CalculateSalesContractPriceWithDetailsAsync(Core.Entities.SalesContract contract)
    {
        var result = new PriceCalculationResult
        {
            CalculationDate = DateTime.UtcNow,
            BenchmarkUsed = contract.PriceFormula?.IndexName ?? "Fixed"
        };

        if (contract.PriceFormula == null)
        {
            throw new InvalidOperationException("Contract has no pricing formula");
        }

        if (contract.PriceFormula.IsFixedPrice)
        {
            result.FinalPrice = contract.PriceFormula.BasePrice?.Amount ?? 0;
            result.CalculationMethod = "Fixed Price";
            result.CalculationDetails["FixedPrice"] = result.FinalPrice;
            return result;
        }

        if (!contract.PricingPeriodStart.HasValue || !contract.PricingPeriodEnd.HasValue)
        {
            throw new InvalidOperationException("Pricing period is required for floating price contracts");
        }

        // Similar logic to purchase contract but with sales-specific adjustments
        var formula = contract.PriceFormula.Formula;
        result.CalculationMethod = contract.PriceFormula.Method.ToString();

        if (formula.Contains("AVG(") && formula.Contains(")"))
        {
            var benchmarkStart = formula.IndexOf("AVG(") + 4;
            var benchmarkEnd = formula.IndexOf(")", benchmarkStart);
            var benchmarkName = formula.Substring(benchmarkStart, benchmarkEnd - benchmarkStart);

            result.DailyPrices = await GetDailyPricesAsync(benchmarkName, 
                contract.PricingPeriodStart.Value, 
                contract.PricingPeriodEnd.Value);

            result.BusinessDaysUsed = result.DailyPrices.Length;
            var avgPrice = result.DailyPrices.Average();

            var premium = contract.Premium?.Amount ?? 0;
            var discount = contract.Discount?.Amount ?? 0;

            result.FinalPrice = avgPrice + premium - discount;

            result.CalculationDetails["BenchmarkAverage"] = avgPrice;
            result.CalculationDetails["Premium"] = premium;
            result.CalculationDetails["Discount"] = discount;
            result.CalculationDetails["PricingPeriodStart"] = contract.PricingPeriodStart.Value;
            result.CalculationDetails["PricingPeriodEnd"] = contract.PricingPeriodEnd.Value;

            return result;
        }

        throw new NotSupportedException($"Pricing formula not supported: {formula}");
    }

    private static string? ExtractFuturesContract(string formula)
    {
        // Extract futures contract from formula
        // e.g., "AVG(BRENT) + BASIS(BRENT-2024-03)" -> "BRENT-2024-03"
        var basisStart = formula.IndexOf("BASIS(");
        if (basisStart == -1) return null;

        var contractStart = basisStart + 6;
        var contractEnd = formula.IndexOf(")", contractStart);
        if (contractEnd == -1) return null;

        return formula.Substring(contractStart, contractEnd - contractStart);
    }
}