import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// TypeScript interfaces for Basis Calculation
export interface BasisCalculationResult {
  productType: string;
  valuationDate: string;
  futuresContract: string;
  spotPrice: number;
  futuresPrice: number;
  basis: number;
  basisPercentage: number;
  calculationTimestamp: string;
}

export interface MultipleBasisRequest {
  productType: string;
  valuationDate: string;
  futuresContracts: string[];
}

export interface MultipleBasisResult {
  [futuresContract: string]: number;
}

export interface BasisHistoryDto {
  date: string;
  productType: string;
  futuresContract: string;
  spotPrice: number;
  futuresPrice: number;
  basis: number;
  basisPercentage: number;
  volume: number;
  isHoliday: boolean;
}

export interface BasisAdjustedPriceRequest {
  futuresPrice: number;
  productType: string;
  valuationDate: string;
  futuresContract: string;
}

export interface BasisAdjustedPriceResult {
  futuresPrice: number;
  basis: number;
  adjustedPrice: number;
  productType: string;
  futuresContract: string;
  valuationDate: string;
  calculationMethod: string;
}

export interface BasisValidationRequest {
  productType: string;
  calculatedBasis: number;
  valuationDate: string;
}

export interface BasisValidationResult {
  isValid: boolean;
  severity: string; // "Normal", "Warning", "Critical"
  message: string;
  expectedRange: {
    min: number;
    max: number;
  };
  historicalAverage: number;
  standardDeviation: number;
  zScore: number;
  recommendations: string[];
}

export interface BasisStatistics {
  productType: string;
  futuresContract: string;
  period: string;
  averageBasis: number;
  minBasis: number;
  maxBasis: number;
  standardDeviation: number;
  volatility: number;
  correlation: number;
  dataPoints: number;
  lastUpdated: string;
}

export interface BasisSpreadAnalysis {
  primaryProduct: string;
  secondaryProduct: string;
  spread: number;
  spreadPercentage: number;
  historicalSpread: number;
  spreadVolatility: number;
  convergenceExpected: boolean;
  arbitrageOpportunity: boolean;
  riskLevel: string;
  recommendations: string[];
}

export interface BasisTrendAnalysis {
  productType: string;
  futuresContract: string;
  trend: string; // "Strengthening", "Weakening", "Stable"
  trendStrength: number; // 0-1 scale
  momentum: number;
  meanReversion: boolean;
  forecastNextWeek: number;
  forecastNextMonth: number;
  confidence: number;
  keyDrivers: string[];
}

export interface BasisError {
  error: string;
  message?: string;
}

/**
 * Basis Calculation API service for calculating and analyzing price basis
 * Corresponds to BasisCalculationController endpoints
 */
