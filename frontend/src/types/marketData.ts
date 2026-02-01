// Market Data Types

export interface MarketDataUploadResultDto {
  success: boolean;
  recordsProcessed: number;
  recordsInserted: number;
  recordsUpdated: number;
  errors: string[];
  warnings: string[];
  fileType: string;
  fileName: string;
  processingTimeMs: number;
}

export interface LatestPricesDto {
  lastUpdateDate: Date;
  spotPrices: ProductPriceDto[];
  futuresPrices: FuturesPriceDto[];
}

export interface ProductPriceDto {
  productCode: string;
  productName: string;
  price: number;
  previousPrice: number | null;
  change: number | null;
  changePercent: number | null;
  priceDate: Date;
  region?: string;  // "Singapore", "Dubai" for spot prices
}

export interface FuturesPriceDto {
  // NEW ARCHITECTURE: Spot and Futures share productCode, differentiated by contractMonth
  productCode: string;           // e.g., "BRENT_CRUDE"
  productName: string;           // e.g., "Brent Crude Oil"
  contractMonth: string;         // e.g., "2025-08"
  price: number;                 // Settlement or Close price
  previousSettlement: number | null;
  change: number | null;
  priceDate: Date;
  region?: string;               // Usually null for futures (exchange-traded)

  // DEPRECATED: Legacy fields for backward compatibility
  /** @deprecated Use productCode instead */
  productType?: string;
  /** @deprecated Use price instead */
  settlementPrice?: number;
  /** @deprecated Use priceDate instead */
  settlementDate?: Date;
}

export interface MarketPriceDto {
  id: string;
  productCode: string;
  productName: string;
  price: number;
  currency: string;
  priceDate: Date;
  priceType: 'Spot' | 'Futures' | 'Forward';
  contractMonth?: string | null;
  dataSource?: string | null;
  isSettlement: boolean;
  importedAt: Date;
  importedBy?: string | null;
  region?: string | null;
}

export interface MarketDataUploadRequest {
  file: File;
  fileType: 'Spot' | 'Futures';
  overwriteExisting?: boolean;
}

export interface MarketDataFilters {
  productCode?: string;
  startDate?: Date;
  endDate?: Date;
  priceType?: 'Spot' | 'Futures' | 'Forward';
  exchange?: string;
}

export interface ImportResult {
  success: boolean;
  message: string;
  recordsProcessed: number;
  recordsImported: number;
  errors: string[];
  fileName: string;
}

export interface DataImportStatus {
  isImporting: boolean;
  progress: number;
  currentFile: string | null;
  totalFiles: number;
  completedFiles: number;
  errors: string[];
  lastImport: string | null;
}

export interface DeleteMarketDataResultDto {
  success: boolean;
  recordsDeleted: number;
  message: string;
  errors: string[];
}

// File type options for upload - simplified to only Spot and Futures
export const FILE_TYPES = [
  { value: 'Spot', label: 'Spot Prices', description: 'Physical market spot prices' },
  { value: 'Futures', label: 'Futures Prices', description: 'Futures contract prices' },
] as const;

export type FileType = typeof FILE_TYPES[number]['value'];

// Supported file formats
export const SUPPORTED_FORMATS = [
  '.xlsx', '.xls', '.csv'
];

export const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

// ===== PROFESSIONAL OIL TRADING INDUSTRY TYPES =====
// Following standards from: Vitol, Trafigura, Glencore, Shell Trading, BP Trading

/**
 * Asset Class Classification (Industry Standard)
 * Maps to major oil product categories used by trading desks worldwide
 */
export enum AssetClass {
  CrudeOil = "Crude Oil",
  MiddleDistillates = "Middle Distillates",
  LightProducts = "Light Products",
  HeavyResiduals = "Heavy Residuals"
}

/**
 * Product Category (Industry Standard)
 * Detailed product classification following refinery product slate
 */
