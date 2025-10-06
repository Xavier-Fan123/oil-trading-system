import { 
  TradingPartner, 
  TradingPartnerSummary, 
  CreateTradingPartnerRequest, 
  UpdateTradingPartnerRequest,
 
} from '../types/tradingPartner';
import { TradingPartnerAnalysis, FinancialReportSummary } from '../types/financialReport';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

class TradingPartnerService {
  private async makeRequest<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    return response.json();
  }

  async getTradingPartners(): Promise<TradingPartnerSummary[]> {
    return this.makeRequest<TradingPartnerSummary[]>('/trading-partners');
  }

  async getTradingPartnerById(id: string): Promise<TradingPartner> {
    return this.makeRequest<TradingPartner>(`/trading-partners/${id}`);
  }

  async createTradingPartner(request: CreateTradingPartnerRequest): Promise<TradingPartner> {
    return this.makeRequest<TradingPartner>('/trading-partners', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async updateTradingPartner(id: string, request: UpdateTradingPartnerRequest): Promise<void> {
    await this.makeRequest(`/trading-partners/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async deleteTradingPartner(id: string): Promise<void> {
    await this.makeRequest(`/trading-partners/${id}`, {
      method: 'DELETE',
    });
  }

  async blockTradingPartner(id: string, reason: string): Promise<void> {
    await this.makeRequest(`/trading-partners/${id}/block`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    });
  }

  async unblockTradingPartner(id: string): Promise<void> {
    await this.makeRequest(`/trading-partners/${id}/unblock`, {
      method: 'POST',
    });
  }

  async getSuppliers(): Promise<TradingPartnerSummary[]> {
    return this.makeRequest<TradingPartnerSummary[]>('/trading-partners?type=Supplier');
  }

  async getCustomers(): Promise<TradingPartnerSummary[]> {
    return this.makeRequest<TradingPartnerSummary[]>('/trading-partners?type=Customer');
  }

  // Financial Analysis Methods
  async getAnalysis(id: string): Promise<TradingPartnerAnalysis> {
    return this.makeRequest<TradingPartnerAnalysis>(`/trading-partners/${id}/analysis`);
  }

  async getFinancialReports(id: string, year?: number): Promise<FinancialReportSummary[]> {
    let endpoint = `/trading-partners/${id}/financial-reports`;
    if (year) {
      endpoint += `?year=${year}`;
    }
    return this.makeRequest<FinancialReportSummary[]>(endpoint);
  }

  async getTradingPartnerWithAnalysis(id: string): Promise<TradingPartner & { 
    analysis: TradingPartnerAnalysis; 
    financialReports: FinancialReportSummary[] 
  }> {
    return this.makeRequest<TradingPartner & { 
      analysis: TradingPartnerAnalysis; 
      financialReports: FinancialReportSummary[] 
    }>(`/trading-partners/${id}/with-analysis`);
  }

  // Risk and Credit Assessment
  async getCreditRiskAssessment(id: string): Promise<{
    creditRisk: string;
    riskScore: number;
    riskFactors: string[];
    recommendations: string[];
  }> {
    return this.makeRequest<{
      creditRisk: string;
      riskScore: number;
      riskFactors: string[];
      recommendations: string[];
    }>(`/trading-partners/${id}/credit-risk`);
  }

  // Financial Health Summary for Dashboard
  async getFinancialHealthSummary(): Promise<{
    totalPartners: number;
    highRiskPartners: number;
    averageHealthScore: number;
    partnersWithRecentReports: number;
  }> {
    return this.makeRequest<{
      totalPartners: number;
      highRiskPartners: number;
      averageHealthScore: number;
      partnersWithRecentReports: number;
    }>('/trading-partners/financial-health-summary');
  }

  // Bulk operations for analysis
  async getBulkAnalysis(ids: string[]): Promise<Record<string, TradingPartnerAnalysis>> {
    return this.makeRequest<Record<string, TradingPartnerAnalysis>>('/trading-partners/bulk-analysis', {
      method: 'POST',
      body: JSON.stringify({ tradingPartnerIds: ids }),
    });
  }
}

export const tradingPartnerService = new TradingPartnerService();