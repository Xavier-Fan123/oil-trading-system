using OilTrading.Core.ValueObjects;

namespace OilTrading.Application.Services;

public interface IBasisCalculationService
{
    /// <summary>
    /// Calculate basis (spread) between spot and futures prices
    /// </summary>
    Task<decimal> CalculateBasisAsync(string productType, DateTime valuationDate, string futuresContract);
    
    /// <summary>
    /// Calculate basis for multiple contracts
    /// </summary>
    Task<Dictionary<string, decimal>> CalculateMultipleBasisAsync(string productType, DateTime valuationDate, string[] futuresContracts);
    
    /// <summary>
    /// Get basis history for analysis
    /// </summary>
    Task<BasisHistoryDto[]> GetBasisHistoryAsync(string productType, string futuresContract, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Calculate basis-adjusted price using futures price + basis
    /// </summary>
    Task<decimal> CalculateBasisAdjustedPriceAsync(decimal futuresPrice, string productType, DateTime valuationDate, string futuresContract);
    
    /// <summary>
    /// Validate if basis is within expected range
    /// </summary>
    Task<BasisValidationResult> ValidateBasisAsync(string productType, decimal calculatedBasis, DateTime valuationDate);
}

public class BasisHistoryDto
{
    public DateTime Date { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string FuturesContract { get; set; } = string.Empty;
    public decimal SpotPrice { get; set; }
    public decimal FuturesPrice { get; set; }
    public decimal Basis { get; set; }
    public decimal BasisPercentage { get; set; }
}

public class BasisValidationResult
{
    public bool IsValid { get; set; }
    public decimal CalculatedBasis { get; set; }
    public decimal ExpectedBasisMin { get; set; }
    public decimal ExpectedBasisMax { get; set; }
    public decimal HistoricalAverage { get; set; }
    public decimal StandardDeviation { get; set; }
    public string? ValidationMessage { get; set; }
    public BasisRiskLevel RiskLevel { get; set; }
}

public enum BasisRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}