export interface TradingPartner {
  id: string;
  companyName: string;
  companyCode: string;
  partnerType: string;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  taxNumber?: string;
  
  // Credit Management
  creditLimit: number;
  creditLimitValidUntil: string;
  paymentTermDays: number;
  currentExposure: number;
  creditUtilization: number;
  
  // Statistics
  totalPurchaseAmount: number;
  totalSalesAmount: number;
  totalTransactions: number;
  lastTransactionDate?: string;
  
  // Status
  isActive: boolean;
  isBlocked: boolean;
  blockReason?: string;
  
  // Calculated fields
  isCreditExceeded: boolean;
  isCreditExpired: boolean;
}

export interface TradingPartnerSummary {
  id: string;
  companyName: string;
  companyCode: string;
  partnerType: string;
  creditLimit: number;
  currentExposure: number;
  creditUtilization: number;
  isActive: boolean;
  isCreditExceeded: boolean;
}

export interface CreateTradingPartnerRequest {
  companyName: string;
  partnerType: string;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  taxNumber?: string;
  creditLimit: number;
  creditLimitValidUntil: string;
  paymentTermDays?: number;
}

export interface UpdateTradingPartnerRequest {
  companyName: string;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  taxNumber?: string;
  creditLimit: number;
  creditLimitValidUntil: string;
  paymentTermDays: number;
  isActive: boolean;
  isBlocked: boolean;
  blockReason?: string;
}

export interface GetTradingPartnersParams {
  partnerType?: string;
  isActive?: boolean;
  searchTerm?: string;
}

export interface PagedResult<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export enum PartnerType {
  Supplier = 'Supplier',
  Customer = 'Customer',
  Both = 'Both'
}

// Import financial report related types
import { TradingPartnerAnalysis, FinancialReportSummary } from './financialReport';

// Extended interfaces for analysis
export interface TradingPartnerWithAnalysis extends TradingPartner {
  analysis?: TradingPartnerAnalysis;
  financialReports?: FinancialReportSummary[];
}