namespace OilTrading.Application.DTOs;

public class MarketPriceDto
{
    public Guid Id { get; set; }
    public DateTime PriceDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string PriceType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ContractMonth { get; set; }
    public string? DataSource { get; set; }
    public bool IsSettlement { get; set; }
    public DateTime ImportedAt { get; set; }
    public string? ImportedBy { get; set; }
    public string? Region { get; set; }  // "Singapore", "Dubai", null for futures
}

public class MarketPriceListDto
{
    public DateTime PriceDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PriceType { get; set; } = string.Empty;
}

public class UploadMarketDataDto
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "DailyPrices" or "ICESettlement"
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string UploadedBy { get; set; } = string.Empty;
}

public class MarketDataUploadResultDto
{
    public bool Success { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int TotalProcessed { get; set; }
    public int RecordsInserted { get; set; }

    // Date range information for the imported data
    public DateTime? EarliestDate { get; set; }
    public DateTime? LatestDate { get; set; }
    public int UniqueDatesImported { get; set; }
    public int UniqueProductsImported { get; set; }

    public List<string> Messages { get; set; } = new();
    public List<MarketPriceDto> ImportedPrices { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class LatestPricesDto
{
    public DateTime LastUpdateDate { get; set; }
    public List<ProductPriceDto> SpotPrices { get; set; } = new();
    public List<FuturesPriceDto> FuturesPrices { get; set; } = new();
}

public class ProductPriceDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? PreviousPrice { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
    public DateTime PriceDate { get; set; }
    public string? Region { get; set; }  // "Singapore", "Dubai" for spot prices
}

public class FuturesPriceDto
{
    // NEW ARCHITECTURE: Spot and Futures share ProductCode, differentiated by ContractMonth
    public string ProductCode { get; set; } = string.Empty;  // e.g., "BRENT_CRUDE"
    public string ProductName { get; set; } = string.Empty;  // e.g., "Brent Crude Oil"
    public string ContractMonth { get; set; } = string.Empty; // e.g., "2025-08"
    public decimal Price { get; set; }  // Settlement or Close price
    public decimal? PreviousSettlement { get; set; }
    public decimal? Change { get; set; }
    public DateTime PriceDate { get; set; }
    public string? Region { get; set; }  // Usually null for futures (exchange-traded)

    // DEPRECATED: Legacy field for backward compatibility
    [Obsolete("Use ProductCode instead. ProductType is deprecated and will be removed in future versions.")]
    public string ProductType => ProductCode; // Alias for backward compatibility

    [Obsolete("Use Price instead. SettlementPrice is deprecated and will be removed in future versions.")]
    public decimal SettlementPrice => Price; // Alias for backward compatibility

    [Obsolete("Use PriceDate instead. SettlementDate is deprecated and will be removed in future versions.")]
    public DateTime SettlementDate => PriceDate; // Alias for backward compatibility
}

public class DeleteMarketDataResultDto
{
    public bool Success { get; set; }
    public int RecordsDeleted { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}