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

  // Contact Information
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  taxNumber?: string;

  // Credit Management - MUST PERSIST
  creditLimit: number;
  creditLimitValidUntil: string;  // ← Added to fix persistence
  paymentTermDays: number;  // ← Added to fix persistence
  currentExposure: number;
  creditUtilization: number;

  // Status - MUST PERSIST
  isActive: boolean;
  isBlocked: boolean;  // ← Added to fix persistence
  blockReason?: string;  // ← Added to fix persistence
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
  isActive?: boolean;
  isBlocked?: boolean;
  blockReason?: string;
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