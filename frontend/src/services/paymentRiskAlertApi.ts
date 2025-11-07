import axios, { AxiosError } from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Enums
export enum AlertSeverity {
  Info = 1,
  Warning = 2,
  Critical = 3,
}

export enum AlertType {
  OverduePayment = 1,
  UpcomingDueDate = 2,
  CreditLimitApproaching = 3,
  CreditLimitExceeded = 4,
  CreditExpired = 5,
  LargeOutstandingAmount = 6,
  FrequentLatePayment = 7,
}

// DTOs
export interface PaymentRiskAlertDto {
  alertId: string;
  tradingPartnerId: string;
  companyName: string;
  companyCode: string;
  alertType: AlertType;
  severity: AlertSeverity;
  title: string;
  description: string;
  amount: number;
  currency: string;
  dueDate: string | null;
  createdDate: string;
  resolvedDate: string | null;
  isResolved: boolean;
  settlementId?: string;
  contractNumber?: string;
  creditUtilizationPercentage?: number;
  creditLimit?: number;
  daysOverdue?: number;
  daysUntilDue?: number;
}

export interface CreatePaymentRiskAlertRequest {
  tradingPartnerId: string;
  alertType: AlertType;
  severity: AlertSeverity;
  title: string;
  description: string;
  amount: number;
  currency: string;
  dueDate?: string;
  settlementId?: string;
  contractNumber?: string;
}

export interface PaymentRiskAlertSummaryDto {
  totalAlerts: number;
  criticalAlerts: number;
  warningAlerts: number;
  infoAlerts: number;
  unresolvedAlerts: number;
  resolvedAlerts: number;
  totalAmountAtRisk: number;
  criticalAmountAtRisk: number;
  warningAmountAtRisk: number;
  overduePaymentCount: number;
  upcomingDueDateCount: number;
  creditLimitExceededCount: number;
  creditLimitApproachingCount: number;
  creditExpiredCount: number;
  largeOutstandingAmountCount: number;
  frequentLatePaymentCount: number;
  tradingPartnersWithAlerts: number;
  tradingPartnersWithCriticalAlerts: number;
}

export interface PaymentRiskAlertFilterRequest {
  tradingPartnerId?: string;
  alertType?: AlertType;
  severity?: AlertSeverity;
  onlyUnresolved?: boolean;
  fromDate?: string;
  toDate?: string;
  pageNumber: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

// API Service
const paymentRiskAlertApi = {
  // Get all alerts with filtering and pagination
  async getAlerts(
    tradingPartnerId?: string,
    alertType?: AlertType,
    severity?: AlertSeverity,
    onlyUnresolved: boolean = true,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Promise<PagedResult<PaymentRiskAlertDto>> {
    try {
      const response = await api.get('/payment-risk-alerts', {
        params: {
          tradingPartnerId,
          alertType,
          severity,
          onlyUnresolved,
          pageNumber,
          pageSize,
        },
      });
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to fetch payment risk alerts');
    }
  },

  // Get alert summary statistics
  async getAlertSummary(): Promise<PaymentRiskAlertSummaryDto> {
    try {
      const response = await api.get('/payment-risk-alerts/summary');
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to fetch alert summary');
    }
  },

  // Get alerts for specific trading partner
  async getPartnerAlerts(tradingPartnerId: string): Promise<PaymentRiskAlertDto[]> {
    try {
      const response = await api.get(`/payment-risk-alerts/partner/${tradingPartnerId}`);
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to fetch partner alerts');
    }
  },

  // Get alert by ID
  async getAlertById(alertId: string): Promise<PaymentRiskAlertDto> {
    try {
      const response = await api.get(`/payment-risk-alerts/${alertId}`);
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to fetch alert');
    }
  },

  // Create new alert
  async createAlert(request: CreatePaymentRiskAlertRequest): Promise<PaymentRiskAlertDto> {
    try {
      const response = await api.post('/payment-risk-alerts', request);
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to create alert');
    }
  },

  // Resolve alert
  async resolveAlert(alertId: string): Promise<PaymentRiskAlertDto> {
    try {
      const response = await api.put(`/payment-risk-alerts/${alertId}/resolve`);
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to resolve alert');
    }
  },

  // Generate automatic alerts
  async generateAutomaticAlerts(): Promise<PaymentRiskAlertSummaryDto> {
    try {
      const response = await api.post('/payment-risk-alerts/generate-automatic');
      return response.data;
    } catch (error) {
      throw handleApiError(error, 'Failed to generate automatic alerts');
    }
  },
};

// Error handling utility
function handleApiError(error: unknown, defaultMessage: string): Error {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<{ error?: string; message?: string }>;
    if (axiosError.response?.data?.error) {
      return new Error(axiosError.response.data.error);
    }
    if (axiosError.response?.data?.message) {
      return new Error(axiosError.response.data.message);
    }
  }
  return new Error(defaultMessage);
}

export default paymentRiskAlertApi;
