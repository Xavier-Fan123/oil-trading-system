/**
 * Financial Report types for the Oil Trading System
 * 
 * These interfaces define the structure for financial reporting data
 * and analysis components, ensuring perfect alignment with backend DTOs.
 */

export interface FinancialReport {
  id: string;
  tradingPartnerId: string;
  reportStartDate: string;
  reportEndDate: string;
  
  // Income Statement Data
  revenue?: number;
  costOfGoodsSold?: number;
  grossProfit?: number;
  operatingExpenses?: number;
  operatingIncome?: number;
  interestExpense?: number;
  interestIncome?: number;
  netIncome?: number;
  
  // Balance Sheet Data
  totalAssets?: number;
  currentAssets?: number;
  nonCurrentAssets?: number;
  totalLiabilities?: number;
  currentLiabilities?: number;
  longTermDebt?: number;
  totalEquity?: number;
  retainedEarnings?: number;
  
  // Cash Flow Data
  operatingCashFlow?: number;
  investingCashFlow?: number;
  financingCashFlow?: number;
  netCashFlow?: number;
  cashAndEquivalents?: number;
  
  // Additional Financial Metrics
  workingCapital?: number;
  totalDebt?: number;
  bookValue?: number;
  
  // Audit and Metadata
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  updatedBy?: string;
  notes?: string;
  isAudited?: boolean;
  auditFirm?: string;
}

export interface FinancialReportSummary {
  id: string;
  tradingPartnerId: string;
  tradingPartnerName: string;
  reportStartDate: string;
  reportEndDate: string;
  revenue?: number;
  netIncome?: number;
  totalAssets?: number;
  totalEquity?: number;
  createdAt: string;
  isAudited?: boolean;
  reportYear: number;
}

export interface CreateFinancialReportRequest {
  tradingPartnerId: string;
  reportStartDate: string;
  reportEndDate: string;
  
  // Income Statement Data (optional)
  revenue?: number;
  costOfGoodsSold?: number;
  grossProfit?: number;
  operatingExpenses?: number;
  operatingIncome?: number;
  interestExpense?: number;
  interestIncome?: number;
  netIncome?: number;
  
  // Balance Sheet Data (optional)
  totalAssets?: number;
  currentAssets?: number;
  nonCurrentAssets?: number;
  totalLiabilities?: number;
  currentLiabilities?: number;
  longTermDebt?: number;
  totalEquity?: number;
  retainedEarnings?: number;
  
  // Cash Flow Data (optional)
  operatingCashFlow?: number;
  investingCashFlow?: number;
  financingCashFlow?: number;
  netCashFlow?: number;
  cashAndEquivalents?: number;
  
  // Additional Financial Metrics (optional)
  workingCapital?: number;
  totalDebt?: number;
  bookValue?: number;
  
  // Metadata
  notes?: string;
  isAudited?: boolean;
  auditFirm?: string;
}

export interface UpdateFinancialReportRequest {
  reportStartDate: string;
  reportEndDate: string;
  
  // Income Statement Data (optional)
  revenue?: number;
  costOfGoodsSold?: number;
  grossProfit?: number;
  operatingExpenses?: number;
  operatingIncome?: number;
  interestExpense?: number;
  interestIncome?: number;
  netIncome?: number;
  
  // Balance Sheet Data (optional)
  totalAssets?: number;
  currentAssets?: number;
  nonCurrentAssets?: number;
  totalLiabilities?: number;
  currentLiabilities?: number;
  longTermDebt?: number;
  totalEquity?: number;
  retainedEarnings?: number;
  
  // Cash Flow Data (optional)
  operatingCashFlow?: number;
  investingCashFlow?: number;
  financingCashFlow?: number;
  netCashFlow?: number;
  cashAndEquivalents?: number;
  
  // Additional Financial Metrics (optional)
  workingCapital?: number;
  totalDebt?: number;
  bookValue?: number;
  
  // Metadata
  notes?: string;
  isAudited?: boolean;
  auditFirm?: string;
}

export interface FinancialReportFilters {
  tradingPartnerId?: string;
  year?: number;
  startDate?: string;
  endDate?: string;
  hasRevenue?: boolean;
  minTotalAssets?: number;
  isAudited?: boolean;
  auditFirm?: string;
}

// Analysis and Calculation Types
export interface FinancialRatio {
  name: string;
  value: number | null;
  description: string;
  category: FinancialRatioCategory;
  isHealthy?: boolean;
  benchmark?: number;
}

export interface FinancialTrend {
  metric: string;
  periods: FinancialTrendPeriod[];
  growthRate?: number;
  isPositiveTrend: boolean;
}

export interface FinancialTrendPeriod {
  year: number;
  value: number;
  yearOverYearChange?: number;
  yearOverYearPercentChange?: number;
}

export interface TradingPartnerAnalysis {
  tradingPartnerId: string;
  tradingPartnerName: string;
  companyCode: string;
  partnerType: string;
  