export enum ProductCategory {
  LightSweetCrude = "Light Sweet Crude",
  HeavySourCrude = "Heavy Sour Crude",
  UltraLowSulfurDiesel = "Ultra Low Sulfur Diesel",
  AviationFuel = "Aviation Fuel",
  MarineGasOil = "Marine Gas Oil",
  MarineFuel = "Marine Fuel",
  MotorGasoline = "Motor Gasoline",
  HighSulfurFuelOil = "High Sulfur Fuel Oil",
  LowSulfurFuelOil = "Low Sulfur Fuel Oil"  // IMO 2020 compliant VLSFO
}

/**
 * Market Type Classification
 * Physical Spot: Regional assessments (MOPS, Platts)
 * Exchange Futures: Listed derivatives (ICE, NYMEX)
 */
export enum MarketType {
  PhysicalSpot = "Physical Spot",
  ExchangeFutures = "Exchange Futures"
}

/**
 * Product Specifications (Industry Standard)
 * Technical characteristics used in contract specifications
 */
export interface ProductSpecs {
  apiGravity?: number;        // API degree for crude oil (Light >34°, Medium 25-34°, Heavy <25°)
  sulfurContent?: number;     // Sulfur % (Sweet <0.5%, Sour >0.5%)
  viscosity?: string;         // CST @ 50°C (e.g., "380 CST @ 50°C")
  octaneRating?: number;      // RON for gasoline (92/95/97 RON)
  flashPoint?: number;        // °C for marine fuels
  cetaneIndex?: number;       // For diesel/gasoil
}

/**
 * Spot Market Configuration
 * Regional physical market assessments
 */
export interface SpotMarketConfig {
  productCode: string;        // API product code (e.g., "BRENT_CRUDE", "BUNKER_SPORE")
  region: string;             // Geographic market (Singapore, Rotterdam, Hong Kong, North Sea)
  source: string;             // Price assessment agency (MOPS, Platts, Argus)
  assessmentType: string;     // Assessment methodology (Dated Brent, MOPS Average, etc.)
}

/**
 * Futures Market Configuration
 * Exchange-traded derivatives
 */
export interface FuturesMarketConfig {
  productCode: string;        // API product code (e.g., "BRENT", "WTI", "GASOIL_FUTURES")
  exchange: string;           // Exchange (ICE, NYMEX, DME)
  contractSize: string;       // Lot size (e.g., "1000 BBL", "100 MT")
  tickSize?: string;          // Minimum price movement
  tradingHours?: string;      // Exchange trading hours (UTC)
}

/**
 * Product Code Mapping (Database → API)
 * Bidirectional mapping between internal database codes and external API codes
 */
export interface ProductCodeMapping {
  databaseCode: string;       // Internal database code (BRENT, WTI, HFO380, etc.)
  displayName: string;        // Professional display name for UI
  assetClass: AssetClass;     // Asset class classification
  category: ProductCategory;  // Detailed product category
  markets: {
    spot?: SpotMarketConfig[];      // Physical spot markets (0-3 regions)
    futures?: FuturesMarketConfig;  // Exchange futures (0-1 exchange)
  };
  specifications: ProductSpecs;     // Technical specifications
  unit: "BBL" | "MT" | "GAL";      // Base unit of measure
}

/**
 * PROFESSIONAL PRODUCT REGISTRY
 * Complete mapping of all database products to API codes and market configurations
 *
 * Data Sources:
 * - Database: DataSeeder.cs (BRENT, WTI, MGO, HFO380)
 * - Database: PostgreSQLDataSeeder.cs (BRENT, WTI, MGO, HSFO, JET, GASOIL)
 * - API Response: /api/market-data/latest (BRENT_CRUDE, BUNKER_*, FUEL_OIL_*, GASOIL_*, GASOLINE_*)
 */