export const basisCalculationApi = {
  /**
   * GET /api/basis-calculation/calculate
   * Calculate basis (spread) between spot and futures prices
   */
  calculateBasis: async (
    productType: string,
    valuationDate: string,
    futuresContract: string
  ): Promise<number> => {
    try {
      const params = new URLSearchParams({
        productType,
        valuationDate,
        futuresContract
      });

      const response = await api.get(`/basis-calculation/calculate?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error calculating basis:', error);
      throw error;
    }
  },

  /**
   * POST /api/basis-calculation/calculate-multiple
   * Calculate basis for multiple futures contracts
   */
  calculateMultipleBasis: async (request: MultipleBasisRequest): Promise<MultipleBasisResult> => {
    try {
      const response = await api.post('/basis-calculation/calculate-multiple', request);
      return response.data;
    } catch (error) {
      console.error('Error calculating multiple basis:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to calculate multiple basis');
      }
      throw error;
    }
  },

  /**
   * GET /api/basis-calculation/history
   * Get basis history for analysis
   */
  getBasisHistory: async (
    productType: string,
    futuresContract: string,
    startDate: string,
    endDate: string
  ): Promise<BasisHistoryDto[]> => {
    try {
      const params = new URLSearchParams({
        productType,
        futuresContract,
        startDate,
        endDate
      });

      const response = await api.get(`/basis-calculation/history?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching basis history:', error);
      throw error;
    }
  },

  /**
   * POST /api/basis-calculation/adjusted-price
   * Calculate basis-adjusted price using futures price + basis
   */
  calculateBasisAdjustedPrice: async (request: BasisAdjustedPriceRequest): Promise<number> => {
    try {
      const response = await api.post('/basis-calculation/adjusted-price', request);
      return response.data;
    } catch (error) {
      console.error('Error calculating basis-adjusted price:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to calculate adjusted price');
      }
      throw error;
    }
  },

  /**
   * POST /api/basis-calculation/validate
   * Validate if basis is within expected range
   */
  validateBasis: async (request: BasisValidationRequest): Promise<BasisValidationResult> => {
    try {
      const response = await api.post('/basis-calculation/validate', request);
      return response.data;
    } catch (error) {
      console.error('Error validating basis:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to validate basis');
      }
      throw error;
    }
  },

  /**
   * GET /api/basis-calculation/statistics
   * Get basis statistics for a product and contract
   */
  getBasisStatistics: async (
    productType: string,
    futuresContract: string,
    period: string = '30D'
  ): Promise<BasisStatistics> => {
    try {
      const params = new URLSearchParams({
        productType,
        futuresContract,
        period
      });

      const response = await api.get(`/basis-calculation/statistics?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching basis statistics:', error);
      throw error;
    }
  },

  /**
   * GET /api/basis-calculation/spread-analysis
   * Analyze spread between two products
   */
  getSpreadAnalysis: async (
    primaryProduct: string,
    secondaryProduct: string,
    analysisDate: string
  ): Promise<BasisSpreadAnalysis> => {
    try {
      const params = new URLSearchParams({
        primaryProduct,
        secondaryProduct,
        analysisDate
      });

      const response = await api.get(`/basis-calculation/spread-analysis?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching spread analysis:', error);
      throw error;
    }
  },

  /**
   * GET /api/basis-calculation/trend-analysis
   * Analyze basis trends and forecast
   */
  getTrendAnalysis: async (
    productType: string,
    futuresContract: string,
    analysisDate: string
  ): Promise<BasisTrendAnalysis> => {
    try {
      const params = new URLSearchParams({
        productType,
        futuresContract,
        analysisDate
      });

      const response = await api.get(`/basis-calculation/trend-analysis?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching trend analysis:', error);
      throw error;
    }
  },

  // Utility methods for data processing and analysis

  /**
   * Validates basis calculation request parameters
   */
  validateCalculationRequest: (
    productType: string,
    valuationDate: string,
    futuresContract: string
  ): string[] => {
    const errors: string[] = [];

    if (!productType || productType.trim() === '') {
      errors.push('Product type is required');
    }

    if (!valuationDate) {
      errors.push('Valuation date is required');
    } else {
      const date = new Date(valuationDate);
      if (isNaN(date.getTime())) {
        errors.push('Invalid valuation date format');
      }
    }

    if (!futuresContract || futuresContract.trim() === '') {
      errors.push('Futures contract is required');
    }

    return errors;
  },

  /**
   * Calculates basis percentage from absolute basis
   */
  calculateBasisPercentage: (basis: number, spotPrice: number): number => {
    if (spotPrice === 0) return 0;
    return (basis / spotPrice) * 100;
  },

  /**
   * Determines basis strength category
   */
  categorizeBasisStrength: (basisPercentage: number): string => {
    const abs = Math.abs(basisPercentage);
    
    if (abs < 1) return 'Very Strong';
    if (abs < 3) return 'Strong';
    if (abs < 5) return 'Moderate';
    if (abs < 10) return 'Weak';
    return 'Very Weak';
  },

  /**
   * Filters basis history by date range
   */
  filterBasisHistory: (
    history: BasisHistoryDto[],
    startDate: string,
    endDate: string
  ): BasisHistoryDto[] => {
    const start = new Date(startDate);
    const end = new Date(endDate);
    
    return history.filter(entry => {
      const entryDate = new Date(entry.date);
      return entryDate >= start && entryDate <= end;
    });
  },

  /**
   * Calculates moving average of basis values
   */
  calculateMovingAverage: (history: BasisHistoryDto[], windowSize: number): number[] => {
    const movingAverages: number[] = [];
    
    for (let i = windowSize - 1; i < history.length; i++) {
      const window = history.slice(i - windowSize + 1, i + 1);
      const average = window.reduce((sum, entry) => sum + entry.basis, 0) / windowSize;
      movingAverages.push(average);
    }
    
    return movingAverages;
  },

  /**
   * Identifies basis outliers using statistical methods
   */
  identifyOutliers: (history: BasisHistoryDto[], zScoreThreshold: number = 2.5): BasisHistoryDto[] => {
    if (history.length < 3) return [];

    const basisValues = history.map(entry => entry.basis);
    const mean = basisValues.reduce((sum, value) => sum + value, 0) / basisValues.length;
    const variance = basisValues.reduce((sum, value) => sum + Math.pow(value - mean, 2), 0) / basisValues.length;
    const standardDeviation = Math.sqrt(variance);

    return history.filter(entry => {
      const zScore = Math.abs((entry.basis - mean) / standardDeviation);
      return zScore > zScoreThreshold;
    });
  },

  /**
   * Calculates correlation between two basis series
   */
  calculateCorrelation: (series1: number[], series2: number[]): number => {
    if (series1.length !== series2.length || series1.length < 2) return 0;

    const n = series1.length;
    const mean1 = series1.reduce((sum, val) => sum + val, 0) / n;
    const mean2 = series2.reduce((sum, val) => sum + val, 0) / n;

    let numerator = 0;
    let sumSq1 = 0;
    let sumSq2 = 0;

    for (let i = 0; i < n; i++) {
      const diff1 = series1[i] - mean1;
      const diff2 = series2[i] - mean2;
      numerator += diff1 * diff2;
      sumSq1 += diff1 * diff1;
      sumSq2 += diff2 * diff2;
    }

    const denominator = Math.sqrt(sumSq1 * sumSq2);
    return denominator === 0 ? 0 : numerator / denominator;
  },

  /**
   * Groups basis history by time period
   */
  groupByPeriod: (
    history: BasisHistoryDto[],
    period: 'daily' | 'weekly' | 'monthly'
  ): { [key: string]: BasisHistoryDto[] } => {
    return history.reduce((groups, entry) => {
      const date = new Date(entry.date);
      let key: string;

      switch (period) {
        case 'weekly':
          const weekStart = new Date(date);
          weekStart.setDate(date.getDate() - date.getDay());
          key = weekStart.toISOString().split('T')[0];
          break;
        case 'monthly':
          key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
          break;
        default: // daily
          key = entry.date.split('T')[0];
      }

      if (!groups[key]) {
        groups[key] = [];
      }
      groups[key].push(entry);
      return groups;
    }, {} as { [key: string]: BasisHistoryDto[] });
  },

  /**
   * Calculates volatility of basis over time
   */
  calculateVolatility: (basisValues: number[], annualizationFactor: number = 252): number => {
    if (basisValues.length < 2) return 0;

    const returns = [];
    for (let i = 1; i < basisValues.length; i++) {
      if (basisValues[i - 1] !== 0) {
        returns.push(Math.log(basisValues[i] / basisValues[i - 1]));
      }
    }

    if (returns.length < 2) return 0;

    const meanReturn = returns.reduce((sum, ret) => sum + ret, 0) / returns.length;
    const variance = returns.reduce((sum, ret) => sum + Math.pow(ret - meanReturn, 2), 0) / (returns.length - 1);
    const dailyVolatility = Math.sqrt(variance);
    
    return dailyVolatility * Math.sqrt(annualizationFactor) * 100; // Convert to percentage
  }
};

export default basisCalculationApi;