  // Credit and Trading Information
  creditLimit: number;
  currentExposure: number;
  creditUtilization: number;
  totalCooperationAmount: number;
  totalCooperationQuantity: number;
  
  // Financial Health Analysis
  financialHealthScore?: number;
  financialHealthStatus: FinancialHealthStatus;
  creditRisk: CreditRisk;
  
  // Financial Ratios
  ratios: FinancialRatio[];
  
  // Trend Analysis
  trends: FinancialTrend[];
  
  // Recent Financial Data
  latestFinancialReport?: FinancialReportSummary;
  financialReportCount: number;
  
  // Analysis Metadata
  analysisDate: string;
  dataQuality: DataQuality;
  recommendations: string[];
}

export interface FinancialHealthIndicator {
  name: string;
  value: number;
  status: FinancialHealthStatus;
  weight: number;
  description: string;
}

// Enums and Status Types
export enum FinancialRatioCategory {
  Liquidity = 'Liquidity',
  Profitability = 'Profitability',
  Efficiency = 'Efficiency',
  Leverage = 'Leverage',
  Market = 'Market'
}

export enum FinancialHealthStatus {
  Excellent = 'Excellent',
  Good = 'Good',
  Fair = 'Fair',
  Poor = 'Poor',
  Critical = 'Critical',
  Unknown = 'Unknown'
}

export enum CreditRisk {
  VeryLow = 'VeryLow',
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  VeryHigh = 'VeryHigh'
}

export enum DataQuality {
  Excellent = 'Excellent',
  Good = 'Good',
  Fair = 'Fair',
  Poor = 'Poor',
  Insufficient = 'Insufficient'
}

// Form-related types
export interface FinancialReportFormData {
  tradingPartnerId: string;
  reportStartDate: string;
  reportEndDate: string;
  
  // Income Statement
  revenue: number | '';
  costOfGoodsSold: number | '';
  grossProfit: number | '';
  operatingExpenses: number | '';
  operatingIncome: number | '';
  interestExpense: number | '';
  interestIncome: number | '';
  netIncome: number | '';
  
  // Balance Sheet
  totalAssets: number | '';
  currentAssets: number | '';
  nonCurrentAssets: number | '';
  totalLiabilities: number | '';
  currentLiabilities: number | '';
  longTermDebt: number | '';
  totalEquity: number | '';
  retainedEarnings: number | '';
  
  // Cash Flow
  operatingCashFlow: number | '';
  investingCashFlow: number | '';
  financingCashFlow: number | '';
  netCashFlow: number | '';
  cashAndEquivalents: number | '';
  
  // Additional Metrics
  workingCapital: number | '';
  totalDebt: number | '';
  bookValue: number | '';
  
  // Metadata
  notes: string;
  isAudited: boolean;
  auditFirm: string;
}

// Chart data types
export interface ChartDataPoint {
  year: number;
  value: number;
  label?: string;
}

export interface FinancialChartData {
  revenue: ChartDataPoint[];
  netIncome: ChartDataPoint[];
  totalAssets: ChartDataPoint[];
  totalEquity: ChartDataPoint[];
}

// Validation types
export interface FinancialReportValidationError {
  field: string;
  message: string;
  code: string;
}

export interface FinancialReportValidationResult {
  isValid: boolean;
  errors: FinancialReportValidationError[];
  warnings: string[];
}

// Grid/Table types for DataGrid
export interface FinancialReportGridRow {
  id: string;
  reportStartDate: string;
  reportEndDate: string;
  revenue: number | null;
  netIncome: number | null;
  totalAssets: number | null;
  totalEquity: number | null;
  isAudited: boolean;
  createdAt: string;
  actions?: string; // For action buttons
}

// Constants
export const FINANCIAL_REPORT_FIELD_LABELS: Record<string, string> = {
  revenue: 'Revenue',
  costOfGoodsSold: 'Cost of Goods Sold',
  grossProfit: 'Gross Profit',
  operatingExpenses: 'Operating Expenses',
  operatingIncome: 'Operating Income',
  interestExpense: 'Interest Expense',
  interestIncome: 'Interest Income',
  netIncome: 'Net Income',
  totalAssets: 'Total Assets',
  currentAssets: 'Current Assets',
  nonCurrentAssets: 'Non-Current Assets',
  totalLiabilities: 'Total Liabilities',
  currentLiabilities: 'Current Liabilities',
  longTermDebt: 'Long-Term Debt',
  totalEquity: 'Total Equity',
  retainedEarnings: 'Retained Earnings',
  operatingCashFlow: 'Operating Cash Flow',
  investingCashFlow: 'Investing Cash Flow',
  financingCashFlow: 'Financing Cash Flow',
  netCashFlow: 'Net Cash Flow',
  cashAndEquivalents: 'Cash and Equivalents',
  workingCapital: 'Working Capital',
  totalDebt: 'Total Debt',
  bookValue: 'Book Value'
};