export const PRODUCT_REGISTRY: Record<string, ProductCodeMapping> = {
  // ===== CRUDE OIL =====

  "BRENT": {
    databaseCode: "BRENT",
    displayName: "Brent Crude",
    assetClass: AssetClass.CrudeOil,
    category: ProductCategory.LightSweetCrude,
    markets: {
      spot: [{
        productCode: "BRENT_CRUDE",
        region: "North Sea",
        source: "Platts",
        assessmentType: "Dated Brent"
      }],
      futures: {
        productCode: "BRENT",
        exchange: "ICE",
        contractSize: "1000 BBL",
        tickSize: "$0.01/BBL",
        tradingHours: "01:00-23:00 UTC"
      }
    },
    specifications: {
      apiGravity: 38.0,
      sulfurContent: 0.37
    },
    unit: "BBL"
  },

  "WTI": {
    databaseCode: "WTI",
    displayName: "WTI Crude",
    assetClass: AssetClass.CrudeOil,
    category: ProductCategory.LightSweetCrude,
    markets: {
      futures: {
        productCode: "WTI",
        exchange: "NYMEX",
        contractSize: "1000 BBL",
        tickSize: "$0.01/BBL",
        tradingHours: "18:00-17:00 EST"
      }
    },
    specifications: {
      apiGravity: 39.6,
      sulfurContent: 0.24
    },
    unit: "BBL"
  },

  // ===== MIDDLE DISTILLATES =====

  "GASOIL": {
    databaseCode: "GASOIL",
    displayName: "Gasoil 0.1% S",
    assetClass: AssetClass.MiddleDistillates,
    category: ProductCategory.UltraLowSulfurDiesel,
    markets: {
      spot: [{
        productCode: "MOPS_GASOIL",
        region: "Singapore",
        source: "MOPS",
        assessmentType: "0.05% S Gasoil"
      }],
      futures: {
        productCode: "GASOIL_FUTURES",
        exchange: "ICE",
        contractSize: "100 MT",
        tickSize: "$0.25/MT"
      }
    },
    specifications: {
      sulfurContent: 0.1,
      cetaneIndex: 46
    },
    unit: "MT"
  },

  "MGO": {
    databaseCode: "MGO",
    displayName: "Marine Gas Oil",
    assetClass: AssetClass.MiddleDistillates,
    category: ProductCategory.MarineGasOil,
    markets: {
      spot: [{
        productCode: "MGO",
        region: "Singapore",
        source: "MOPS",
        assessmentType: "MGO 0.5% S"
      }]
    },
    specifications: {
      sulfurContent: 0.5,
      flashPoint: 60
    },
    unit: "MT"
  },

  "JET": {
    databaseCode: "JET",
    displayName: "Jet Fuel (Kerosene)",
    assetClass: AssetClass.MiddleDistillates,
    category: ProductCategory.AviationFuel,
    markets: {
      spot: [{
        productCode: "JET_FUEL",
        region: "Singapore",
        source: "MOPS",
        assessmentType: "Jet Kerosene"
      }]
    },
    specifications: {
      flashPoint: 38
    },
    unit: "BBL"
  },

  // ===== LIGHT PRODUCTS (GASOLINE) =====

  "GASOLINE_92": {
    databaseCode: "GASOLINE_92",
    displayName: "Gasoline 92 RON",
    assetClass: AssetClass.LightProducts,
    category: ProductCategory.MotorGasoline,
    markets: {
      spot: [{
        productCode: "GASOLINE_92",
        region: "Singapore",
        source: "MOPS",
        assessmentType: "92 RON Unleaded"
      }]
    },
    specifications: {
      octaneRating: 92
    },
    unit: "BBL"
  },

  "GASOLINE_95": {
    databaseCode: "GASOLINE_95",
    displayName: "Gasoline 95 RON",
    assetClass: AssetClass.LightProducts,
    category: ProductCategory.MotorGasoline,
    markets: {
      spot: [{
        productCode: "GASOLINE_95",
        region: "Singapore",
        source: "MOPS",
        assessmentType: "95 RON Unleaded"
      }]
    },
    specifications: {
      octaneRating: 95
    },
    unit: "BBL"
  },

  "GASOLINE_97": {
    databaseCode: "GASOLINE_97",
    displayName: "Gasoline 97 RON",
    assetClass: AssetClass.LightProducts,
    category: ProductCategory.MotorGasoline,
    markets: {
      spot: [{
        productCode: "GASOLINE_97",
        region: "Singapore",
        source: "MOPS",
        assessmentType: "97 RON Unleaded"
      }]
    },
    specifications: {
      octaneRating: 97
    },
    unit: "BBL"
  },

  // ===== HEAVY RESIDUALS (FUEL OIL) =====

  "HFO380": {
    databaseCode: "HFO380",
    displayName: "HSFO 380 CST",
    assetClass: AssetClass.HeavyResiduals,
    category: ProductCategory.HighSulfurFuelOil,
    markets: {
      spot: [
        {
          productCode: "BUNKER_SPORE",
          region: "Singapore",
          source: "MOPS",
          assessmentType: "380 CST 3.5% S"
        },
        {
          productCode: "BUNKER_HK",
          region: "Hong Kong",
          source: "MOPS",
          assessmentType: "380 CST 3.5% S"
        },
        {
          productCode: "FUEL_OIL_35_RTDM",
          region: "Rotterdam",
          source: "Platts",
          assessmentType: "380 CST 3.5% S"
        }
      ],
      futures: {
        productCode: "SG380",  // ICE Singapore HSFO 380 CST Futures
        exchange: "ICE Singapore",
        contractSize: "100 MT",
        tickSize: "$0.25/MT"
      }
    },
    specifications: {
      viscosity: "380 CST @ 50°C",
      sulfurContent: 3.5
    },
    unit: "MT"
  },

  "HSFO": {
    databaseCode: "HSFO",
    displayName: "HSFO 380 CST",
    assetClass: AssetClass.HeavyResiduals,
    category: ProductCategory.HighSulfurFuelOil,
    markets: {
      spot: [
        {
          productCode: "BUNKER_SPORE",
          region: "Singapore",
          source: "MOPS",
          assessmentType: "380 CST 3.5% S"
        }
      ]
    },
    specifications: {
      viscosity: "380 CST @ 50°C",
      sulfurContent: 3.5
    },
    unit: "MT"
  },

  // ===== VERY LOW SULFUR FUEL OIL (IMO 2020) =====

  "VLSFO": {
    databaseCode: "VLSFO",
    displayName: "VLSFO 0.5% S (IMO 2020)",
    assetClass: AssetClass.HeavyResiduals,
    category: ProductCategory.LowSulfurFuelOil,
    markets: {
      spot: [
        {
          productCode: "MARINE_FUEL_05",
          region: "Singapore",
          source: "MOPS",
          assessmentType: "0.5% S Marine Fuel"
        },
        {
          productCode: "MARINE_FUEL_05_RTDM",
          region: "Rotterdam",
          source: "Platts",
          assessmentType: "0.5% S Marine Fuel FOB RTDM"
        },
        {
          productCode: "MOPS_MARINE_05",
          region: "Singapore",
          source: "MOPS",
          assessmentType: "MOPS 0.5% S"
        }
      ],
      futures: {
        productCode: "SG05",  // ICE Singapore VLSFO 0.5% S Futures
        exchange: "ICE Singapore",
        contractSize: "100 MT",
        tickSize: "$0.25/MT"
      }
    },
    specifications: {
      viscosity: "380 CST @ 50°C",
      sulfurContent: 0.5
    },
    unit: "MT"
  }
};

