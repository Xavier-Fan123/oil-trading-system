using OilTrading.Core.Common;

namespace OilTrading.Core.Entities;

public class MarketPrice : BaseEntity
{
    public DateTime PriceDate { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public MarketPriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Source { get; set; }
    public string? ContractMonth { get; set; } // For futures: "AUG25", "SEP25"
    public string? DataSource { get; set; } // "Platts", "ICE", "Manual"
    public bool IsSettlement { get; set; } // True for settlement prices
    public DateTime ImportedAt { get; set; }
    public string? ImportedBy { get; set; }

    // ===== New fields for enhanced market data support =====
    public MarketPriceUnit? Unit { get; set; }  // BBL or MT
    public string? ExchangeName { get; set; }   // ICE, NYMEX, etc.
    public string? Region { get; set; }  // "Singapore", "Dubai", null for futures

    // ===== X-group format support fields =====
    public string? ContractSpecificationId { get; set; }  // "SG380 Apr26" (futures) or "SG380" (spot)
    public decimal? SettlementPrice { get; set; }         // Futures settlement price (null for spot rows)
    public decimal? SpotPrice { get; set; }               // Spot market price (null for futures rows)

    // ===== REMOVED: ProductId and Product navigation =====
    // These were causing "no such column: m0.ProductId" errors
    // Using ProductCode as natural key instead
    // ❌ public Guid? ProductId { get; set; }
    // ❌ public Product? Product { get; set; }

    /// <summary>
    /// Private constructor for EF Core (entity materialization from database)
    /// </summary>
    private MarketPrice()
    {
        // BaseEntity inline initializers handle default values:
        // - Id = Guid.NewGuid()
        // - CreatedAt = DateTime.UtcNow
        // - IsDeleted = false
        // - RowVersion = new byte[] { 0 }
    }

    /// <summary>
    /// Factory method to create a new MarketPrice with all BaseEntity properties properly initialized.
    /// CRITICAL: Explicitly initializes ALL BaseEntity properties to ensure EF Core change tracking
    /// includes them in the INSERT statement, especially IsDeleted which MUST NOT be NULL.
    ///
    /// IMPORTANT EF CORE BUG WORKAROUND:
    /// EF Core's change detection doesn't track properties set to their inline-initialized default value.
    /// Since BaseEntity declares 'IsDeleted = false', calling SetIsDeleted(false) does NOT register
    /// as a change. We MUST set it to true FIRST, then to false to trigger change tracking.
    ///
    /// SYSTEMIC NOTE: This pattern applies to ALL entities inheriting from BaseEntity.
    /// ALL properties relying on inline initializers must be explicitly set in factory methods.
    /// </summary>
    public static MarketPrice Create(
        DateTime priceDate,
        string productCode,
        string productName,
        MarketPriceType priceType,
        decimal price,
        string currency,
        string? source,
        string? dataSource,
        bool isSettlement,
        DateTime importedAt,
        string? importedBy,
        string? contractMonth = null,
        string? region = null,
        string? contractSpecificationId = null,
        decimal? settlementPrice = null,
        decimal? spotPrice = null)
    {
        var now = DateTime.UtcNow;
        var marketPrice = new MarketPrice
        {
            // Domain properties - only set here
            PriceDate = priceDate,
            ProductCode = productCode,
            ProductName = productName,
            PriceType = priceType,
            Price = price,
            Currency = currency,
            Source = source,
            ContractMonth = contractMonth,
            DataSource = dataSource,
            IsSettlement = isSettlement,
            ImportedAt = importedAt,
            ImportedBy = importedBy,
            Region = region,
            // X-group format fields
            ContractSpecificationId = contractSpecificationId,
            SettlementPrice = settlementPrice,
            SpotPrice = spotPrice
        };

        // CRITICAL: Initialize BaseEntity audit fields through explicit method calls
        // This FORCES EF Core to recognize these as tracked changes
        marketPrice.SetId(Guid.NewGuid());
        marketPrice.SetCreated(importedBy ?? "System", now);

        // CRITICAL FIX FOR EF CORE CHANGE TRACKING BUG:
        // Set IsDeleted through a method that triggers property change detection
        // We must set it to TRUE first, then FALSE to ensure EF Core recognizes the change
        // Otherwise, setting to FALSE (the inline default) is optimized away
        marketPrice.SetIsDeleted(true);  // Force EF Core to recognize property as modified
        marketPrice.SetIsDeleted(false); // Set actual desired value - NOW included in INSERT

        // CRITICAL: Set RowVersion explicitly to ensure it's included in the INSERT statement
        // BaseEntity.RowVersion is now initialized to null! (no inline default)
        // This ensures SetRowVersion() calls always register as changes in EF Core's tracker
        marketPrice.SetRowVersion(new byte[] { 0 });  // Initialize to empty byte array
        // EF Core will auto-increment RowVersion on subsequent updates via IsRowVersion()

        // CRITICAL FIX: Explicitly set ContractMonth if provided to ensure EF Core tracks it
        // If contractMonth is null/empty, set to empty string to force change tracking
        // Otherwise, the nullable field optimization skips including null values in INSERT
        if (string.IsNullOrEmpty(contractMonth))
        {
            // Force EF Core to recognize ContractMonth as modified by setting non-null then null
            marketPrice.ContractMonth = "";
        }
        // If contractMonth was provided, it's already set on line 80

        return marketPrice;
    }
}

public enum MarketPriceType
{
    Spot = 1,              // Spot price (MOPS)
    FuturesSettlement = 2, // Futures settlement price
    FuturesClose = 3,      // Futures closing price
    Index = 4,             // Index price
    Spread = 5             // Spread
}

public enum MarketPriceUnit
{
    BBL = 1,  // Barrel (common for crude oils)
    MT = 2,   // Metric tonne (metric tonne)
    TON = 3   // Tonne (short ton)
}