import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// TypeScript interfaces for Price Validation
export interface ValidatePriceRequest {
  productType: string;
  price: number;
  priceDate: string;
}

export interface PriceValidationResult {
  isValid: boolean;
  severity: string; // "Normal", "Warning", "Critical"
  message: string;
  price: number;
  productType: string;
  priceDate: string;
  expectedRange: {
    min: number;
    max: number;
  };
  marketAverage: number;
  deviationPercentage: number;
  zScore: number;
  confidenceLevel: number;
  validationRules: string[];
  recommendations: string[];
}

export interface ValidatePriceSeriesRequest {
  productType: string;
  prices: { [date: string]: number };
}

export interface PriceAnomalyResult {
  date: string;
  price: number;
  expectedPrice: number;
  deviation: number;
  deviationPercentage: number;
  severity: number; // 0-1 scale
  anomalyType: string; // "Spike", "Drop", "Gradual", "Pattern"
  confidence: number;
  marketConditions: string[];
  possibleCauses: string[];
}

export interface PriceVolatilityMetrics {
  productType: string;
  period: string;
  startDate: string;
  endDate: string;
  dailyVolatility: number;
  weeklyVolatility: number;
  monthlyVolatility: number;
  annualizedVolatility: number;
  averagePrice: number;
  minPrice: number;
  maxPrice: number;
  priceRange: number;
  coefficientOfVariation: number;
  kurtosis: number;
  skewness: number;
  valueAtRisk95: number;
  valueAtRisk99: number;
  dataPoints: number;
}

export interface ValidatePriceChangeRequest {
  productType: string;
  oldPrice: number;
  newPrice: number;
  changeDate: string;
}

export interface PriceChangeValidation {
  isValidChange: boolean;
  changePercentage: number;
  changeAmount: number;
  severity: string;
  maxAllowedChange: number;
  exceedsThreshold: boolean;
  marketJustification: boolean;
  requiresApproval: boolean;
  reasonCodes: string[];
  recommendations: string[];
  historicalComparison: {
    similarChangesInPeriod: number;
    averageChangeInPeriod: number;
    maxChangeInPeriod: number;
  };
}

export interface PriceValidationConfig {
  productType: string;
  enabledRules: string[];
  thresholds: {
    maxDailyChange: number;
    maxWeeklyChange: number;
    outlierZScore: number;
    volatilityThreshold: number;
    minimumPrice: number;
    maximumPrice: number;
  };
  marketDataSources: string[];
  validationWindows: {
    shortTerm: number; // days
    mediumTerm: number; // days
    longTerm: number; // days
  };
  alertSettings: {
    emailNotification: boolean;
    slackNotification: boolean;
    dashboardAlert: boolean;
  };
  lastUpdated: string;
  updatedBy?: string;
}

export interface PriceValidationSummary {
  date: string;
  productSummaries: ProductValidationSummary[];
  overallStatus: string;
  totalAnomalies: number;
  criticalAnomalies: number;
  warningAnomalies: number;
  validationCoverage: number;
  lastValidationRun: string;
}

export interface ProductValidationSummary {
  productType: string;
  anomalyCount: number;
  highSeverityAnomalies: number;
  currentVolatility: number;
  validationStatus: string; // "Normal", "Warning", "Critical"
  lastValidation: string;
  dataQuality: number; // 0-100 percentage
  recommendedActions: string[];
}

export interface PriceOutlierDetection {
  productType: string;
  detectionMethod: string; // "Z-Score", "IQR", "Isolation Forest", "LSTM"
  outliers: Array<{
    date: string;
    price: number;
    outlierScore: number;
    isConfirmed: boolean;
    explanation: string;
  }>;
  modelAccuracy: number;
  falsePositiveRate: number;
  lastModelUpdate: string;
}

