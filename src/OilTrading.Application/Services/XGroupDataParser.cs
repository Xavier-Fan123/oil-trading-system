namespace OilTrading.Application.Services;

/// <summary>
/// Parser for X-group format market data (Merged_Futures_Spot_Data.xlsx).
/// Handles the 7-column format with combined futures and spot price data.
/// </summary>
public static class XGroupDataParser
{
    /// <summary>
    /// Column indices for X-group Excel format (0-based).
    /// </summary>
    public static class ColumnIndex
    {
        public const int ContractSpecificationId = 0;  // 合约细则ID
        public const int ContractDescription = 1;       // 合约细则描述
        public const int PriceDate = 2;                 // 报价日期
        public const int SettlementPrice = 3;           // 结算价
        public const int SpotPrice = 4;                 // 现货价格
        public const int Unit = 5;                      // 报价单位
        public const int Currency = 6;                  // 报价货币
    }

    /// <summary>
    /// Chinese column header names for X-group format.
    /// </summary>
    public static class ChineseHeaders
    {
        public const string ContractSpecificationId = "合约细则ID";
        public const string ContractDescription = "合约细则描述";
        public const string PriceDate = "报价日期";
        public const string SettlementPrice = "结算价";
        public const string SpotPrice = "现货价格";
        public const string Unit = "报价单位";
        public const string Currency = "报价货币";
    }

    /// <summary>
    /// Represents parsed result from a contract specification ID.
    /// </summary>
    public class ParsedContractId
    {
        public string ProductCode { get; set; } = string.Empty;
        public string? ContractMonth { get; set; }
        public bool IsFutures { get; set; }
        public string OriginalId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a single parsed row from X-group format.
    /// </summary>
    public class ParsedXGroupRow
    {
        public string ContractSpecificationId { get; set; } = string.Empty;
        public string ContractDescription { get; set; } = string.Empty;
        public DateTime PriceDate { get; set; }
        public decimal? SettlementPrice { get; set; }
        public decimal? SpotPrice { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";

        // Parsed fields
        public string ProductCode { get; set; } = string.Empty;
        public string? ContractMonth { get; set; }
        public bool IsFutures { get; set; }
    }

    /// <summary>
    /// Month name abbreviations for contract month parsing.
    /// </summary>
    private static readonly Dictionary<string, int> MonthNameToNumber = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Jan", 1 }, { "Feb", 2 }, { "Mar", 3 }, { "Apr", 4 },
        { "May", 5 }, { "Jun", 6 }, { "Jul", 7 }, { "Aug", 8 },
        { "Sep", 9 }, { "Oct", 10 }, { "Nov", 11 }, { "Dec", 12 }
    };

    private static readonly Dictionary<int, string> MonthNumberToName = new()
    {
        { 1, "Jan" }, { 2, "Feb" }, { 3, "Mar" }, { 4, "Apr" },
        { 5, "May" }, { 6, "Jun" }, { 7, "Jul" }, { 8, "Aug" },
        { 9, "Sep" }, { 10, "Oct" }, { 11, "Nov" }, { 12, "Dec" }
    };

    /// <summary>
    /// Parse a contract specification ID to extract product code and contract month.
    /// </summary>
    /// <param name="contractSpecId">The contract specification ID (e.g., "SG380 Apr26" or "SG380")</param>
    /// <returns>Parsed contract ID with product code and optional contract month</returns>
    /// <example>
    /// "SG380 Apr26" -> ProductCode: "SG380", ContractMonth: "Apr26", IsFutures: true
    /// "SG380" -> ProductCode: "SG380", ContractMonth: null, IsFutures: false
    /// "GO 10ppm Feb26" -> ProductCode: "GO 10ppm", ContractMonth: "Feb26", IsFutures: true
    /// "GO 10ppm" -> ProductCode: "GO 10ppm", ContractMonth: null, IsFutures: false
    /// "Brt Fut Mar26" -> ProductCode: "Brt Fut", ContractMonth: "Mar26", IsFutures: true
    /// </example>
    public static ParsedContractId ParseContractSpecificationId(string contractSpecId)
    {
        if (string.IsNullOrWhiteSpace(contractSpecId))
        {
            return new ParsedContractId { OriginalId = contractSpecId ?? string.Empty };
        }

        var trimmedId = contractSpecId.Trim();
        var result = new ParsedContractId { OriginalId = trimmedId };

        // Try to find a contract month pattern at the end (e.g., "Apr26", "Feb27")
        // Pattern: 3-letter month + 2-digit year
        var parts = trimmedId.Split(' ');

        if (parts.Length >= 2)
        {
            var lastPart = parts[^1];

            // Check if last part is a contract month (e.g., "Apr26")
            if (IsContractMonth(lastPart))
            {
                result.ContractMonth = lastPart;
                result.IsFutures = true;
                // Product code is everything except the last part
                result.ProductCode = string.Join(" ", parts[..^1]);
            }
            else
            {
                // No contract month found, entire string is product code
                result.ProductCode = trimmedId;
                result.IsFutures = false;
            }
        }
        else
        {
            // Single part - just product code (spot)
            result.ProductCode = trimmedId;
            result.IsFutures = false;
        }

        return result;
    }

    /// <summary>
    /// Check if a string matches the contract month pattern (e.g., "Apr26").
    /// </summary>
    private static bool IsContractMonth(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 4 || value.Length > 5)
            return false;

