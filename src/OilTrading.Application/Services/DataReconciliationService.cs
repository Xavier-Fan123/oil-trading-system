using Microsoft.Extensions.Logging;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Services;

/// <summary>
/// Service for data reconciliation and consistency checking between modules
/// </summary>
public class DataReconciliationService
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IFuturesDealRepository _futuresDealRepository;
    private readonly IPaperContractRepository _paperContractRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly ILogger<DataReconciliationService> _logger;
    
    public DataReconciliationService(
        IMarketDataRepository marketDataRepository,
        IFuturesDealRepository futuresDealRepository,
        IPaperContractRepository paperContractRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        ILogger<DataReconciliationService> logger)
    {
        _marketDataRepository = marketDataRepository;
        _futuresDealRepository = futuresDealRepository;
        _paperContractRepository = paperContractRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// Reconcile futures deals with paper contracts
    /// </summary>
    public async Task<DataReconciliationResult> ReconcileFuturesWithPaperContracts(
        DateTime reconciliationDate,
        CancellationToken cancellationToken = default)
    {
        var result = new DataReconciliationResult
        {
            ReconciliationDate = reconciliationDate,
            Module1 = "FuturesDeals",
            Module2 = "PaperContracts"
        };
        
        try
        {
            // Get futures positions
            var futuresPositions = await _futuresDealRepository.GetPositionsByProductAsync(cancellationToken);
            
            // Get paper contract positions
            var paperContracts = await _paperContractRepository.GetOpenPositionsAsync(cancellationToken);
            var paperPositions = paperContracts
                .GroupBy(p => $"{p.ProductType}_{p.ContractMonth}")
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.Position == PositionType.Long ? p.Quantity : -p.Quantity)
                );
            
            // Compare positions
            foreach (var futuresPos in futuresPositions)
            {
                var productKey = futuresPos.Key;
                var futuresQty = futuresPos.Value;
                
                if (paperPositions.TryGetValue(productKey, out var paperQty))
                {
                    var difference = Math.Abs(futuresQty - paperQty);
                    
                    if (difference > 0.01m) // Allow small rounding differences
                    {
                        result.Discrepancies.Add(new ReconciliationDiscrepancy
                        {
                            Key = productKey,
                            Module1Value = futuresQty.ToString("F2"),
                            Module2Value = paperQty.ToString("F2"),
                            Difference = difference.ToString("F2"),
                            Description = $"Position mismatch for {productKey}: Futures={futuresQty:F2}, Paper={paperQty:F2}"
                        });
                    }
                    else
                    {
                        result.MatchedItems++;
                    }
                    
                    paperPositions.Remove(productKey);
                }
                else
                {
                    result.Discrepancies.Add(new ReconciliationDiscrepancy
                    {
                        Key = productKey,
                        Module1Value = futuresQty.ToString("F2"),
                        Module2Value = "0",
                        Difference = futuresQty.ToString("F2"),
                        Description = $"Futures position {productKey} not found in paper contracts"
                    });
                }
            }
            
            // Check remaining paper positions
            foreach (var paperPos in paperPositions)
            {
                result.Discrepancies.Add(new ReconciliationDiscrepancy
                {
                    Key = paperPos.Key,
                    Module1Value = "0",
                    Module2Value = paperPos.Value.ToString("F2"),
                    Difference = paperPos.Value.ToString("F2"),
                    Description = $"Paper contract position {paperPos.Key} not found in futures deals"
                });
            }
            
            result.IsReconciled = result.Discrepancies.Count == 0;
            result.TotalItems = futuresPositions.Count + paperPositions.Count;
            
            _logger.LogInformation(
                "Futures-Paper reconciliation completed. Matched: {Matched}, Discrepancies: {Discrepancies}",
                result.MatchedItems, result.Discrepancies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during futures-paper reconciliation");
            result.Errors.Add($"Reconciliation error: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Reconcile market prices with contract pricing
    /// </summary>
    public async Task<DataReconciliationResult> ReconcileMarketPricesWithContracts(
        DateTime priceDate,
        CancellationToken cancellationToken = default)
    {
        var result = new DataReconciliationResult
        {
            ReconciliationDate = priceDate,
            Module1 = "MarketPrices",
            Module2 = "Contracts"
        };
        
        try
        {
            // Get latest market prices
            var latestPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
            
            // Get active purchase contracts with index-based pricing
            var purchaseContracts = await _purchaseContractRepository.GetAllAsync(cancellationToken);
            var activeContracts = purchaseContracts
                .Where(c => c.Status == ContractStatus.Active && 
                           c.PriceFormula != null &&
                           !string.IsNullOrEmpty(c.PriceFormula.IndexName))
                .ToList();
            
            // Check if required prices exist for active contracts
            foreach (var contract in activeContracts)
            {
                var indexName = contract.PriceFormula.IndexName;
                var marketPrice = latestPrices.FirstOrDefault(p => 
                    p.ProductCode.Equals(indexName, StringComparison.OrdinalIgnoreCase));
                
                if (marketPrice == null)
                {
                    result.Discrepancies.Add(new ReconciliationDiscrepancy
                    {
                        Key = contract.ContractNumber.Value,
                        Module1Value = "Missing",
                        Module2Value = indexName,
                        Description = $"Market price missing for contract {contract.ContractNumber.Value} index: {indexName}"
                    });
                }
                else if ((DateTime.UtcNow - marketPrice.PriceDate).TotalDays > 3)
                {
                    result.Discrepancies.Add(new ReconciliationDiscrepancy
                    {
                        Key = contract.ContractNumber.Value,
                        Module1Value = marketPrice.PriceDate.ToString("yyyy-MM-dd"),
                        Module2Value = indexName,
                        Description = $"Market price stale for contract {contract.ContractNumber.Value}. Last update: {marketPrice.PriceDate:yyyy-MM-dd}"
                    });
                }
                else
                {
                    result.MatchedItems++;
                }
            }
            
            result.TotalItems = activeContracts.Count;
            result.IsReconciled = result.Discrepancies.Count == 0;
            
            _logger.LogInformation(
                "Price-Contract reconciliation completed. Matched: {Matched}, Issues: {Issues}",
                result.MatchedItems, result.Discrepancies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during price-contract reconciliation");
            result.Errors.Add($"Reconciliation error: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Validate data integrity across modules
    /// </summary>
    public async Task<DataIntegrityReport> ValidateDataIntegrity(
        CancellationToken cancellationToken = default)
    {
        var report = new DataIntegrityReport
        {
            CheckDate = DateTime.UtcNow
        };
        
        // Check for duplicate deal numbers
        var deals = await _futuresDealRepository.GetAllAsync(cancellationToken);
        var duplicateDeals = deals
            .GroupBy(d => d.DealNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicateDeals.Any())
        {
            report.Issues.Add(new IntegrityIssue
            {
                Severity = "High",
                Module = "FuturesDeals",
                Issue = $"Duplicate deal numbers found: {string.Join(", ", duplicateDeals.Take(5))}"
            });
        }
        
        // Check for orphaned paper contracts (simplified check as PaperContract doesn't have FuturesDealIds)
        // This would need to be implemented based on actual relationship structure
        var paperContracts = await _paperContractRepository.GetOpenPositionsAsync(cancellationToken);
        var orphanedContracts = new List<PaperContract>(); // Placeholder for now
        
        if (orphanedContracts.Any())
        {
            report.Issues.Add(new IntegrityIssue
            {
                Severity = "Medium",
                Module = "PaperContracts",
                Issue = $"Found {orphanedContracts.Count} paper contracts with invalid futures deal references"
            });
        }
        
        // Check for missing market prices
        var requiredProducts = new[] { "ICE_BRENT", "MOPS_380", "MOPS_180", "IPE_GASOIL" };
        var latestPrices = await _marketDataRepository.GetLatestPricesAsync(cancellationToken);
        
        foreach (var product in requiredProducts)
        {
            var price = latestPrices.FirstOrDefault(p => p.ProductCode == product);
            if (price == null)
            {
                report.Issues.Add(new IntegrityIssue
                {
                    Severity = "High",
                    Module = "MarketPrices",
                    Issue = $"Missing market price for critical product: {product}"
                });
            }
            else if ((DateTime.UtcNow - price.PriceDate).TotalDays > 7)
            {
                report.Issues.Add(new IntegrityIssue
                {
                    Severity = "Medium",
                    Module = "MarketPrices",
                    Issue = $"Stale market price for {product}. Last update: {price.PriceDate:yyyy-MM-dd}"
                });
            }
        }
        
        // Check contract date consistency
        var purchaseContracts = await _purchaseContractRepository.GetAllAsync(cancellationToken);
        var invalidDateContracts = purchaseContracts
            .Where(c => c.PricingPeriodStart != null && 
                       c.PricingPeriodEnd != null && c.PricingPeriodStart > c.PricingPeriodEnd)
            .ToList();
        
        if (invalidDateContracts.Any())
        {
            report.Issues.Add(new IntegrityIssue
            {
                Severity = "High",
                Module = "PurchaseContracts",
                Issue = $"Found {invalidDateContracts.Count} contracts with invalid delivery dates"
            });
        }
        
        report.IsValid = !report.Issues.Any(i => i.Severity == "High");
        report.TotalIssues = report.Issues.Count;
        
        _logger.LogInformation(
            "Data integrity check completed. Issues found: {IssueCount} (High: {HighCount}, Medium: {MediumCount})",
            report.TotalIssues,
            report.Issues.Count(i => i.Severity == "High"),
            report.Issues.Count(i => i.Severity == "Medium"));
        
        return report;
    }
    
    /// <summary>
    /// Auto-fix common data issues
    /// </summary>
    public async Task<AutoFixResult> AutoFixDataIssues(
        CancellationToken cancellationToken = default)
    {
        var result = new AutoFixResult
        {
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // Fix missing total values in futures deals
            var deals = await _futuresDealRepository.GetAllAsync(cancellationToken);
            var dealsToFix = deals.Where(d => d.TotalValue == 0 && d.Quantity > 0 && d.Price > 0).ToList();
            
            foreach (var deal in dealsToFix)
            {
                deal.TotalValue = deal.Quantity * deal.Price;
                await _futuresDealRepository.UpdateAsync(deal, cancellationToken);
                result.FixedItems++;
            }
            
            if (dealsToFix.Any())
            {
                result.Fixes.Add($"Fixed {dealsToFix.Count} futures deals with missing total values");
            }
            
            // Update market prices for futures with current settlement prices
            var openPositions = await _futuresDealRepository.GetOpenPositionsAsync(cancellationToken);
            var productGroups = openPositions.GroupBy(d => new { d.ProductCode, d.ContractMonth });
            
            foreach (var group in productGroups)
            {
                var latestPrice = await _marketDataRepository.GetLatestPriceAsync(
                    $"ICE_{group.Key.ProductCode}_{group.Key.ContractMonth}", 
                    cancellationToken);
                
                if (latestPrice != null)
                {
                    foreach (var deal in group)
                    {
                        deal.CalculateUnrealizedPnL(latestPrice.Price);
                        await _futuresDealRepository.UpdateAsync(deal, cancellationToken);
                    }
                    result.FixedItems += group.Count();
                }
            }
            
            result.EndTime = DateTime.UtcNow;
            result.Success = true;
            
            _logger.LogInformation(
                "Auto-fix completed. Fixed {FixedCount} items in {Duration}ms",
                result.FixedItems,
                (result.EndTime - result.StartTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-fix");
            result.Success = false;
            result.Errors.Add($"Auto-fix error: {ex.Message}");
        }
        
        return result;
    }
}

// Supporting classes for reconciliation results
public class DataReconciliationResult
{
    public DateTime ReconciliationDate { get; set; }
    public string Module1 { get; set; } = string.Empty;
    public string Module2 { get; set; } = string.Empty;
    public bool IsReconciled { get; set; }
    public int TotalItems { get; set; }
    public int MatchedItems { get; set; }
    public List<ReconciliationDiscrepancy> Discrepancies { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ReconciliationDiscrepancy
{
    public string Key { get; set; } = string.Empty;
    public string Module1Value { get; set; } = string.Empty;
    public string Module2Value { get; set; } = string.Empty;
    public string Difference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DataIntegrityReport
{
    public DateTime CheckDate { get; set; }
    public bool IsValid { get; set; }
    public int TotalIssues { get; set; }
    public List<IntegrityIssue> Issues { get; set; } = new();
}

public class IntegrityIssue
{
    public string Severity { get; set; } = string.Empty; // High, Medium, Low
    public string Module { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
}

public class AutoFixResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public int FixedItems { get; set; }
    public List<string> Fixes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}