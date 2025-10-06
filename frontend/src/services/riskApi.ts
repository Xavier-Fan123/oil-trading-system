import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export interface RiskMetrics {
  portfolioValue: number;
  var95: number;
  var99: number;
  expectedShortfall95: number;
  expectedShortfall99: number;
  portfolioVolatility: number;
  maxDrawdown: number;
  concentrationRisk: number;
  numberOfPositions: number;
  diversificationBenefit: number;
}

export interface StressTestResult {
  scenarioName: string;
  description: string;
  portfolioChange: number;
  percentageChange: number;
  newPortfolioValue: number;
}

export interface ProductRisk {
  productType: string;
  exposure: number;
  var95: number;
  var99: number;
  volatility: number;
  contribution: number;
}

export interface BacktestResult {
  date: string;
  portfolioValue: number;
  predictedVaR: number;
  actualPnL: number;
  breached: boolean;
  confidenceLevel: number;
}

export interface RiskCalculationResponse {
  timestamp: string;
  calculationTimeMs: number;
  riskMetrics: RiskMetrics;
  stressTests: StressTestResult[];
  productRisks: ProductRisk[];
  riskLimits: {
    var95Limit: number;
    var99Limit: number;
    concentrationLimit: number;
    maxDrawdownLimit: number;
  };
  breachedLimits: string[];
}

export interface PortfolioSummary {
  totalValue: number;
  totalPositions: number;
  riskMetrics: RiskMetrics;
  topRisks: ProductRisk[];
  alerts: {
    level: 'info' | 'warning' | 'error';
    message: string;
    timestamp: string;
  }[];
}

// Risk API endpoints
export const riskApi = {
  calculateRisk: async (): Promise<RiskCalculationResponse> => {
    const response = await api.get('/risk/calculate');
    return response.data;
  },

  getPortfolioSummary: async (): Promise<PortfolioSummary> => {
    const response = await api.get('/risk/portfolio-summary');
    return response.data;
  },

  getProductRisk: async (productType: string): Promise<ProductRisk> => {
    const response = await api.get(`/risk/product/${productType}`);
    return response.data;
  },

  runBacktest: async (days?: number): Promise<BacktestResult[]> => {
    const params = new URLSearchParams();
    if (days) params.append('days', days.toString());
    
    const response = await api.get(`/risk/backtest?${params.toString()}`);
    return response.data;
  }
};

export default api;