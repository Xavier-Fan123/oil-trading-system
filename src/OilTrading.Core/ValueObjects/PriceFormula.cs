using OilTrading.Core.Common;
using System.Text.RegularExpressions;

namespace OilTrading.Core.ValueObjects;

public class PriceFormula : ValueObject
{
    public string Formula { get; private set; } = string.Empty;
    public PricingMethod Method { get; private set; }
    public string? IndexName { get; private set; }
    public decimal? FixedPrice { get; private set; }
    public Money? Premium { get; private set; }
    public Money? Discount { get; private set; }
    public string? Currency { get; private set; }
    public string? Unit { get; private set; }
    public int? PricingDays { get; private set; }
    public DateTime? PricingPeriodStart { get; private set; }
    public DateTime? PricingPeriodEnd { get; private set; }
    
    // Mixed-unit pricing support
    public QuantityUnit? BenchmarkUnit { get; private set; }
    public QuantityUnit? AdjustmentUnit { get; private set; }
    public Money? Adjustment { get; private set; }
    public QuantityCalculationMode CalculationMode { get; private set; }
    public decimal? ContractualConversionRatio { get; private set; }
    
    // Additional properties for compatibility
    public bool IsFixedPrice => Method == PricingMethod.Fixed;
    public Money? BasePrice => FixedPrice.HasValue ? new Money(FixedPrice.Value, Currency ?? "USD") : null;

    // Regex patterns for formula parsing
    private static readonly Regex ComplexFormulaRegex = new(
        @"^(?<method>AVG|MIN|MAX|FIRST|LAST|WAVG|MEDIAN|MODE)\((?<index>[^)]+)\)\s*(?<operation>[+\-])\s*(?<adjustment>[\d.]+)\s*(?<currency>[A-Z]{3})(/(?<unit>[A-Z]+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex SimpleIndexFormulaRegex = new(
        @"^(?<index>[A-Z_\s]+)\s*(?<operation>[+\-])\s*(?<adjustment>[\d.]+)\s*(?<currency>[A-Z]{3})(/(?<unit>[A-Z]+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex IndexOnlyRegex = new(
        @"^(?<method>AVG|MIN|MAX|FIRST|LAST|WAVG|MEDIAN|MODE)\((?<index>[^)]+)\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex FixedPriceRegex = new(
        @"^(?<price>[\d.]+)\s*(?<currency>[A-Z]{3})(/(?<unit>[A-Z]+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private PriceFormula() { } // For EF Core

    private PriceFormula(string formula, PricingMethod method, string? indexName = null, 
        decimal? fixedPrice = null, Money? premium = null, Money? discount = null,
        string? currency = null, string? unit = null, int? pricingDays = null,
        QuantityUnit? benchmarkUnit = null, QuantityUnit? adjustmentUnit = null,
        Money? adjustment = null, QuantityCalculationMode calculationMode = QuantityCalculationMode.ActualBLQuantities,
        decimal? contractualConversionRatio = null)
    {
        Formula = formula;
        Method = method;
        IndexName = indexName;
        FixedPrice = fixedPrice;
        Premium = premium;
        Discount = discount;
        Currency = currency;
        Unit = unit;
        PricingDays = pricingDays;
        BenchmarkUnit = benchmarkUnit;
        AdjustmentUnit = adjustmentUnit;
        Adjustment = adjustment;
        CalculationMode = calculationMode;
        ContractualConversionRatio = contractualConversionRatio;
    }

    public static PriceFormula Fixed(decimal price, string currency = "USD", string? unit = null)
    {
        if (price < 0)
            throw new DomainException("Fixed price cannot be negative");

        var formula = unit != null ? $"{price:F2} {currency}/{unit}" : $"{price:F2} {currency}";
        
        return new PriceFormula(
            formula, 
            PricingMethod.Fixed, 
            fixedPrice: price,
            currency: currency,
            unit: unit
        );
    }

    public static PriceFormula Index(string indexName, PricingMethod method = PricingMethod.AVG, 
        Money? adjustment = null, int? pricingDays = null)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            throw new DomainException("Index name cannot be null or empty");

        var formula = $"{method}({indexName})";
        
        if (adjustment != null && !adjustment.IsZero())
        {
            var operation = adjustment.IsPositive() ? "+" : "-";
            var absAmount = Math.Abs(adjustment.Amount);
            var unit = string.IsNullOrEmpty(adjustment.Currency) ? "" : $"/{adjustment.Currency}";
            formula += $" {operation} {absAmount:F2} {adjustment.Currency}{unit}";
        }

        return new PriceFormula(
            formula, 
            method, 
            indexName: indexName,
            premium: adjustment?.IsPositive() == true ? adjustment : null,
            discount: adjustment?.IsNegative() == true ? Money.Dollar(Math.Abs(adjustment.Amount)) : null,
            currency: adjustment?.Currency,
            pricingDays: pricingDays
        );
    }

