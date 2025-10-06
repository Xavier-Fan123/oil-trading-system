using Microsoft.Extensions.Logging;
using OilTrading.Application.Services;
using OilTrading.Application.DTOs;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.TransactionOperations;

public class RiskLimitCheckOperation : ITransactionOperation
{
    private readonly IRealTimeRiskMonitoringService _riskMonitoringService;
    private readonly ILogger<RiskLimitCheckOperation> _logger;

    private RiskCheckResult? _riskCheckResult;
    private bool _limitTemporarilyIncreased = false;
    private Guid? _contractId;

    public string OperationName => "RiskLimitCheck";
    public int Order { get; set; } = 3;
    public bool RequiresCompensation => true;

    public RiskLimitCheckOperation(
        IRealTimeRiskMonitoringService riskMonitoringService,
        ILogger<RiskLimitCheckOperation> logger)
    {
        _riskMonitoringService = riskMonitoringService;
        _logger = logger;
    }

    public async Task<OperationResult> ExecuteAsync(TransactionContext context)
    {
        _logger.LogInformation("Executing risk limit check operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // Get contract ID from context
            if (context.Data.TryGetValue("ContractId", out var contractIdObj))
            {
                _contractId = (Guid)contractIdObj;
            }

            if (!_contractId.HasValue)
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Contract ID not found in transaction context"
                };
            }