/**
 * API to Database Code Reverse Mapping
 * Maps API product codes back to database codes for query resolution
 */
export const API_TO_DATABASE: Record<string, string> = {
  // Crude Oil
  "BRENT_CRUDE": "BRENT",
  "BRENT": "BRENT",
  "ICE_BRENT": "BRENT",
  "WTI": "WTI",
  "WTI_CRUDE": "WTI",
  "NYMEX_WTI": "WTI",

  // Middle Distillates
  "GASOIL_FUTURES": "GASOIL",
  "MOPS_GASOIL": "GASOIL",
  "ICE_GASOIL": "GASOIL",
  "MGO": "MGO",
  "JET_FUEL": "JET",
  "MOPS_JET": "JET",

  // Light Products
  "GASOLINE_92": "GASOLINE_92",
  "GASOLINE_95": "GASOLINE_95",
  "GASOLINE_97": "GASOLINE_97",
  "RBOB": "GASOLINE_95",
  "MOPS_GASOLINE": "GASOLINE_95",

  // Heavy Residuals - HSFO 380
  "BUNKER_SPORE": "HFO380",
  "BUNKER_HK": "HFO380",
  "HSFO_HK": "HFO380",
  "HSFO_SPORE": "HFO380",
  "FUEL_OIL_35_RTDM": "HFO380",
  "HSFO": "HFO380",
  "HSFO_380": "HFO380",

  // Brent futures variants (Fix 1: Futures Display)
  "BRT_FUT": "BRENT",
  "BRT_SWP": "BRENT",
  "92R_BRT": "BRENT",
  "92R_TS": "BRENT",

  // Gasoil futures variants (Fix 1: Futures Display)
  "GO/_180": "GASOIL",
  "GO/_380": "GASOIL",
  "GO_10PPM": "GASOIL",

  // ICE Singapore Fuel Oil Futures (Outright Contracts)
  "SG380": "HFO380",  // HSFO 380 CST Futures
  "SG05": "VLSFO",    // VLSFO 0.5% S Futures
  "SG180": "HFO180",  // HSFO 180 CST Futures (if supported)

  // Spread Contracts (Price Differentials - NOT outright futures)
  "0.5_EW": "VLSFO",   // VLSFO Europe-Asia spread
  "380_EW": "HFO380",  // HSFO 380 Europe-Asia spread
  "SG380_TS": "HFO380", // HSFO 380 time spread

  // VLSFO spot price mappings
  "MARINE_FUEL_05": "VLSFO",
  "MARINE_FUEL_05_RTDM": "VLSFO",
  "MOPS_MARINE_05": "VLSFO",
  "VLSFO_HK": "VLSFO",
  "VLSFO_SPORE": "VLSFO",
  "MOPAG_VLSFO": "VLSFO",  // Arab Gulf VLSFO

  // MGO (Marine Gas Oil) mappings - ALL regions
  "MGO_HK": "MGO",
  "MGO_SPORE": "MGO",
  "MGO_RTDM": "MGO",        // Rotterdam MGO
  "MOPS_MGO": "MGO",        // Singapore MOPS MGO
  "MOPAG_MGO": "MGO",       // Arab Gulf MGO
};