    public static PriceFormula MixedUnit(
        string indexName,
        PricingMethod method,
        QuantityUnit benchmarkUnit,
        Money? adjustment = null,
        QuantityUnit? adjustmentUnit = null,
        QuantityCalculationMode calculationMode = QuantityCalculationMode.ActualBLQuantities,
        decimal? contractualConversionRatio = null,
        int? pricingDays = null)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            throw new DomainException("Index name cannot be null or empty");

        if (calculationMode == QuantityCalculationMode.ContractualConversion && !contractualConversionRatio.HasValue)
            throw new DomainException("Contractual conversion ratio is required when using ContractualConversion mode");

        if (contractualConversionRatio.HasValue && contractualConversionRatio <= 0)
            throw new DomainException("Contractual conversion ratio must be greater than zero");

        var formula = $"{method}({indexName})@{benchmarkUnit}";
        
        if (adjustment != null && !adjustment.IsZero())
        {
            var operation = adjustment.IsPositive() ? "+" : "";
            var unit = adjustmentUnit?.ToString() ?? benchmarkUnit.ToString();
            formula += $" {operation} {adjustment.Amount:F2} {adjustment.Currency}@{unit}";
        }

        if (calculationMode == QuantityCalculationMode.ContractualConversion)
        {
            formula += $" (Ratio: 1:{contractualConversionRatio:F1})";
        }

