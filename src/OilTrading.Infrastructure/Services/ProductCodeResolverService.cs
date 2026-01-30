using OilTrading.Application.Services;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Professional product code resolver service implementation.
/// Maintains complete product registry following international oil trading industry standards.
/// Based on terminology and practices from Vitol, Trafigura, Glencore, Shell Trading, BP Trading.
/// </summary>
public class ProductCodeResolverService : IProductCodeResolverService
{
    // Product registry: Single source of truth for all product mappings
    private static readonly Dictionary<string, ProductMapping> ProductRegistry = new()
    {
        // CRUDE OIL - Light Sweet Crude
        ["BRENT"] = new ProductMapping
        {
            DatabaseCode = "BRENT",
            DisplayName = "Brent Crude",
            AssetClass = "Crude Oil",
            Category = "Light Sweet Crude",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "BRENT_CRUDE", Region = "North Sea", Source = "Platts", AssessmentType = "Dated Brent" }
            },
            FuturesMarket = new FuturesMarket
            {
                ProductCode = "BRENT",
                Exchange = "ICE",
                ContractSize = "1000 BBL",
                TickSize = "$0.01/BBL",
                TradingHours = "01:00-23:00 UTC"
            },
            Specifications = new ProductSpecs
            {
                ApiGravity = 38.0,
                SulfurContent = 0.37
            },
            Unit = "BBL"
        },

        ["WTI"] = new ProductMapping
        {
            DatabaseCode = "WTI",
            DisplayName = "WTI Crude",
            AssetClass = "Crude Oil",
            Category = "Light Sweet Crude",
            SpotMarkets = new List<SpotMarket>(),
            FuturesMarket = new FuturesMarket
            {
                ProductCode = "WTI",
                Exchange = "NYMEX",
                ContractSize = "1000 BBL",
                TickSize = "$0.01/BBL",
                TradingHours = "18:00-17:00 ET (Sun-Fri)"
            },
            Specifications = new ProductSpecs
            {
                ApiGravity = 39.6,
                SulfurContent = 0.24
            },
            Unit = "BBL"
        },

        // MIDDLE DISTILLATES - Gasoil/Diesel
        ["GASOIL"] = new ProductMapping
        {
            DatabaseCode = "GASOIL",
            DisplayName = "Gasoil 0.1% S",
            AssetClass = "Middle Distillates",
            Category = "Ultra Low Sulfur Diesel",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "MOPS_GASOIL", Region = "Singapore", Source = "MOPS", AssessmentType = "0.05% S Gasoil" }
            },
            FuturesMarket = new FuturesMarket
            {
                ProductCode = "GASOIL_FUTURES",
                Exchange = "ICE",
                ContractSize = "100 MT",
                TickSize = "$0.25/MT",
                TradingHours = "01:00-23:00 UTC"
            },
            Specifications = new ProductSpecs
            {
                SulfurContent = 0.05,
                CetaneIndex = 51.0
            },
            Unit = "MT"
        },

        ["MGO"] = new ProductMapping
        {
            DatabaseCode = "MGO",
            DisplayName = "Marine Gas Oil",
            AssetClass = "Middle Distillates",
            Category = "Marine Gas Oil",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "MGO", Region = "Singapore", Source = "MOPS", AssessmentType = "MGO 0.5% S" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                SulfurContent = 0.5,
                Viscosity = "11 CST @ 40°C",
                FlashPoint = 60.0
            },
            Unit = "MT"
        },

        // LIGHT PRODUCTS - Aviation Fuel
        ["JET"] = new ProductMapping
        {
            DatabaseCode = "JET",
            DisplayName = "Jet Fuel (Kerosene)",
            AssetClass = "Light Products",
            Category = "Aviation Fuel",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "JET_FUEL", Region = "Singapore", Source = "MOPS", AssessmentType = "Jet Kerosene" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                FlashPoint = 38.0
            },
            Unit = "BBL"
        },

        // LIGHT PRODUCTS - Motor Gasoline
        ["GASOLINE_92"] = new ProductMapping
        {
            DatabaseCode = "GASOLINE_92",
            DisplayName = "Gasoline 92 RON",
            AssetClass = "Light Products",
            Category = "Motor Gasoline",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "GASOLINE_92", Region = "Singapore", Source = "MOPS", AssessmentType = "92 RON Unl" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                OctaneRating = 92
            },
            Unit = "BBL"
        },

        ["GASOLINE_95"] = new ProductMapping
        {
            DatabaseCode = "GASOLINE_95",
            DisplayName = "Gasoline 95 RON",
            AssetClass = "Light Products",
            Category = "Motor Gasoline",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "GASOLINE_95", Region = "Singapore", Source = "MOPS", AssessmentType = "95 RON Unl" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                OctaneRating = 95
            },
            Unit = "BBL"
        },

        ["GASOLINE_97"] = new ProductMapping
        {
            DatabaseCode = "GASOLINE_97",
            DisplayName = "Gasoline 97 RON",
            AssetClass = "Light Products",
            Category = "Motor Gasoline",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "GASOLINE_97", Region = "Singapore", Source = "MOPS", AssessmentType = "97 RON Unl" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                OctaneRating = 97
            },
            Unit = "BBL"
        },

        // HEAVY RESIDUALS - High Sulfur Fuel Oil
        ["HFO380"] = new ProductMapping
        {
            DatabaseCode = "HFO380",
            DisplayName = "HSFO 380 CST",
            AssetClass = "Heavy Residuals",
            Category = "High Sulfur Fuel Oil",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "BUNKER_SPORE", Region = "Singapore", Source = "MOPS", AssessmentType = "380 CST 3.5% S" },
                new() { ProductCode = "BUNKER_HK", Region = "Hong Kong", Source = "MOPS", AssessmentType = "380 CST 3.5% S" },
                new() { ProductCode = "FUEL_OIL_35_RTDM", Region = "Rotterdam", Source = "Platts", AssessmentType = "380 CST 3.5% S" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                Viscosity = "380 CST @ 50°C",
                SulfurContent = 3.5
            },
            Unit = "MT"
        },

        ["HSFO"] = new ProductMapping
        {
            DatabaseCode = "HSFO",
            DisplayName = "HSFO 380 CST",
            AssetClass = "Heavy Residuals",
            Category = "High Sulfur Fuel Oil",
            SpotMarkets = new List<SpotMarket>
            {
                new() { ProductCode = "BUNKER_SPORE", Region = "Singapore", Source = "MOPS", AssessmentType = "380 CST 3.5% S" }
            },
            FuturesMarket = null,
            Specifications = new ProductSpecs
            {
                Viscosity = "380 CST @ 50°C",
                SulfurContent = 3.5
            },
            Unit = "MT"
        }
    };

    // Reverse mapping: API code → Database code
    private static readonly Dictionary<string, string> ApiToDatabase = new()
    {
        // Crude Oil
        ["BRENT_CRUDE"] = "BRENT",
        ["BRENT"] = "BRENT",
        ["ICE_BRENT"] = "BRENT",
        ["WTI"] = "WTI",

        // Middle Distillates
        ["GASOIL_FUTURES"] = "GASOIL",
        ["MOPS_GASOIL"] = "GASOIL",
        ["MGO"] = "MGO",
        ["JET_FUEL"] = "JET",

        // Light Products
        ["GASOLINE_92"] = "GASOLINE_92",
        ["GASOLINE_95"] = "GASOLINE_95",
        ["GASOLINE_97"] = "GASOLINE_97",

        // Heavy Residuals
        ["BUNKER_SPORE"] = "HFO380",
        ["BUNKER_HK"] = "HFO380",
        ["FUEL_OIL_35_RTDM"] = "HFO380",
        ["HSFO"] = "HFO380"
    };

    public string? ResolveToAPICode(string databaseCode, string marketType, string? region = null)
    {
        if (string.IsNullOrEmpty(databaseCode))
            return null;

        if (!ProductRegistry.TryGetValue(databaseCode, out var product))
            return null;

        if (marketType == "Physical Spot" || marketType == "Spot")
        {
            if (product.SpotMarkets == null || product.SpotMarkets.Count == 0)
                return null;

            if (!string.IsNullOrEmpty(region))
            {
                var spotMarket = product.SpotMarkets.FirstOrDefault(m => m.Region == region);
                return spotMarket?.ProductCode;
            }

            // Return first spot market if no region specified
            return product.SpotMarkets[0].ProductCode;
        }
        else if (marketType == "Exchange Futures" || marketType == "Futures")
        {
            return product.FuturesMarket?.ProductCode;
        }

        return null;
    }

    public string? ResolveToDBCode(string apiCode)
    {
        if (string.IsNullOrEmpty(apiCode))
            return null;

        return ApiToDatabase.TryGetValue(apiCode, out var dbCode) ? dbCode : null;
    }

    public IEnumerable<string> GetAvailableRegions(string databaseCode)
    {
        if (string.IsNullOrEmpty(databaseCode))
            return Enumerable.Empty<string>();

        if (!ProductRegistry.TryGetValue(databaseCode, out var product))
            return Enumerable.Empty<string>();

        if (product.SpotMarkets == null || product.SpotMarkets.Count == 0)
            return Enumerable.Empty<string>();

        return product.SpotMarkets.Select(m => m.Region);
    }

    public string? GetDisplayName(string databaseCode)
    {
        if (string.IsNullOrEmpty(databaseCode))
            return null;

        return ProductRegistry.TryGetValue(databaseCode, out var product) ? product.DisplayName : null;
    }

    public string? GetAssetClass(string databaseCode)
    {
        if (string.IsNullOrEmpty(databaseCode))
            return null;

        return ProductRegistry.TryGetValue(databaseCode, out var product) ? product.AssetClass : null;
    }

    public bool HasSpotMarkets(string databaseCode)
    {
        if (string.IsNullOrEmpty(databaseCode))
            return false;

        if (!ProductRegistry.TryGetValue(databaseCode, out var product))
            return false;

        return product.SpotMarkets != null && product.SpotMarkets.Count > 0;
    }

    public bool HasFuturesMarkets(string databaseCode)
    {
        if (string.IsNullOrEmpty(databaseCode))
            return false;

        if (!ProductRegistry.TryGetValue(databaseCode, out var product))
            return false;

        return product.FuturesMarket != null;
    }

    public IEnumerable<string> GetAllDatabaseCodes()
    {
        return ProductRegistry.Keys;
    }

    public IEnumerable<string> ResolveWithPrefixMatching(string productCodePrefix)
    {
        if (string.IsNullOrEmpty(productCodePrefix))
            return Enumerable.Empty<string>();

        var results = new List<string>();

        // Try exact database code match first
        if (ProductRegistry.TryGetValue(productCodePrefix, out var product))
        {
            // Add all spot market codes
            if (product.SpotMarkets != null)
            {
                results.AddRange(product.SpotMarkets.Select(m => m.ProductCode));
            }

            // Add futures market code
            if (product.FuturesMarket != null)
            {
                results.Add(product.FuturesMarket.ProductCode);
            }
        }

        // Also check API codes that start with the prefix
        var matchingApiCodes = ApiToDatabase.Keys
            .Where(apiCode => apiCode.StartsWith(productCodePrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        results.AddRange(matchingApiCodes);

        return results.Distinct();
    }

    // Internal data models
    private class ProductMapping
    {
        public string DatabaseCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AssetClass { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<SpotMarket>? SpotMarkets { get; set; }
        public FuturesMarket? FuturesMarket { get; set; }
        public ProductSpecs Specifications { get; set; } = new();
        public string Unit { get; set; } = string.Empty;
    }

    private class SpotMarket
    {
        public string ProductCode { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = string.Empty;
    }

    private class FuturesMarket
    {
        public string ProductCode { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string ContractSize { get; set; } = string.Empty;
        public string? TickSize { get; set; }
        public string? TradingHours { get; set; }
    }

    private class ProductSpecs
    {
        public double? ApiGravity { get; set; }
        public double? SulfurContent { get; set; }
        public string? Viscosity { get; set; }
        public double? OctaneRating { get; set; }
        public double? FlashPoint { get; set; }
        public double? CetaneIndex { get; set; }
    }
}