        // First 3 characters should be a month abbreviation
        var monthPart = value[..3];
        if (!MonthNameToNumber.ContainsKey(monthPart))
            return false;

        // Remaining characters should be numeric (year)
        var yearPart = value[3..];
        return int.TryParse(yearPart, out var year) && year >= 0 && year <= 99;
    }

    /// <summary>
    /// Normalize contract month to ISO format (e.g., "Apr26" -> "2026-04").
    /// </summary>
    /// <param name="contractMonth">Contract month in X-group format (e.g., "Apr26")</param>
    /// <returns>ISO formatted month (e.g., "2026-04") or null if invalid</returns>
    public static string? NormalizeContractMonth(string? contractMonth)
    {
        if (string.IsNullOrEmpty(contractMonth) || contractMonth.Length < 4)
            return null;

        var monthPart = contractMonth[..3];
        var yearPart = contractMonth[3..];

        if (!MonthNameToNumber.TryGetValue(monthPart, out var month))
            return null;

        if (!int.TryParse(yearPart, out var year))
            return null;

        // Convert 2-digit year to 4-digit (assume 2000s for years < 50, 1900s otherwise)
        var fullYear = year < 50 ? 2000 + year : 1900 + year;

        return $"{fullYear:0000}-{month:00}";
    }

    /// <summary>
    /// Convert ISO format month to X-group format (e.g., "2026-04" -> "Apr26").
    /// </summary>
    public static string? ToXGroupContractMonth(string? isoMonth)
    {
        if (string.IsNullOrEmpty(isoMonth))
            return null;

        var parts = isoMonth.Split('-');
        if (parts.Length != 2)
            return null;

        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
            return null;

        if (month < 1 || month > 12)
            return null;

        var monthName = MonthNumberToName[month];
        var yearSuffix = (year % 100).ToString("00");

        return $"{monthName}{yearSuffix}";
    }

    /// <summary>
    /// Parse unit string to determine if it's barrels or metric tonnes.
    /// </summary>
    public static Core.Entities.MarketPriceUnit? ParseUnit(string? unit)
    {
        if (string.IsNullOrEmpty(unit))
            return null;

        return unit.ToUpperInvariant() switch
        {
            "BBLS" or "BBL" or "BARRELS" => Core.Entities.MarketPriceUnit.BBL,
            "MT" or "TONNES" or "TON" or "METRIC TONNES" => Core.Entities.MarketPriceUnit.MT,
            _ => null
        };
    }

    /// <summary>
    /// Validate that a row has the expected X-group format.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateXGroupRow(ParsedXGroupRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ContractSpecificationId))
            return (false, "Contract Specification ID is required");

        if (row.PriceDate == default)
            return (false, "Price Date is required");

        if (!row.SettlementPrice.HasValue && !row.SpotPrice.HasValue)
            return (false, "Either Settlement Price or Spot Price must be provided");

        if (row.SettlementPrice.HasValue && row.SettlementPrice <= 0)
            return (false, "Settlement Price must be positive");

        if (row.SpotPrice.HasValue && row.SpotPrice <= 0)
            return (false, "Spot Price must be positive");

        if (string.IsNullOrWhiteSpace(row.Unit))
            return (false, "Unit is required");

        return (true, null);
    }

    /// <summary>
    /// Check if an Excel file appears to be in X-group format based on headers.
    /// </summary>
    public static bool IsXGroupFormat(IEnumerable<string> headers)
    {
        var headerList = headers.ToList();

        // Check for Chinese headers (primary detection)
        var hasChineseHeaders = headerList.Any(h => h.Contains("合约细则")) ||
                                headerList.Any(h => h.Contains("结算价")) ||
                                headerList.Any(h => h.Contains("现货价格"));

        if (hasChineseHeaders)
            return true;

        // Check for English equivalents
        var requiredEnglishHeaders = new[] { "ProductID", "Settlement", "Spot", "Unit", "Currency" };
        var matchCount = requiredEnglishHeaders.Count(req =>
            headerList.Any(h => h.Equals(req, StringComparison.OrdinalIgnoreCase)));

        return matchCount >= 4;
    }

    /// <summary>
    /// Get the effective price from a parsed row (Settlement for futures, Spot for spot).
    /// </summary>
    public static decimal? GetEffectivePrice(ParsedXGroupRow row)
    {
        return row.IsFutures ? row.SettlementPrice : row.SpotPrice;
    }

    /// <summary>
    /// Get all X-group product codes.
    /// </summary>
    public static IReadOnlyList<string> GetKnownProductCodes()
    {
        return new[]
        {
            "Brt Fut",    // Brent Crude Oil Futures
            "GO 10ppm",   // Gasoil 10ppm
            "MF 0.5",     // Marine Fuel 0.5%
            "SG180",      // Singapore Fuel Oil 180cst
            "SG380"       // Singapore Fuel Oil 380cst
        };
    }

    /// <summary>
    /// Get product display name from product code.
    /// </summary>
    public static string GetProductDisplayName(string productCode)
    {
        return productCode switch
        {
            "Brt Fut" => "ICE Brent Future (USD/BBLS)",
            "GO 10ppm" => "Gas oil 10ppm (USD/BBLS)",
            "MF 0.5" => "MOPS Marine Fuel 0.5% (USD/MT)",
            "SG180" => "MOPS FO 180cst FOB Sg (USD/MT)",
            "SG380" => "MOPS FO 380cst FOB Sg (USD/MT)",
            _ => productCode
        };
    }
}