        return new PriceFormula(
            formula,
            method,
            indexName: indexName,
            benchmarkUnit: benchmarkUnit,
            adjustmentUnit: adjustmentUnit ?? benchmarkUnit,
            adjustment: adjustment,
            calculationMode: calculationMode,
            contractualConversionRatio: contractualConversionRatio,
            currency: adjustment?.Currency,
            pricingDays: pricingDays
        );
    }

    public static PriceFormula Parse(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new DomainException("Formula cannot be null or empty");

        var trimmed = formula.Trim();

        // Try complex formula: AVG(MOPS FO 380) + 5 USD/MT
        var complexMatch = ComplexFormulaRegex.Match(trimmed);
        if (complexMatch.Success)
        {
            var method = Enum.Parse<PricingMethod>(complexMatch.Groups["method"].Value, true);
            var indexName = complexMatch.Groups["index"].Value.Trim();
            var operation = complexMatch.Groups["operation"].Value;
            var adjustment = decimal.Parse(complexMatch.Groups["adjustment"].Value);
            var currency = complexMatch.Groups["currency"].Value;
            var unit = complexMatch.Groups["unit"].Success ? complexMatch.Groups["unit"].Value : null;

            if (operation == "-")
                adjustment = -adjustment;

            var adjustmentMoney = new Money(adjustment, currency);
            
            return Index(indexName, method, adjustmentMoney);
        }

        // Try simple index formula: MOPS FO 380 + 5 USD/MT
        var simpleMatch = SimpleIndexFormulaRegex.Match(trimmed);
        if (simpleMatch.Success)
        {
            var indexName = simpleMatch.Groups["index"].Value.Trim();
            var operation = simpleMatch.Groups["operation"].Value;
            var adjustment = decimal.Parse(simpleMatch.Groups["adjustment"].Value);
            var currency = simpleMatch.Groups["currency"].Value;
            var unit = simpleMatch.Groups["unit"].Success ? simpleMatch.Groups["unit"].Value : null;

            if (operation == "-")
                adjustment = -adjustment;

            var adjustmentMoney = new Money(adjustment, currency);
            
            return Index(indexName, PricingMethod.AVG, adjustmentMoney);
        }

        // Try index only: AVG(BRENT)
        var indexMatch = IndexOnlyRegex.Match(trimmed);
        if (indexMatch.Success)
        {
            var method = Enum.Parse<PricingMethod>(indexMatch.Groups["method"].Value, true);
            var indexName = indexMatch.Groups["index"].Value.Trim();
            
            return Index(indexName, method);
        }

        // Try fixed price: 75.50 USD/BBL
        var fixedMatch = FixedPriceRegex.Match(trimmed);
        if (fixedMatch.Success)
        {
            var price = decimal.Parse(fixedMatch.Groups["price"].Value);
            var currency = fixedMatch.Groups["currency"].Value;
            var unit = fixedMatch.Groups["unit"].Success ? fixedMatch.Groups["unit"].Value : null;
            
            return Fixed(price, currency, unit);
        }

        // Fallback to custom formula
        return new PriceFormula(trimmed, PricingMethod.Custom);
    }

    public decimal CalculatePrice(Dictionary<string, decimal[]> indexPrices, DateTime? eventDate = null)
    {
        return Method switch
        {
            PricingMethod.Fixed => FixedPrice ?? throw new DomainException("Fixed price not set"),
            PricingMethod.AVG => CalculateIndexPrice(indexPrices, prices => prices.Average()),
            PricingMethod.MIN => CalculateIndexPrice(indexPrices, prices => prices.Min()),
            PricingMethod.MAX => CalculateIndexPrice(indexPrices, prices => prices.Max()),
            PricingMethod.FIRST => CalculateIndexPrice(indexPrices, prices => prices.First()),
            PricingMethod.LAST => CalculateIndexPrice(indexPrices, prices => prices.Last()),
            PricingMethod.WAVG => CalculateIndexPrice(indexPrices, CalculateWeightedAverage),
            PricingMethod.MEDIAN => CalculateIndexPrice(indexPrices, CalculateMedian),
            PricingMethod.MODE => CalculateIndexPrice(indexPrices, CalculateMode),
            PricingMethod.Custom => throw new DomainException("Custom formula calculation not implemented"),
            _ => throw new DomainException($"Unknown pricing method: {Method}")
        };
    }

    private decimal CalculateIndexPrice(Dictionary<string, decimal[]> indexPrices, Func<decimal[], decimal> aggregator)
    {
        if (string.IsNullOrEmpty(IndexName))
            throw new DomainException("Index name not set for index-based pricing");

        if (!indexPrices.TryGetValue(IndexName, out var prices) || prices.Length == 0)
            throw new DomainException($"Index prices not found for: {IndexName}");

        var basePrice = aggregator(prices);
        
        if (Premium != null)
            basePrice += Premium.Amount;
        
        if (Discount != null)
            basePrice -= Discount.Amount;

        return basePrice;
    }

    private static decimal CalculateWeightedAverage(decimal[] prices)
    {
        // Simple implementation - could be enhanced with actual weights
        return prices.Average();
    }

    private static decimal CalculateMedian(decimal[] prices)
    {
        var sorted = prices.OrderBy(x => x).ToArray();
        var mid = sorted.Length / 2;
        
        return sorted.Length % 2 == 0 
            ? (sorted[mid - 1] + sorted[mid]) / 2 
            : sorted[mid];
    }

    private static decimal CalculateMode(decimal[] prices)
    {
        return prices
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }

    public void SetPricingPeriod(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            throw new DomainException("Pricing period start must be before end date");

        PricingPeriodStart = startDate;
        PricingPeriodEnd = endDate;
        PricingDays = (int)(endDate - startDate).TotalDays;
    }

    public bool IsFloatingPrice() => Method != PricingMethod.Fixed;
    
    public bool RequiresPricingPeriod() => IsFloatingPrice() && IndexName != null;

    public bool IsValid()
    {
        try
        {
            return Method switch
            {
                PricingMethod.Fixed => FixedPrice.HasValue && FixedPrice >= 0,
                PricingMethod.Custom => !string.IsNullOrEmpty(Formula),
                _ => !string.IsNullOrEmpty(IndexName)
            };
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Formula;
        yield return Method;
        yield return IndexName ?? string.Empty;
        yield return BenchmarkUnit ?? QuantityUnit.MT;
        yield return AdjustmentUnit ?? QuantityUnit.MT;
        yield return CalculationMode;
        yield return ContractualConversionRatio ?? 0m;
    }

    public override string ToString() => Formula;

    public static implicit operator string(PriceFormula formula) => formula.Formula;
}

public enum PricingMethod
{
    Fixed = 1,
    AVG = 2,     // Average
    MIN = 3,     // Minimum
    MAX = 4,     // Maximum
    FIRST = 5,   // First price in period
    LAST = 6,    // Last price in period
    WAVG = 7,    // Weighted Average
    MEDIAN = 8,  // Median price
    MODE = 9,    // Most frequent price
    Custom = 10  // Custom formula
}

public enum QuantityCalculationMode
{
    ActualBLQuantities = 1,    // Use actual B/L or CQ quantities (MT and BBL)
    ContractualConversion = 2  // Use contractual conversion ratio
}