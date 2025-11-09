import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Daily settlement trend data
 */
export interface DailySettlementTrend {
  date: string;
  settlementCount: number;
  totalAmount: number;
  completedCount: number;
  pendingCount: number;
}

/**
 * Currency-wise breakdown
 */
export interface CurrencyBreakdown {
  currency: string;
  settlementCount: number;
  totalAmount: number;
  percentageOfTotal: number;
}

/**
 * Partner settlement summary
 */
export interface PartnerSettlementSummary {
  partnerId: string;
  partnerName: string;
  settlementCount: number;
  totalAmount: number;
  averageAmount: number;
  settlementType: string;
}

/**
 * Status distribution for visualization
 */
export interface StatusDistribution {
  status: string;
  count: number;
  percentage: number;
}

/**
 * Comprehensive settlement analytics DTO
 */
export interface SettlementAnalytics {
  totalSettlements: number;
  totalAmount: number;
  averageAmount: number;
  minimumAmount: number;
  maximumAmount: number;
  settlementsByStatus: Record<string, number>;
  settlementsByCurrency: Record<string, number>;
  settlementsByType: Record<string, number>;
  averageProcessingTimeDays: number;
  slaComplianceRate: number;
  dailyTrends: DailySettlementTrend[];
  currencyBreakdown: CurrencyBreakdown[];
  topPartners: PartnerSettlementSummary[];
  statusDistribution: StatusDistribution[];
}

/**
 * Settlement metrics/KPIs DTO
 */
export interface SettlementMetrics {
  totalSettlementValue: number;
  totalSettlementCount: number;
  averageProcessingTimeHours: number;
  onTimeCompletionRate: number;
  settlementsWithErrors: number;
  successRate: number;
  averageSettlementValue: number;
  pendingSettlements: number;
  overdueSettlements: number;
  uniquePartners: number;
  mostCommonCurrency: string;
  settlementCountTrend: number;
  settlementValueTrend: number;
  calculatedAt: string;
}

/**
 * Dashboard summary combining analytics and metrics
 */
export interface SettlementDashboardSummary {
  analytics: SettlementAnalytics;
  metrics: SettlementMetrics;
  generatedAt: string;
  analysisPeriodDays: number;
}

/**
 * Settlement Analytics API Service
 * Provides methods to fetch settlement analytics, metrics, and KPIs from backend
 */
export const settlementAnalyticsApi = {
  /**
   * Retrieve comprehensive settlement analytics and statistics
   * @param daysToAnalyze Number of days to analyze (default: 30)
   * @param isSalesSettlement Filter by settlement type: true=sales, false=purchase, null=all
   * @param currency Filter by currency code
   * @param status Filter by settlement status
   * @returns Promise with settlement analytics
   */
  getAnalytics: async (
    daysToAnalyze: number = 30,
    isSalesSettlement?: boolean | null,
    currency?: string,
    status?: string
  ): Promise<SettlementAnalytics> => {
    const params = new URLSearchParams();
    params.append('daysToAnalyze', daysToAnalyze.toString());
    if (isSalesSettlement !== undefined && isSalesSettlement !== null) {
      params.append('isSalesSettlement', isSalesSettlement.toString());
    }
    if (currency) {
      params.append('currency', currency);
    }
    if (status) {
      params.append('status', status);
    }

    const response = await api.get<SettlementAnalytics>(
      `/settlement-analytics/analytics?${params.toString()}`
    );
    return response.data;
  },

  /**
   * Retrieve key settlement performance metrics and KPIs for dashboard
   * @param daysToAnalyze Number of days to analyze (default: 7)
   * @returns Promise with settlement metrics
   */
  getMetrics: async (daysToAnalyze: number = 7): Promise<SettlementMetrics> => {
    const response = await api.get<SettlementMetrics>(
      `/settlement-analytics/metrics?daysToAnalyze=${daysToAnalyze}`
    );
    return response.data;
  },

  /**
   * Retrieve daily settlement trend data for charting
   * @param daysToAnalyze Number of days of historical data (default: 30)
   * @returns Promise with daily trends array
   */
  getDailyTrends: async (
    daysToAnalyze: number = 30
  ): Promise<DailySettlementTrend[]> => {
    const response = await api.get<DailySettlementTrend[]>(
      `/settlement-analytics/daily-trends?daysToAnalyze=${daysToAnalyze}`
    );
    return response.data;
  },

  /**
   * Retrieve settlement distribution by currency
   * @param daysToAnalyze Number of days to analyze (default: 30)
   * @returns Promise with currency breakdown array
   */
  getCurrencyBreakdown: async (
    daysToAnalyze: number = 30
  ): Promise<CurrencyBreakdown[]> => {
    const response = await api.get<CurrencyBreakdown[]>(
      `/settlement-analytics/currency-breakdown?daysToAnalyze=${daysToAnalyze}`
    );
    return response.data;
  },

  /**
   * Retrieve settlement distribution by status
   * @param daysToAnalyze Number of days to analyze (default: 30)
   * @returns Promise with status distribution array
   */
  getStatusDistribution: async (
    daysToAnalyze: number = 30
  ): Promise<StatusDistribution[]> => {
    const response = await api.get<StatusDistribution[]>(
      `/settlement-analytics/status-distribution?daysToAnalyze=${daysToAnalyze}`
    );
    return response.data;
  },

  /**
   * Retrieve top trading partners by settlement volume
   * @param daysToAnalyze Number of days to analyze (default: 30)
   * @returns Promise with top partners array
   */
  getTopPartners: async (
    daysToAnalyze: number = 30
  ): Promise<PartnerSettlementSummary[]> => {
    const response = await api.get<PartnerSettlementSummary[]>(
      `/settlement-analytics/top-partners?daysToAnalyze=${daysToAnalyze}`
    );
    return response.data;
  },

  /**
   * Retrieve comprehensive settlement dashboard summary
   * Combines analytics, metrics, and key statistics in single response
   * @param daysToAnalyze Number of days to analyze (default: 30)
   * @returns Promise with complete dashboard summary
   */
  getDashboardSummary: async (
    daysToAnalyze: number = 30
  ): Promise<SettlementDashboardSummary> => {
    const response = await api.get<SettlementDashboardSummary>(
      `/settlement-analytics/summary?daysToAnalyze=${daysToAnalyze}`
    );
    return response.data;
  },
};