/**
 * Product Code Resolver (Professional Utility Class)
 * Handles bidirectional product code resolution with market type and region awareness
 */
export class ProductCodeResolver {
  /**
   * Resolve database code to API code based on market type and region
   * @param databaseCode - Internal database code (e.g., "BRENT", "HFO380")
   * @param marketType - Market type (Physical Spot or Exchange Futures)
   * @param region - Optional region for spot markets (Singapore, Rotterdam, Hong Kong)
   * @returns API product code or null if not found
   *
   * Examples:
   *   - resolveToAPICode("BRENT", MarketType.PhysicalSpot) → "BRENT_CRUDE"
   *   - resolveToAPICode("BRENT", MarketType.ExchangeFutures) → "BRENT"
   *   - resolveToAPICode("HFO380", MarketType.PhysicalSpot, "Singapore") → "BUNKER_SPORE"
   *   - resolveToAPICode("HFO380", MarketType.PhysicalSpot, "Rotterdam") → "FUEL_OIL_35_RTDM"
   */
  static resolveToAPICode(
    databaseCode: string,
    marketType: MarketType,
    region?: string
  ): string | null {
    const product = PRODUCT_REGISTRY[databaseCode];
    if (!product) return null;

    if (marketType === MarketType.PhysicalSpot) {
      if (!product.markets.spot || product.markets.spot.length === 0) {
        return null;
      }

      if (region) {
        // Find specific regional market
        const spotMarket = product.markets.spot.find(m => m.region === region);
        return spotMarket?.productCode || null;
      } else {
        // Return first available spot market
        return product.markets.spot[0].productCode;
      }
    } else if (marketType === MarketType.ExchangeFutures) {
      return product.markets.futures?.productCode || null;
    }

    return null;
  }

