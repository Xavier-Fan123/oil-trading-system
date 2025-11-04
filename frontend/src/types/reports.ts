/**
 * Contract Execution Report DTO
 */
export interface ContractExecutionReportDto {
  id: string;
  contractId: string;
  contractNumber: string;
  contractType: 'Purchase' | 'Sales';
  reportGeneratedDate: string; // ISO 8601 format

  // Contract Basic Information
  tradingPartnerId?: string;
  tradingPartnerName: string;
  productId?: string;
  productName: string;
  quantity: number;
  quantityUnit: string;
  contractStatus: string;

  // Execution Metrics
  contractValue?: number;
  currency?: string;
  executedQuantity?: number;
  executionPercentage: number;

  // Dates
  createdDate?: string; // ISO 8601 format
  activatedDate?: string; // ISO 8601 format
  laycanStart?: string; // ISO 8601 format
  laycanEnd?: string; // ISO 8601 format
  estimatedDeliveryDate?: string; // ISO 8601 format
  actualDeliveryDate?: string; // ISO 8601 format
  settlementDate?: string; // ISO 8601 format
  completionDate?: string; // ISO 8601 format

  // Settlement Information
  settlementCount: number;
  totalSettledAmount: number;
  paidSettledAmount: number;
  unpaidSettledAmount: number;
  paymentStatus: 'NotDue' | 'NotPaid' | 'PartiallyPaid' | 'Paid';

  // Shipping/Logistics Information
  shippingOperationCount: number;
  loadPort?: string;
  dischargePort?: string;
  deliveryTerms?: string;

  // Performance Indicators
  daysToActivation: number;
  daysToCompletion: number;
  isOnSchedule: boolean;
  executionStatus: 'OnTrack' | 'Delayed' | 'Completed' | 'Cancelled';

  // Pricing Information
  benchmarkPrice?: number;
  adjustmentPrice?: number;
  finalPrice?: number;
  isPriceFinalized: boolean;

  // Risk & Compliance
  hasRiskViolations: boolean;
  isCompliant: boolean;

  // Metadata
  notes?: string;
  lastUpdatedDate: string; // ISO 8601 format
}

/**
 * Filter criteria for contract execution reports
 */
export interface ContractExecutionReportFilter {
  contractType?: 'Purchase' | 'Sales';
  executionStatus?: 'OnTrack' | 'Delayed' | 'Completed' | 'Cancelled';
  fromDate?: Date;
  toDate?: Date;
  tradingPartnerId?: string;
  productId?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

/**
 * Export options for reports
 */
export interface ExportOptions {
  format: 'csv' | 'excel' | 'pdf';
  includeFilters?: boolean;
  fileName?: string;
}

/**
 * Report summary statistics
 */
export interface ReportSummary {
  totalContracts: number;
  completedContracts: number;
  delayedContracts: number;
  onTrackContracts: number;
  totalContractValue: number;
  totalSettledAmount: number;
  averageExecutionPercentage: number;
  paymentCompletionRate: number;
}
