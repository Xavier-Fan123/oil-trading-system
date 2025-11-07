import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

export interface RiskLevel {
  id: number;
  name: 'Low' | 'Medium' | 'High' | 'Critical';
}

export interface TradingPartnerExposureDto {
  tradingPartnerId: string;
  companyName: string;
  companyCode: string;
  partnerType: number;

  // Credit Management
  creditLimit: number;
  availableCredit: number;
  currentExposure: number;
  creditUtilizationPercentage: number;

  // Outstanding Amounts
  outstandingApAmount: number;  // Accounts Payable (we owe)
  outstandingArAmount: number;  // Accounts Receivable (they owe us)
  netExposure: number;

  // Overdue Information
  overdueApAmount: number;
  overdueArAmount: number;
  overdueSettlementCount: number;

  // Settlement Statistics
  totalUnpaidSettlements: number;
  settlementsDueIn30Days: number;

  // Risk Assessment
  riskLevel: number;  // 1=Low, 2=Medium, 3=High, 4=Critical
  riskLevelDescription: string;
  isOverLimit: boolean;
  isCreditExpired: boolean;

  // Status
  isActive: boolean;
  isBlocked: boolean;
  blockReason?: string;

  // Timestamps
  creditLimitValidUntil: Date;
  lastTransactionDate?: Date;
  exposureCalculatedDate: Date;
}

export interface PartnerSettlementSummaryDto {
  tradingPartnerId: string;
  companyName: string;

  // AP (Purchase Contracts - we owe)
  totalApAmount: number;
  paidApAmount: number;
  unpaidApAmount: number;
  apSettlementCount: number;

  // AR (Sales Contracts - they owe us)
  totalArAmount: number;
  paidArAmount: number;
  unpaidArAmount: number;
  arSettlementCount: number;

  // Net Position
  netAmount: number;
  netDirection: string;  // "We Owe", "They Owe Us", "Balanced"
}

export const tradingPartnerExposureApi = {
  /**
   * Get credit exposure and risk level for a specific trading partner
   */
  getPartnerExposure: async (partnerId: string) => {
    const response = await api.get<TradingPartnerExposureDto>(
      `/trading-partners/${partnerId}/exposure`
    );
    return response.data;
  },

  /**
   * Get all trading partners sorted by risk level
   */
  getAllExposure: async (
    sortBy?: string,
    sortDescending: boolean = true,
    pageNumber?: number,
    pageSize?: number
  ) => {
    const params = new URLSearchParams();
    if (sortBy) params.append('sortBy', sortBy);
    params.append('sortDescending', sortDescending.toString());
    if (pageNumber) params.append('pageNumber', pageNumber.toString());
    if (pageSize) params.append('pageSize', pageSize.toString());

    const response = await api.get<TradingPartnerExposureDto[]>(
      `/trading-partners/exposure/all?${params.toString()}`
    );
    return response.data;
  },

  /**
   * Get trading partners with high or critical risk levels
   */
  getAtRiskPartners: async (minimumRiskLevel: number = 3) => {
    const response = await api.get<TradingPartnerExposureDto[]>(
      `/trading-partners/exposure/at-risk`,
      {
        params: { minimumRiskLevel },
      }
    );
    return response.data;
  },

  /**
   * Get detailed settlement summary for a trading partner (AP and AR breakdown)
   */
  getSettlementDetails: async (partnerId: string) => {
    const response = await api.get<PartnerSettlementSummaryDto>(
      `/trading-partners/${partnerId}/settlement-details`
    );
    return response.data;
  },
};

export default tradingPartnerExposureApi;
