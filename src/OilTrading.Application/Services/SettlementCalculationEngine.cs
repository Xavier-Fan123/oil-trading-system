using OilTrading.Core.Entities;
using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

/// <summary>
/// Shared calculation engine for settlement amount calculations.
/// Handles:
/// - Benchmark price calculations (MT and BBL)
/// - Adjustment price calculations
/// - Cargo value calculations
/// - Total settlement amount with charges
/// - Quantity conversions between MT and BBL
/// </summary>
public class SettlementCalculationEngine
{
    /// <summary>
    /// Calculates benchmark amount from quantity and price
    /// Formula: CalculationQuantity * BenchmarkPrice (converted to appropriate unit)
    /// </summary>
    public decimal CalculateBenchmarkAmount(
        decimal benchmarkPrice,
        decimal quantityMT,
        decimal quantityBBL,
        QuantityUnit contractUnit,
        string priceUnit = "MT")
    {
        try
        {
            // Convert price unit if necessary
            decimal effectivePrice = benchmarkPrice;

            // Select quantity based on contract unit and price unit
            decimal quantity = contractUnit == QuantityUnit.MT ? quantityMT : quantityBBL;

            // If price is per BBL but quantity is MT, convert
            if (priceUnit == "BBL" && contractUnit == QuantityUnit.MT)
            {
                const decimal defaultTonBarrelRatio = 7.6m; // Standard MT to BBL conversion
                quantity = quantityMT * defaultTonBarrelRatio;
            }
            else if (priceUnit == "MT" && contractUnit == QuantityUnit.BBL)
            {
                const decimal defaultTonBarrelRatio = 7.6m;
                quantity = quantityBBL / defaultTonBarrelRatio;
            }

            return RoundToTwoDecimals(quantity * effectivePrice);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error calculating benchmark amount. Benchmark Price: {benchmarkPrice}, " +
                $"Quantity MT: {quantityMT}, Quantity BBL: {quantityBBL}, " +
                $"Contract Unit: {contractUnit}, Price Unit: {priceUnit}", ex);
        }
    }

    /// <summary>
    /// Calculates adjustment amount (premium/discount applied to cargo)
    /// Formula: CargoValue * AdjustmentPercentage or FixedAmount
    /// </summary>
    public decimal CalculateAdjustmentAmount(
        decimal benchmarkAmount,
        decimal? adjustmentPercentage = null,
        decimal? adjustmentFixedAmount = null)
    {
        try
        {
            if (adjustmentFixedAmount.HasValue)
            {
                return RoundToTwoDecimals(adjustmentFixedAmount.Value);
            }

            if (adjustmentPercentage.HasValue)
            {
                return RoundToTwoDecimals(benchmarkAmount * (adjustmentPercentage.Value / 100m));
            }

            return 0m;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error calculating adjustment amount. Benchmark Amount: {benchmarkAmount}, " +
                $"Adjustment %: {adjustmentPercentage}, Fixed Amount: {adjustmentFixedAmount}", ex);
        }
    }

    /// <summary>
    /// Calculates cargo value (subtotal before charges)
    /// Formula: BenchmarkAmount + AdjustmentAmount
    /// </summary>
    public decimal CalculateCargoValue(decimal benchmarkAmount, decimal adjustmentAmount)
    {
        try
        {
            return RoundToTwoDecimals(benchmarkAmount + adjustmentAmount);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error calculating cargo value. Benchmark: {benchmarkAmount}, Adjustment: {adjustmentAmount}", ex);
        }
    }

    /// <summary>
    /// Calculates total settlement amount including charges
    /// Formula: CargoValue + TotalCharges
    /// </summary>
    public decimal CalculateTotalSettlementAmount(decimal cargoValue, decimal totalCharges)
    {
        try
        {
            return RoundToTwoDecimals(cargoValue + totalCharges);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error calculating total settlement. Cargo Value: {cargoValue}, Total Charges: {totalCharges}", ex);
        }
    }

    /// <summary>
    /// Applies exchange rate conversion to settlement amount
    /// Formula: SettlementAmount * ExchangeRate
    /// </summary>
    public decimal ApplyExchangeRate(decimal settlementAmount, decimal exchangeRate)
    {
        try
        {
            if (exchangeRate <= 0)
            {
                throw new ArgumentException("Exchange rate must be greater than zero", nameof(exchangeRate));
            }

            return RoundToTwoDecimals(settlementAmount * exchangeRate);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error applying exchange rate. Amount: {settlementAmount}, Rate: {exchangeRate}", ex);
        }
    }

    /// <summary>
    /// Converts quantity between MT and BBL using the contract's ton-barrel ratio
    /// </summary>
    public (decimal QuantityMT, decimal QuantityBBL) ConvertQuantities(
        decimal originalQuantity,
        QuantityUnit originalUnit,
        decimal tonBarrelRatio = 7.6m)
    {
        try
        {
            if (tonBarrelRatio <= 0)
            {
                throw new ArgumentException("Ton-barrel ratio must be greater than zero", nameof(tonBarrelRatio));
            }

            if (originalUnit == QuantityUnit.MT)
            {
                decimal quantityBBL = RoundToSixDecimals(originalQuantity * tonBarrelRatio);
                return (originalQuantity, quantityBBL);
            }
            else
            {
                decimal quantityMT = RoundToSixDecimals(originalQuantity / tonBarrelRatio);
                return (quantityMT, originalQuantity);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error converting quantities. Original: {originalQuantity} {originalUnit}, Ratio: {tonBarrelRatio}", ex);
        }
    }

    /// <summary>
    /// Validates settlement calculation completeness
    /// Returns validation errors if any required fields are missing
    /// </summary>
    public List<string> ValidateCalculationCompletion(
        decimal actualQuantityMT,
        decimal actualQuantityBBL,
        decimal benchmarkPrice,
        decimal? benchmarkAmount = null,
        IReadOnlyList<SettlementCharge>? charges = null)
    {
        var errors = new List<string>();

        // Check quantities
        if (actualQuantityMT == 0 && actualQuantityBBL == 0)
        {
            errors.Add("Actual quantities must be provided (either MT or BBL)");
        }

        // Check benchmark price
        if (benchmarkPrice <= 0)
        {
            errors.Add("Benchmark price must be greater than zero");
        }

        // Check benchmark amount calculated
        if (benchmarkAmount.HasValue && benchmarkAmount <= 0)
        {
            errors.Add("Benchmark amount must be calculated and greater than zero");
        }

        // Check charges if collection provided
        if (charges != null && charges.Count > 0)
        {
            foreach (var charge in charges)
            {
                if (charge.Amount < 0)
                {
                    errors.Add($"Charge '{charge.Description}' has negative amount");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Calculates settlement variance (difference between actual and calculated quantities)
    /// Useful for quality control and reconciliation
    /// </summary>
    public decimal CalculateQuantityVariance(decimal actualQuantity, decimal calculatedQuantity)
    {
        try
        {
            if (calculatedQuantity == 0)
            {
                return 0m;
            }

            decimal variance = ((actualQuantity - calculatedQuantity) / calculatedQuantity) * 100m;
            return RoundToTwoDecimals(variance);
        }
        catch (DivideByZeroException ex)
        {
            throw new InvalidOperationException(
                "Cannot calculate variance with zero calculated quantity", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error calculating quantity variance. Actual: {actualQuantity}, Calculated: {calculatedQuantity}", ex);
        }
    }

    /// <summary>
    /// Calculates average cost per unit (useful for reporting)
    /// Formula: TotalAmount / Quantity
    /// </summary>
    public decimal CalculateAverageCostPerUnit(decimal totalAmount, decimal quantity, QuantityUnit unit)
    {
        try
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
            }

            decimal costPerUnit = totalAmount / quantity;
            return RoundToFourDecimals(costPerUnit);
        }
        catch (DivideByZeroException ex)
        {
            throw new InvalidOperationException("Cannot calculate average cost per unit with zero quantity", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error calculating average cost per unit. Total: {totalAmount}, Quantity: {quantity} {unit}", ex);
        }
    }

    /// <summary>
    /// Rounds decimal to 2 decimal places (standard for currency)
    /// </summary>
    private decimal RoundToTwoDecimals(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Rounds decimal to 4 decimal places (for pricing)
    /// </summary>
    private decimal RoundToFourDecimals(decimal value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Rounds decimal to 6 decimal places (for quantity)
    /// </summary>
    private decimal RoundToSixDecimals(decimal value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
