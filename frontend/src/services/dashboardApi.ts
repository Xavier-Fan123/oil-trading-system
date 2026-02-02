import axios from 'axios';

// Create dedicated API instance for dashboard
const dashboardApiInstance = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ============================================================================
// Dashboard API Response Types - Matching Backend C# DTOs (camelCase JSON)
// ============================================================================

export interface DashboardOverviewDto {
  totalPositions: number;
  totalExposure: number;
  netExposure: number;
  longPositions: number;
  shortPositions: number;
  flatPositions: number;
  dailyPnL: number;
  unrealizedPnL: number;
  vaR95: number;
  vaR99: number;
  portfolioVolatility: number;
  activePurchaseContracts: number;
  activeSalesContracts: number;
  pendingContracts: number;
  marketDataPoints: number;
  lastMarketUpdate: string;
  alertCount: number;
  calculatedAt: string;
}

export interface TradingMetricsDto {
  period: string;
  totalTrades: number;
  totalVolume: number;
  averageTradeSize: number;
  purchaseVolume: number;
  salesVolume: number;
  paperVolume: number;
  longPaperVolume: number;
  shortPaperVolume: number;
  productBreakdown: Record<string, number>;
  counterpartyBreakdown: Record<string, number>;
  tradeFrequency: number;
  volumeByProduct: Record<string, number>;
  calculatedAt: string;
}

export interface DailyPnLEntry {
  date: string;
  dailyPnL: number;
  cumulativePnL: number;
}

export interface ProductPerformanceEntry {
  product: string;
  exposure: number;
  pnL: number;
  return: number;
}

export interface PerformanceAnalyticsDto {
  period: string;
  totalPnL: number;
  realizedPnL: number;
  unrealizedPnL: number;
  bestPerformingProduct: string;
  worstPerformingProduct: string;
  totalReturn: number;
  annualizedReturn: number;
  sharpeRatio: number;
  maxDrawdown: number;
  winRate: number;
  profitFactor: number;
  vaRUtilization: number;
  volatilityAdjustedReturn: number;
  dailyPnLHistory: DailyPnLEntry[];
  productPerformance: ProductPerformanceEntry[];
  calculatedAt: string;
}

export interface KeyPriceEntry {
  product: string;
  price: number;
  change: number;
  changePercent: number;
  lastUpdate: string;
}

export interface MarketTrendEntry {
  product: string;
  trend: string;
  strength: number;
}

export interface MarketInsightsDto {
  marketDataCount: number;
  lastUpdate: string;
  keyPrices: KeyPriceEntry[];
  volatilityIndicators: Record<string, number>;
  correlationMatrix: Record<string, Record<string, number>>;
  technicalIndicators: Record<string, number>;
  marketTrends: MarketTrendEntry[];
  sentimentIndicators: Record<string, number>;
  calculatedAt: string;
}

export interface SystemHealthDto {
  databaseStatus: string;
  cacheStatus: string;
  marketDataStatus: string;
  overallStatus: string;
}

export interface UpcomingLaycanEntry {
  contractNumber: string;
  contractType: string;
  laycanStart: string;
  laycanEnd: string;
  product: string;
  quantity: number;
}

export interface OperationalStatusDto {
  activeShipments: number;
  pendingDeliveries: number;
  completedDeliveries: number;
  contractsAwaitingExecution: number;
  contractsInLaycan: number;
  upcomingLaycans: UpcomingLaycanEntry[];
  systemHealth: SystemHealthDto;
  cacheHitRatio: number;
  lastDataRefresh: string;
  calculatedAt: string;
}

export interface AlertDto {
  type: string;
  severity: string;
  message: string;
  timestamp: string;
}

export interface KpiSummaryDto {
  totalExposure: number;
  dailyPnL: number;
  vaR95: number;
  portfolioCount: number;
  exposureUtilization: number;
  riskUtilization: number;
  calculatedAt: string;
}

// Dashboard API Service
export const dashboardApi = {
  // Get overview data
  getOverview: async (): Promise<DashboardOverviewDto> => {
    const response = await dashboardApiInstance.get('/dashboard/overview');
    return response.data;
  },

  // Get trading metrics
  getTradingMetrics: async (startDate?: string, endDate?: string): Promise<TradingMetricsDto> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await dashboardApiInstance.get(`/dashboard/trading-metrics?${params.toString()}`);
    return response.data;
  },

  // Get performance analytics
  getPerformanceAnalytics: async (startDate?: string, endDate?: string): Promise<PerformanceAnalyticsDto> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await dashboardApiInstance.get(`/dashboard/performance?${params.toString()}`);
    return response.data;
  },

  // Get market insights
  getMarketInsights: async (): Promise<MarketInsightsDto> => {
    const response = await dashboardApiInstance.get('/dashboard/market-insights');
    return response.data;
  },

  // Get operational status
  getOperationalStatus: async (): Promise<OperationalStatusDto> => {
    const response = await dashboardApiInstance.get('/dashboard/operational-status');
    return response.data;
  },

  // Get alerts
  getAlerts: async (): Promise<AlertDto[]> => {
    const response = await dashboardApiInstance.get('/dashboard/alerts');
    return response.data;
  },

  // Get KPIs
  getKpis: async (): Promise<KpiSummaryDto> => {
    const response = await dashboardApiInstance.get('/dashboard/kpis');
    return response.data;
  },
};