            // Get contract details for risk assessment
            var contractData = await GetContractRiskDataAsync(_contractId.Value, context);
            if (contractData == null)
            {
                return new OperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Unable to retrieve contract data for risk assessment"
                };
            }

            // Perform risk limit checks
            _riskCheckResult = await PerformRiskChecksAsync(contractData);

            if (!_riskCheckResult.PassesAllChecks)
            {
                // Log the risk violations
                foreach (var violation in _riskCheckResult.Violations)
                {
                    _logger.LogWarning("Risk limit violation for contract {ContractId}: {Violation}", 
                        _contractId, violation);
                }

                // Check if we can auto-approve with temporary limit increase
                if (_riskCheckResult.CanAutoApprove && context.Data.ContainsKey("AllowRiskOverride"))
                {
                    var overrideResult = await TemporaryLimitIncreaseAsync(contractData);
                    if (overrideResult.IsSuccessful)
                    {
                        _limitTemporarilyIncreased = true;
                        _logger.LogInformation("Temporarily increased risk limits for contract {ContractId} in transaction {TransactionId}", 
                            _contractId, context.TransactionId);
                    }
                    else
                    {
                        return new OperationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Risk limit violations detected and cannot be overridden: {string.Join(", ", _riskCheckResult.Violations)}"
                        };
                    }
                }
                else
                {
                    return new OperationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Risk limit violations detected: {string.Join(", ", _riskCheckResult.Violations)}"
                    };
                }
            }

            _logger.LogInformation("Risk limit checks passed for contract {ContractId} in transaction {TransactionId}", 
                _contractId, context.TransactionId);

            return new OperationResult
            {
                IsSuccess = true,
                Data = new Dictionary<string, object>
                {
                    ["RiskScore"] = _riskCheckResult.OverallRiskScore,
                    ["ViolationCount"] = _riskCheckResult.Violations.Count,
                    ["TemporaryLimitIncrease"] = _limitTemporarilyIncreased
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing risk limit check operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OperationResult> CompensateAsync(TransactionContext context)
    {
        _logger.LogInformation("Compensating risk limit check operation for transaction {TransactionId}", context.TransactionId);

        try
        {
            // If we temporarily increased limits, revert them
            if (_limitTemporarilyIncreased && _contractId.HasValue)
            {
                var revertResult = await RevertTemporaryLimitIncreaseAsync(_contractId.Value);
                if (!revertResult.IsSuccessful)
                {
                    _logger.LogError("Failed to revert temporary risk limit increase for contract {ContractId}: {Error}", 
                        _contractId, revertResult.ErrorMessage);
                    
                    return new OperationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = revertResult.ErrorMessage ?? "Failed to revert temporary risk limit increase"
                    };
                }

                _logger.LogInformation("Reverted temporary risk limit increase for contract {ContractId} in compensation for transaction {TransactionId}", 
                    _contractId, context.TransactionId);
            }

            return new OperationResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compensating risk limit check operation in transaction {TransactionId}", context.TransactionId);
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<ContractRiskData?> GetContractRiskDataAsync(Guid contractId, TransactionContext context)
    {
        // Extract contract risk data from context or fetch from repositories
        if (context.Data.TryGetValue("ContractType", out var contractTypeObj) &&
            context.Data.TryGetValue("ContractData", out var contractDataObj))
        {
            var contractType = contractTypeObj.ToString();
            
            if (contractType?.Equals("Purchase", StringComparison.OrdinalIgnoreCase) == true &&
                contractDataObj is PurchaseContractCreationData purchaseData)
            {
                return new ContractRiskData
                {
                    ContractId = contractId,
                    ContractType = "Purchase",
                    ProductId = purchaseData.ProductId,
                    SupplierId = purchaseData.SupplierId,
                    ContractValue = CalculateContractValue(purchaseData),
                    Quantity = purchaseData.ContractQuantity,
                    DeliveryStartDate = purchaseData.DeliveryStartDate,
                    DeliveryEndDate = purchaseData.DeliveryEndDate
                };
            }
            else if (contractType?.Equals("Sales", StringComparison.OrdinalIgnoreCase) == true &&
                     contractDataObj is SalesContractCreationData salesData)
            {
                return new ContractRiskData
                {
                    ContractId = contractId,
                    ContractType = "Sales",
                    ProductId = salesData.ProductId,
                    CustomerId = salesData.CustomerId,
                    ContractValue = CalculateContractValue(salesData),
                    Quantity = salesData.ContractQuantity,
                    DeliveryStartDate = salesData.DeliveryStartDate,
                    DeliveryEndDate = salesData.DeliveryEndDate
                };
            }
        }

        return null;
    }

    private async Task<RiskCheckResult> PerformRiskChecksAsync(ContractRiskData contractData)
    {
        var result = new RiskCheckResult
        {
            ContractId = contractData.ContractId,
            OverallRiskScore = 0
        };

        // Check 1: Contract value limits
        await CheckContractValueLimitsAsync(contractData, result);

        // Check 2: Counterparty exposure limits
        await CheckCounterpartyExposureLimitsAsync(contractData, result);

        // Check 3: Product concentration limits
        await CheckProductConcentrationLimitsAsync(contractData, result);

        // Check 4: Portfolio VaR limits
        await CheckPortfolioVaRLimitsAsync(contractData, result);

        // Check 5: Tenor limits
        await CheckTenorLimitsAsync(contractData, result);

        // Calculate overall risk score
        result.OverallRiskScore = CalculateOverallRiskScore(result);

        // Determine if violations can be auto-approved
        result.CanAutoApprove = DetermineAutoApprovalEligibility(result);

        result.PassesAllChecks = !result.Violations.Any();

        return result;
    }

    private async Task CheckContractValueLimitsAsync(ContractRiskData contractData, RiskCheckResult result)
    {
        // Check single contract value limit
        const decimal singleContractLimit = 10_000_000m; // $10M
        if (contractData.ContractValue > singleContractLimit)
        {
            result.Violations.Add($"Contract value {contractData.ContractValue:C} exceeds single contract limit {singleContractLimit:C}");
        }
    }

    private async Task CheckCounterpartyExposureLimitsAsync(ContractRiskData contractData, RiskCheckResult result)
    {
        // Check counterparty exposure limit
        var counterpartyId = contractData.SupplierId ?? contractData.CustomerId;
        if (counterpartyId.HasValue)
        {
            // In a real implementation, this would check current exposure + new contract value
            const decimal counterpartyLimit = 50_000_000m; // $50M
            var currentExposure = await GetCounterpartyExposureAsync(counterpartyId.Value);
            var newTotalExposure = currentExposure + contractData.ContractValue;

            if (newTotalExposure > counterpartyLimit)
            {
                result.Violations.Add($"Counterparty exposure {newTotalExposure:C} exceeds limit {counterpartyLimit:C}");
            }
        }
    }

    private async Task CheckProductConcentrationLimitsAsync(ContractRiskData contractData, RiskCheckResult result)
    {
        // Check product concentration limit
        const decimal productConcentrationLimit = 0.3m; // 30%
        var productConcentration = await GetProductConcentrationAsync(contractData.ProductId, contractData.ContractValue);

        if (productConcentration > productConcentrationLimit)
        {
            result.Violations.Add($"Product concentration {productConcentration:P} exceeds limit {productConcentrationLimit:P}");
        }
    }

    private async Task CheckPortfolioVaRLimitsAsync(ContractRiskData contractData, RiskCheckResult result)
    {
        // Check portfolio VaR impact
        const decimal varLimit = 100_000m; // $100K VaR limit
        var currentVaR = await _riskMonitoringService.GetRealTimeVaRAsync();
        var estimatedVaRIncrease = EstimateVaRIncrease(contractData);

        if (currentVaR.VaR95 + estimatedVaRIncrease > varLimit)
        {
            result.Violations.Add($"Portfolio VaR {currentVaR.VaR95 + estimatedVaRIncrease:C} exceeds limit {varLimit:C}");
        }
    }

    private async Task CheckTenorLimitsAsync(ContractRiskData contractData, RiskCheckResult result)
    {
        // Check delivery period tenor
        var tenor = contractData.DeliveryEndDate - contractData.DeliveryStartDate;
        const int maxTenorDays = 365; // 1 year maximum

        if (tenor.TotalDays > maxTenorDays)
        {
            result.Violations.Add($"Contract tenor {tenor.TotalDays} days exceeds limit {maxTenorDays} days");
        }
    }

    private decimal CalculateOverallRiskScore(RiskCheckResult result)
    {
        // Simple risk scoring: more violations = higher risk score
        return result.Violations.Count * 10m + (result.Violations.Count > 0 ? 50m : 0m);
    }

    private bool DetermineAutoApprovalEligibility(RiskCheckResult result)
    {
        // Allow auto-approval for minor violations only
        return result.Violations.Count <= 1 && result.OverallRiskScore <= 60m;
    }

    private async Task<RiskOverrideResult> TemporaryLimitIncreaseAsync(ContractRiskData contractData)
    {
        // Implementation would temporarily increase relevant risk limits
        // This is a placeholder for the actual implementation
        _logger.LogInformation("Implementing temporary risk limit increase for contract {ContractId}", contractData.ContractId);
        
        return new RiskOverrideResult { IsSuccessful = true };
    }

    private async Task<RiskOverrideResult> RevertTemporaryLimitIncreaseAsync(Guid contractId)
    {
        // Implementation would revert the temporary limit increases
        _logger.LogInformation("Reverting temporary risk limit increase for contract {ContractId}", contractId);
        
        return new RiskOverrideResult { IsSuccessful = true };
    }

    private decimal CalculateContractValue(PurchaseContractCreationData data)
    {
        // Simplified contract value calculation
        if (data.PriceFormula.IsFixedPrice && data.PriceFormula.BasePrice != null)
        {
            return data.PriceFormula.BasePrice.Amount * data.ContractQuantity.Value;
        }
        
        // For floating prices, use an estimated value
        return 75m * data.ContractQuantity.Value; // $75 per unit estimate
    }

    private decimal CalculateContractValue(SalesContractCreationData data)
    {
        // Simplified contract value calculation
        if (data.PriceFormula.IsFixedPrice && data.PriceFormula.BasePrice != null)
        {
            return data.PriceFormula.BasePrice.Amount * data.ContractQuantity.Value;
        }
        
        // For floating prices, use an estimated value
        return 75m * data.ContractQuantity.Value; // $75 per unit estimate
    }

    private async Task<decimal> GetCounterpartyExposureAsync(Guid counterpartyId)
    {
        // Placeholder - would query actual counterparty exposure
        return Random.Shared.Next(1_000_000, 10_000_000);
    }

    private async Task<decimal> GetProductConcentrationAsync(Guid productId, decimal newContractValue)
    {
        // Placeholder - would calculate actual product concentration
        return (decimal)(Random.Shared.NextSingle() * 0.4f); // 0-40% concentration
    }

    private decimal EstimateVaRIncrease(ContractRiskData contractData)
    {
        // Simplified VaR increase estimation
        return contractData.ContractValue * 0.02m; // 2% of contract value
    }
}

// Supporting classes
public class ContractRiskData
{
    public Guid ContractId { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal ContractValue { get; set; }
    public Quantity Quantity { get; set; } = null!;
    public DateTime DeliveryStartDate { get; set; }
    public DateTime DeliveryEndDate { get; set; }
}

public class RiskCheckResult
{
    public Guid ContractId { get; set; }
    public bool PassesAllChecks { get; set; }
    public List<string> Violations { get; set; } = new();
    public decimal OverallRiskScore { get; set; }
    public bool CanAutoApprove { get; set; }
}

public class RiskOverrideResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}