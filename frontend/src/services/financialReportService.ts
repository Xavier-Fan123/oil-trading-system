import { 
  FinancialReport,
  FinancialReportSummary,
  CreateFinancialReportRequest, 
  UpdateFinancialReportRequest,
  FinancialReportFilters
} from '../types/financialReport';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

class FinancialReportService {
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

  async getFinancialReports(tradingPartnerId?: string, year?: number): Promise<FinancialReportSummary[]> {
    let endpoint = '/financial-reports';
    const params = new URLSearchParams();
    
    if (tradingPartnerId) {
      params.append('tradingPartnerId', tradingPartnerId);
    }
    if (year) {
      params.append('year', year.toString());
    }
    
    if (params.toString()) {
      endpoint += `?${params.toString()}`;
    }
    
    return this.makeRequest<FinancialReportSummary[]>(endpoint);
  }

  async getFinancialReportById(id: string): Promise<FinancialReport> {
    return this.makeRequest<FinancialReport>(`/financial-reports/${id}`);
  }

  async createFinancialReport(request: CreateFinancialReportRequest): Promise<FinancialReport> {
    return this.makeRequest<FinancialReport>('/financial-reports', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async updateFinancialReport(id: string, request: UpdateFinancialReportRequest): Promise<FinancialReport> {
    return this.makeRequest<FinancialReport>(`/financial-reports/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async deleteFinancialReport(id: string): Promise<void> {
    await this.makeRequest(`/financial-reports/${id}`, {
      method: 'DELETE',
    });
  }

  async getFinancialReportsWithFilters(filters: FinancialReportFilters): Promise<FinancialReportSummary[]> {
    const params = new URLSearchParams();
    
    if (filters.tradingPartnerId) {
      params.append('tradingPartnerId', filters.tradingPartnerId);
    }
    if (filters.year) {
      params.append('year', filters.year.toString());
    }
    if (filters.startDate) {
      params.append('startDate', filters.startDate);
    }
    if (filters.endDate) {
      params.append('endDate', filters.endDate);
    }
    if (filters.hasRevenue !== undefined) {
      params.append('hasRevenue', filters.hasRevenue.toString());
    }
    if (filters.minTotalAssets) {
      params.append('minTotalAssets', filters.minTotalAssets.toString());
    }
    
    return this.makeRequest<FinancialReportSummary[]>(`/financial-reports?${params.toString()}`);
  }

  async validateFinancialReportPeriod(
    tradingPartnerId: string, 
    startDate: string, 
    endDate: string,
    excludeId?: string
  ): Promise<{ isValid: boolean; conflictingReports: string[] }> {
    const params = new URLSearchParams({
      tradingPartnerId,
      startDate,
      endDate
    });
    
    if (excludeId) {
      params.append('excludeId', excludeId);
    }
    
    return this.makeRequest<{ isValid: boolean; conflictingReports: string[] }>(
      `/financial-reports/validate-period?${params.toString()}`
    );
  }

  async getBulkFinancialData(tradingPartnerIds: string[]): Promise<Record<string, FinancialReportSummary[]>> {
    return this.makeRequest<Record<string, FinancialReportSummary[]>>('/financial-reports/bulk', {
      method: 'POST',
      body: JSON.stringify({ tradingPartnerIds }),
    });
  }
}

export const financialReportService = new FinancialReportService();