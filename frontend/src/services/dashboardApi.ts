import axios from 'axios';

// Create dedicated API instance for dashboard
const dashboardApiInstance = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Dashboard API Response Types
export interface DashboardOverviewDto {
  totalContracts: number;
  totalValue: number;
  totalQuantity: number;
  averageMargin: number;
  activeContracts: number;
  pendingApproval: number;
  completedToday: number;
  riskExposure: number;
  topProducts: Array<{
    productType: string;
    value: number;
    quantity: number;
    contracts: number;
  }>;
  monthlyTrends: Array<{
    month: string;
    contractCount: number;
    totalValue: number;
    averageMargin: number;
  }>;
}

export interface TradingMetricsDto {
  totalTrades: number;
  totalVolume: number;
  avgTradeSize: number;
  winRate: number;
  avgHoldingPeriod: number;
  sharpeRatio: number;
  maxDrawdown: number;
  profitFactor: number;
  dailyMetrics: Array<{
    date: string;
    trades: number;
    volume: number;
    pnl: number;
    winRate: number;
  }>;
}

export interface PerformanceAnalyticsDto {
  totalPnL: number;
  unrealizedPnL: number;
  realizedPnL: number;
  roi: number;
  volatility: number;
  var95: number;
  var99: number;
  performanceData: Array<{
    date: string;
    dailyPnL: number;
    cumulativePnL: number;
    unrealizedPnL: number;
    volume: number;
  }>;
  monthlyBreakdown: Array<{
    month: string;
    pnl: number;
    roi: number;
    trades: number;
  }>;
}

export interface MarketInsightsDto {
  volatilityIndex: number;
  marketTrend: 'Bullish' | 'Bearish' | 'Sideways';
  correlationMatrix: Record<string, Record<string, number>>;
  priceMovements: Array<{
    product: string;
    current: number;
    change: number;
    changePercent: number;
    volume: number;
  }>;
  technicalIndicators: Array<{
    product: string;
    rsi: number;
    sma20: number;
    sma50: number;
    bollinger: {
      upper: number;
      middle: number;
      lower: number;
    };
  }>;
}

export interface OperationalStatusDto {
  systemHealth: 'Healthy' | 'Warning' | 'Critical';
  services: Array<{
    name: string;
    status: 'Online' | 'Offline' | 'Degraded';
    responseTime: number;
    lastChecked: string;
  }>;
  dataFreshness: Array<{
    dataType: string;
    lastUpdate: string;
    status: 'Fresh' | 'Stale' | 'Missing';
  }>;
  alertsSummary: {
    critical: number;
    warning: number;
    info: number;
  };
}

export interface AlertDto {
  id: string;
  type: 'Critical' | 'Warning' | 'Info';
  title: string;
  message: string;
  timestamp: string;
  isRead: boolean;
  source: string;
}

export interface KpiSummaryDto {
  revenue: {
    current: number;
    target: number;
    variance: number;
    trend: 'Up' | 'Down' | 'Stable';
  };
  volume: {
    current: number;
    target: number;
    variance: number;
    trend: 'Up' | 'Down' | 'Stable';
  };
  margin: {
    current: number;
    target: number;
    variance: number;
    trend: 'Up' | 'Down' | 'Stable';
  };
  riskUtilization: {
    current: number;
    limit: number;
    percentage: number;
    status: 'Safe' | 'Warning' | 'Critical';
  };
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