  /**
   * Resolve API code to database code
   * @param apiCode - API product code (e.g., "BRENT_CRUDE", "BUNKER_SPORE")
   * @returns Database code or null if not found
   *
   * Examples:
   *   - resolveToDBCode("BRENT_CRUDE") → "BRENT"
   *   - resolveToDBCode("BUNKER_SPORE") → "HFO380"
   *   - resolveToDBCode("GASOLINE_92") → "GASOLINE_92"
   */
  static resolveToDBCode(apiCode: string): string | null {
    return API_TO_DATABASE[apiCode] || null;
  }

  /**
   * Get available regions for a database product code
   * @param databaseCode - Internal database code
   * @returns Array of region names (empty if futures-only product)
   *
   * Examples:
   *   - getAvailableRegions("HFO380") → ["Singapore", "Hong Kong", "Rotterdam"]
   *   - getAvailableRegions("BRENT") → ["North Sea"]
   *   - getAvailableRegions("WTI") → [] (futures only, no physical regions)
   */
  static getAvailableRegions(databaseCode: string): string[] {
    const product = PRODUCT_REGISTRY[databaseCode];
    if (!product?.markets.spot) return [];
    return product.markets.spot.map(m => m.region);
  }

  /**
   * Get all available database product codes
   * @returns Array of database codes
   */
  static getAllDatabaseCodes(): string[] {
    return Object.keys(PRODUCT_REGISTRY);
  }

  /**
   * Get product display name for UI
   * @param databaseCode - Internal database code
   * @returns Professional display name
   */
  static getDisplayName(databaseCode: string): string | null {
    return PRODUCT_REGISTRY[databaseCode]?.displayName || null;
  }

  /**
   * Get asset class for a product
   * @param databaseCode - Internal database code
   * @returns Asset class enum value
   */
  static getAssetClass(databaseCode: string): AssetClass | null {
    return PRODUCT_REGISTRY[databaseCode]?.assetClass || null;
  }

  /**
   * Check if product has spot markets
   * @param databaseCode - Internal database code
   * @returns True if spot markets available
   */
  static hasSpotMarkets(databaseCode: string): boolean {
    const product = PRODUCT_REGISTRY[databaseCode];
    return !!product?.markets.spot && product.markets.spot.length > 0;
  }

  /**
   * Check if product has futures markets
   * @param databaseCode - Internal database code
   * @returns True if futures market available
   */
  static hasFuturesMarkets(databaseCode: string): boolean {
    return !!PRODUCT_REGISTRY[databaseCode]?.markets.futures;
  }
}

// ===== LEGACY 4-TIER SELECTION UI TYPES (BACKWARD COMPATIBILITY) =====

// Base product interface for TIER 1 selection
export interface BaseProduct {
  name: string;                  // "HSFO 380 CST"
  code: string;                  // Database code (e.g., "HFO380")
  availableRegions: string[];    // ["Singapore", "Hong Kong", "Rotterdam"]
}