export interface MarketContextData {
  productType: string;
  date: string;
  marketConditions: {
    oilInventory: string; // "High", "Normal", "Low"
    geopoliticalTension: string; // "High", "Medium", "Low"
    economicIndicators: string; // "Positive", "Neutral", "Negative"
    seasonalFactors: string;
    weatherImpact: string;
  };
  newsEvents: Array<{
    headline: string;
    impact: string; // "Bullish", "Bearish", "Neutral"
    relevance: number; // 0-1 scale
  }>;
  correlatedAssets: Array<{
    asset: string;
    correlation: number;
    priceChange: number;
  }>;
}

export interface PriceValidationError {
  error: string;
  message?: string;
}

/**
 * Price Validation API service for validating and analyzing oil price data
 * Corresponds to PriceValidationController endpoints
 */
export const priceValidationApi = {
  /**
   * POST /api/price-validation/validate-price
   * Validate a single price point
   */
  validatePrice: async (request: ValidatePriceRequest): Promise<PriceValidationResult> => {
    try {
      const response = await api.post('/price-validation/validate-price', request);
      return response.data;
    } catch (error) {
      console.error('Error validating price:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to validate price');
      }
      throw error;
    }
  },

  /**
   * POST /api/price-validation/validate-series
   * Validate multiple prices in a series
   */
  validatePriceSeries: async (request: ValidatePriceSeriesRequest): Promise<PriceValidationResult[]> => {
    try {
      const response = await api.post('/price-validation/validate-series', request);
      return response.data;
    } catch (error) {
      console.error('Error validating price series:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to validate price series');
      }
      throw error;
    }
  },

  /**
   * GET /api/price-validation/anomalies
   * Detect price anomalies in historical data
   */
  detectAnomalies: async (
    productType: string,
    startDate: string,
    endDate: string
  ): Promise<PriceAnomalyResult[]> => {
    try {
      const params = new URLSearchParams({
        productType,
        startDate,
        endDate
      });

      const response = await api.get(`/price-validation/anomalies?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error detecting price anomalies:', error);
      throw error;
    }
  },

  /**
   * GET /api/price-validation/volatility
   * Get price volatility metrics
   */
  getVolatilityMetrics: async (
    productType: string,
    startDate: string,
    endDate: string
  ): Promise<PriceVolatilityMetrics> => {
    try {
      const params = new URLSearchParams({
        productType,
        startDate,
        endDate
      });

      const response = await api.get(`/price-validation/volatility?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching volatility metrics:', error);
      throw error;
    }
  },

  /**
   * POST /api/price-validation/validate-change
   * Validate price change between two prices
   */
  validatePriceChange: async (request: ValidatePriceChangeRequest): Promise<PriceChangeValidation> => {
    try {
      const response = await api.post('/price-validation/validate-change', request);
      return response.data;
    } catch (error) {
      console.error('Error validating price change:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to validate price change');
      }
      throw error;
    }
  },

  /**
   * GET /api/price-validation/config/{productType}
   * Get validation configuration for a product type
   */
  getValidationConfig: async (productType: string): Promise<PriceValidationConfig> => {
    try {
      const response = await api.get(`/price-validation/config/${productType}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching validation config:', error);
      throw error;
    }
  },

  /**
   * PUT /api/price-validation/config/{productType}
   * Update validation configuration for a product type
   */
  updateValidationConfig: async (
    productType: string,
    config: PriceValidationConfig
  ): Promise<{ message: string }> => {
    try {
      const response = await api.put(`/price-validation/config/${productType}`, config);
      return response.data;
    } catch (error) {
      console.error('Error updating validation config:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to update validation config');
      }
      throw error;
    }
  },

  /**
   * GET /api/price-validation/summary
   * Get price validation summary for dashboard
   */
  getValidationSummary: async (
    productTypes: string[],
    date?: string
  ): Promise<PriceValidationSummary> => {
    try {
      const params = new URLSearchParams();
      productTypes.forEach(type => params.append('productTypes', type));
      if (date) params.append('date', date);

      const response = await api.get(`/price-validation/summary?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching validation summary:', error);
      throw error;
    }
  },

  /**
   * GET /api/price-validation/outliers
   * Advanced outlier detection using machine learning
   */
  detectOutliers: async (
    productType: string,
    method: string = 'Z-Score',
    lookbackDays: number = 30
  ): Promise<PriceOutlierDetection> => {
    try {
      const params = new URLSearchParams({
        productType,
        method,
        lookbackDays: lookbackDays.toString()
      });

      const response = await api.get(`/price-validation/outliers?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error detecting outliers:', error);
      throw error;
    }
  },

  /**
   * GET /api/price-validation/market-context
   * Get market context data for price validation
   */
  getMarketContext: async (productType: string, date: string): Promise<MarketContextData> => {
    try {
      const params = new URLSearchParams({
        productType,
        date
      });

      const response = await api.get(`/price-validation/market-context?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching market context:', error);
      throw error;
    }
  },

  // Utility methods for data validation and analysis

  /**
   * Validates a price validation request
   */
  validatePriceRequest: (request: ValidatePriceRequest): string[] => {
    const errors: string[] = [];

    if (!request.productType || request.productType.trim() === '') {
      errors.push('Product type is required');
    }

    if (!request.price || request.price <= 0) {
      errors.push('Price must be greater than zero');
    }

    if (!request.priceDate) {
      errors.push('Price date is required');
    } else {
      const date = new Date(request.priceDate);
      if (isNaN(date.getTime())) {
        errors.push('Invalid price date format');
      }
    }

    return errors;
  },

  /**
   * Calculates z-score for price validation
   */
  calculateZScore: (price: number, mean: number, standardDeviation: number): number => {
    if (standardDeviation === 0) return 0;
    return (price - mean) / standardDeviation;
  },

  /**
   * Determines price validation severity based on z-score
   */
  getSeverityFromZScore: (zScore: number): string => {
    const abs = Math.abs(zScore);
    
    if (abs <= 1.5) return 'Normal';
    if (abs <= 2.5) return 'Warning';
    return 'Critical';
  },

  /**
   * Calculates percentage deviation from expected price
   */
  calculateDeviationPercentage: (actualPrice: number, expectedPrice: number): number => {
    if (expectedPrice === 0) return 0;
    return ((actualPrice - expectedPrice) / expectedPrice) * 100;
  },

  /**
   * Filters anomalies by severity threshold
   */
  filterAnomaliesBySeverity: (
    anomalies: PriceAnomalyResult[],
    minSeverity: number
  ): PriceAnomalyResult[] => {
    return anomalies.filter(anomaly => anomaly.severity >= minSeverity);
  },

  /**
   * Groups anomalies by type
   */
  groupAnomaliesByType: (anomalies: PriceAnomalyResult[]): { [type: string]: PriceAnomalyResult[] } => {
    return anomalies.reduce((groups, anomaly) => {
      const type = anomaly.anomalyType;
      if (!groups[type]) {
        groups[type] = [];
      }
      groups[type].push(anomaly);
      return groups;
    }, {} as { [type: string]: PriceAnomalyResult[] });
  },

  /**
   * Calculates confidence interval for price predictions
   */
  calculateConfidenceInterval: (
    predictions: number[],
    confidenceLevel: number = 0.95
  ): { lower: number; upper: number } => {
    if (predictions.length === 0) return { lower: 0, upper: 0 };

    const sorted = [...predictions].sort((a, b) => a - b);
    const alpha = 1 - confidenceLevel;
    const lowerIndex = Math.floor(alpha / 2 * sorted.length);
    const upperIndex = Math.ceil((1 - alpha / 2) * sorted.length) - 1;

    return {
      lower: sorted[Math.max(0, lowerIndex)],
      upper: sorted[Math.min(sorted.length - 1, upperIndex)]
    };
  },

  /**
   * Identifies price patterns in historical data
   */
  identifyPricePatterns: (prices: { date: string; price: number }[]): {
    pattern: string;
    strength: number;
    duration: number;
    confidence: number;
  }[] => {
    if (prices.length < 5) return [];

    const patterns: { pattern: string; strength: number; duration: number; confidence: number }[] = [];
    const priceValues = prices.map(p => p.price);

    // Trend analysis
    let upTrend = 0;
    let downTrend = 0;
    for (let i = 1; i < priceValues.length; i++) {
      if (priceValues[i] > priceValues[i - 1]) upTrend++;
      else if (priceValues[i] < priceValues[i - 1]) downTrend++;
    }

    if (upTrend > downTrend * 2) {
      patterns.push({
        pattern: 'Uptrend',
        strength: upTrend / (priceValues.length - 1),
        duration: priceValues.length,
        confidence: Math.min(upTrend / (downTrend + 1), 1)
      });
    } else if (downTrend > upTrend * 2) {
      patterns.push({
        pattern: 'Downtrend',
        strength: downTrend / (priceValues.length - 1),
        duration: priceValues.length,
        confidence: Math.min(downTrend / (upTrend + 1), 1)
      });
    }

    // Volatility clustering
    const returns = [];
    for (let i = 1; i < priceValues.length; i++) {
      returns.push(Math.abs(Math.log(priceValues[i] / priceValues[i - 1])));
    }

    const avgReturn = returns.reduce((sum, ret) => sum + ret, 0) / returns.length;
    const highVolPeriods = returns.filter(ret => ret > avgReturn * 1.5).length;

    if (highVolPeriods > returns.length * 0.3) {
      patterns.push({
        pattern: 'High Volatility Clustering',
        strength: highVolPeriods / returns.length,
        duration: priceValues.length,
        confidence: Math.min(highVolPeriods / (returns.length * 0.3), 1)
      });
    }

    return patterns;
  },

  /**
   * Calculates moving average for price smoothing
   */
  calculateMovingAverage: (prices: number[], window: number): number[] => {
    const movingAverages: number[] = [];
    
    for (let i = window - 1; i < prices.length; i++) {
      const windowPrices = prices.slice(i - window + 1, i + 1);
      const average = windowPrices.reduce((sum, price) => sum + price, 0) / window;
      movingAverages.push(average);
    }
    
    return movingAverages;
  },

  /**
   * Detects price support and resistance levels
   */
  detectSupportResistance: (prices: number[], tolerance: number = 0.02): {
    support: number[];
    resistance: number[];
  } => {
    const support: number[] = [];
    const resistance: number[] = [];
    
    for (let i = 1; i < prices.length - 1; i++) {
      const current = prices[i];
      const prev = prices[i - 1];
      const next = prices[i + 1];
      
      // Local minimum (support)
      if (current < prev && current < next) {
        support.push(current);
      }
      
      // Local maximum (resistance)
      if (current > prev && current > next) {
        resistance.push(current);
      }
    }
    
    // Group similar levels
    const groupLevels = (levels: number[]): number[] => {
      if (levels.length === 0) return [];
      
      const grouped: number[] = [];
      const sorted = [...levels].sort((a, b) => a - b);
      let currentGroup = [sorted[0]];
      
      for (let i = 1; i < sorted.length; i++) {
        if (Math.abs(sorted[i] - sorted[i - 1]) / sorted[i - 1] <= tolerance) {
          currentGroup.push(sorted[i]);
        } else {
          grouped.push(currentGroup.reduce((sum, val) => sum + val, 0) / currentGroup.length);
          currentGroup = [sorted[i]];
        }
      }
      
      grouped.push(currentGroup.reduce((sum, val) => sum + val, 0) / currentGroup.length);
      return grouped;
    };
    
    return {
      support: groupLevels(support),
      resistance: groupLevels(resistance)
    };
  }
};

export default priceValidationApi;