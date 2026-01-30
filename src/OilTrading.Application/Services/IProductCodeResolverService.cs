namespace OilTrading.Application.Services;

/// <summary>
/// Professional product code resolution service for international oil trading system.
/// Handles bidirectional mapping between database codes and API codes with market type awareness.
/// Follows industry standards from top-tier trading companies (Vitol, Trafigura, Glencore).
/// </summary>
public interface IProductCodeResolverService
{
    /// <summary>
    /// Resolves database product code to API product code with market type and region awareness.
    /// </summary>
    /// <param name="databaseCode">Internal database product code (e.g., "BRENT", "HFO380")</param>
    /// <param name="marketType">Market type: "Physical Spot" or "Exchange Futures"</param>
    /// <param name="region">Geographic region (optional, for spot markets)</param>
    /// <returns>API product code (e.g., "BRENT_CRUDE", "BUNKER_SPORE") or null if not found</returns>
    /// <example>
    /// ResolveToAPICode("HFO380", "Physical Spot", "Singapore") → "BUNKER_SPORE"
    /// ResolveToAPICode("HFO380", "Physical Spot", "Rotterdam") → "FUEL_OIL_35_RTDM"
    /// ResolveToAPICode("GASOIL", "Exchange Futures", null) → "GASOIL_FUTURES"
    /// </example>
    string? ResolveToAPICode(string databaseCode, string marketType, string? region = null);

    /// <summary>
    /// Resolves API product code to database product code.
    /// </summary>
    /// <param name="apiCode">API product code (e.g., "BRENT_CRUDE", "BUNKER_SPORE")</param>
    /// <returns>Database product code (e.g., "BRENT", "HFO380") or null if not found</returns>
    /// <example>
    /// ResolveToDBCode("BRENT_CRUDE") → "BRENT"
    /// ResolveToDBCode("BUNKER_SPORE") → "HFO380"
    /// ResolveToDBCode("GASOIL_FUTURES") → "GASOIL"
    /// </example>
    string? ResolveToDBCode(string apiCode);

    /// <summary>
    /// Gets all available geographic regions for a database product code.
    /// </summary>
    /// <param name="databaseCode">Internal database product code</param>
    /// <returns>List of available regions (e.g., ["Singapore", "Rotterdam", "Hong Kong"])</returns>
    /// <example>
    /// GetAvailableRegions("HFO380") → ["Singapore", "Hong Kong", "Rotterdam"]
    /// GetAvailableRegions("BRENT") → ["North Sea"]
    /// GetAvailableRegions("WTI") → [] (futures only, no physical regions)
    /// </example>
    IEnumerable<string> GetAvailableRegions(string databaseCode);

    /// <summary>
    /// Gets professional display name for a database product code.
    /// </summary>
    /// <param name="databaseCode">Internal database product code</param>
    /// <returns>Professional display name (e.g., "Brent Crude", "HSFO 380 CST") or null</returns>
    string? GetDisplayName(string databaseCode);

    /// <summary>
    /// Gets asset class for a database product code.
    /// </summary>
    /// <param name="databaseCode">Internal database product code</param>
    /// <returns>Asset class (e.g., "Crude Oil", "Middle Distillates", "Heavy Residuals")</returns>
    string? GetAssetClass(string databaseCode);

    /// <summary>
    /// Checks if a database product has physical spot markets.
    /// </summary>
    /// <param name="databaseCode">Internal database product code</param>
    /// <returns>True if product has spot markets, false otherwise</returns>
    bool HasSpotMarkets(string databaseCode);

    /// <summary>
    /// Checks if a database product has exchange futures markets.
    /// </summary>
    /// <param name="databaseCode">Internal database product code</param>
    /// <returns>True if product has futures markets, false otherwise</returns>
    bool HasFuturesMarkets(string databaseCode);

    /// <summary>
    /// Gets all database product codes in the product registry.
    /// </summary>
    /// <returns>List of all database product codes</returns>
    IEnumerable<string> GetAllDatabaseCodes();

    /// <summary>
    /// Resolves product code with prefix matching support for base products.
    /// Supports queries like "BRENT" matching "BRENT_JAN25", "BRENT_FEB25", etc.
    /// </summary>
    /// <param name="productCodePrefix">Product code or prefix</param>
    /// <returns>List of matching API product codes</returns>
    IEnumerable<string> ResolveWithPrefixMatching(string productCodePrefix);
}
