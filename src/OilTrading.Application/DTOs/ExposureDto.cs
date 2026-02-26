namespace OilTrading.Application.DTOs;

/// <summary>
/// Represents a single row from the exposure table (敞口表) Excel upload.
/// </summary>
public class ExposureRowDto
{
    /// <summary>品种 (Product Code): e.g., SG180, SG380, MF 0.5, GO 10ppm, Brt Fut</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>合约月份 (Contract Month): e.g., "Apr26", "May26". Optional for spot-only rows.</summary>
    public string? ContractMonth { get; set; }

    /// <summary>现货持仓 (Spot Position): positive = long, negative = short, 0 = no position</summary>
    public decimal SpotPosition { get; set; }

    /// <summary>期货持仓 (Futures Position): positive = long, negative = short, 0 = no position</summary>
    public decimal FuturesPosition { get; set; }

    /// <summary>单位 (Unit): MT or BBL</summary>
    public string Unit { get; set; } = "MT";
}

/// <summary>
/// A single position derived from an exposure row, split into spot or futures.
/// </summary>
public class ExposurePositionDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string? ContractMonth { get; set; }
    public decimal Quantity { get; set; }
    public string PositionType { get; set; } = "Spot"; // "Spot" or "Futures"
    public string Unit { get; set; } = "MT";
}

/// <summary>
/// Complete result of exposure upload + VaR/CVaR calculation.
/// </summary>
public class ExposureVaRResultDto
{
    public DateTime CalculationDate { get; set; }
    public decimal TotalPositionValue { get; set; }

    // Portfolio VaR
    public decimal PortfolioVar1Day95 { get; set; }
    public decimal PortfolioVar1Day99 { get; set; }
    public decimal PortfolioVar10Day95 { get; set; }
    public decimal PortfolioVar10Day99 { get; set; }

    // Portfolio CVaR (Expected Shortfall)
    public decimal PortfolioCVaR1Day95 { get; set; }
    public decimal PortfolioCVaR1Day99 { get; set; }
    public decimal PortfolioCVaR10Day95 { get; set; }
    public decimal PortfolioCVaR10Day99 { get; set; }

    // Per-position breakdown
    public List<ExposurePositionVaRDto> PositionVaRs { get; set; } = new();

    // Upload metadata
    public int RowsParsed { get; set; }
    public int PositionsProcessed { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<ExposureRowDto> ParsedExposures { get; set; } = new();
}

/// <summary>
/// VaR/CVaR breakdown for a single position from the exposure table.
/// </summary>
public class ExposurePositionVaRDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string? ContractMonth { get; set; }
    public string PositionType { get; set; } = string.Empty; // "Spot" or "Futures"
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "MT";
    public decimal LatestPrice { get; set; }
    public decimal PositionValue { get; set; }
    public decimal DailyVolatility { get; set; }
    public decimal Var1Day95 { get; set; }
    public decimal Var1Day99 { get; set; }
    public decimal Var10Day95 { get; set; }
    public decimal Var10Day99 { get; set; }
    public decimal CVaR1Day95 { get; set; }
    public decimal CVaR1Day99 { get; set; }
    public decimal CVaR10Day95 { get; set; }
    public decimal CVaR10Day99 { get; set; }
}