/**
 * Extract base product code from a product code that may include contract month
 * Examples:
 *   - "ICE_BRENT_JAN25" → "ICE_BRENT"
 *   - "BRENT_2025_01" → "BRENT"
 *   - "WTI_JAN2025" → "WTI"
 *   - "MOPS_380" → "MOPS_380" (no change)
 *   - "SG380 2605" → "SG380" (Platts MOPS format)
 *   - "SG380 TS 2605" → "SG380 TS" (Time spread - preserves derivative suffix)
 *   - "GO 10ppm 2601" → "GO 10ppm" (Multi-word base product)
 */
export function extractBaseProductCode(productCode: string): string {
  if (!productCode) return '';

  // Strategy 0: CRITICAL FIX - Remove Platts MOPS format (space + YYMM contract month)
  // This MUST be first to handle the actual CSV format correctly
  // Pattern: "PRODUCT YYMM" or "PRODUCT DESCRIPTOR YYMM" (e.g., "SG380 2605", "GO 10ppm 2601", "SG380 TS 2605")
  // Match 4 digits at end of string preceded by a space
  const plattsRegex = /\s+\d{4}$/;
  let baseCode = productCode.replace(plattsRegex, '');

  // Strategy 1: Remove month codes (JAN25, FEB25, MAR25, etc.)
  const monthSuffixRegex = /_(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\d{2}$/i;
  baseCode = baseCode.replace(monthSuffixRegex, '');

  // Strategy 2: Remove month codes without underscore (JAN2025, FEB2025, etc.)
  const monthYearRegex = /_(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\d{4}$/i;
  baseCode = baseCode.replace(monthYearRegex, '');

  // Strategy 3: Remove date patterns (2025_01, 2025-01, etc.)
  const dateSuffixRegex = /_\d{4}[-_]\d{2}$/;
  baseCode = baseCode.replace(dateSuffixRegex, '');

  // Strategy 4: Remove numeric year-month patterns (202501, 2025-01, etc.)
  const numericDateRegex = /_?\d{6}$/;
  baseCode = baseCode.replace(numericDateRegex, '');

  return baseCode;
}

/**
 * Extract base product name from product code
 * PROFESSIONAL VERSION - Uses ProductCodeResolver with PRODUCT_REGISTRY
 */
export function getBaseProductName(productCode: string, productName?: string): string {
  if (!productCode) return productName || '';

  // Extract base code (remove contract month suffixes)
  const baseCode = extractBaseProductCode(productCode);

  // Try API → Database code resolution
  const dbCode = ProductCodeResolver.resolveToDBCode(baseCode);
  if (dbCode) {
    const displayName = ProductCodeResolver.getDisplayName(dbCode);
    if (displayName) return displayName;
  }

  // Try direct database code lookup
  const directDisplayName = ProductCodeResolver.getDisplayName(baseCode);
  if (directDisplayName) return directDisplayName;

  // Fallback: Use product name or clean up product code
  if (productName) {
    // Remove month codes from product name (e.g., "Brent Crude JAN25" → "Brent Crude")
    return productName
      .replace(/\s*(JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\s*\d{2,4}/i, '')
      .replace(/\s*\d{4}[-_]\d{2}/, '')
      .trim();
  }

  // Last resort: Use base code with underscores replaced by spaces
  return baseCode.replace(/_/g, ' ');
}

/**
 * Get region for a product code
 * PROFESSIONAL VERSION - Uses ProductCodeResolver with PRODUCT_REGISTRY
 */
export function getProductRegion(productCode: string): string | null {
  if (!productCode) return null;

  // Extract base code (remove contract month suffixes)
  const baseCode = extractBaseProductCode(productCode);

  // Try API → Database code resolution
  const dbCode = ProductCodeResolver.resolveToDBCode(baseCode);
  if (dbCode) {
    const regions = ProductCodeResolver.getAvailableRegions(dbCode);
    if (regions.length > 0) return regions[0]; // Return first region
  }

  // Try direct database code lookup
  const directRegions = ProductCodeResolver.getAvailableRegions(baseCode);
  if (directRegions.length > 0) return directRegions[0];

  return null;
}