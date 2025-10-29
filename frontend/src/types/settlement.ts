// Settlement-related types and enums for the frontend

// Enums matching the backend
export enum DocumentType {
  BillOfLading = 1,
  QuantityCertificate = 2,
  QualityCertificate = 3,
  Other = 99
}

export enum ContractSettlementStatus {
  Draft = 1,
  DataEntered = 2,
  Calculated = 3,
  Reviewed = 4,
  Approved = 5,
  Finalized = 6,
  Cancelled = 7
}

export enum ChargeType {
  Demurrage = 1,        // Demurrage fee
  Despatch = 2,         // Despatch fee
  InspectionFee = 3,    // Inspection fee
  PortCharges = 4,      // Port charges
  FreightCost = 5,      // Freight cost
  InsurancePremium = 6, // Insurance premium
  BankCharges = 7,      // Bank charges
  StorageFee = 8,       // Storage fee
  AgencyFee = 9,        // Agency fee
  Other = 99            // Other charges
}

export enum QuantityUnit {
  MT = 1,
  BBL = 2,
  GAL = 3,
  LOTS = 4
}

export enum CalculationMode {
  UseActualQuantities = 1,
  UseMTForAll = 2,
  UseBBLForAll = 3,
  UseContractSpecified = 4
}

// Main settlement DTOs
export interface ContractSettlementDto {
  id: string;
  
  // Contract reference information
  contractId: string;
  contractNumber: string;
  externalContractNumber: string;
  
  // Document information (B/L or CQ)
  documentNumber?: string;
  documentType: string;
  documentDate: Date;
  
  // Actual quantities from B/L or CQ
  actualQuantityMT: number;
  actualQuantityBBL: number;
  
  // Calculation quantities (may differ based on calculation mode)
  calculationQuantityMT: number;
  calculationQuantityBBL: number;
  quantityCalculationNote?: string;
  
  // Price information (from market data)
  benchmarkPrice: number;
  benchmarkPriceFormula?: string;
  pricingStartDate?: Date;
  pricingEndDate?: Date;
  benchmarkPriceCurrency: string;
  
  // Calculation results
  benchmarkAmount: number;    // Benchmark price calculation
  adjustmentAmount: number;   // Adjustment price calculation
  cargoValue: number;         // Subtotal: benchmark + adjustment
  totalCharges: number;       // Sum of all charges
  totalSettlementAmount: number; // Final settlement amount
  settlementCurrency: string;
  
  // Exchange rate handling
  exchangeRate?: number;
  exchangeRateNote?: string;
  
  // Status management
  status: string;
  isFinalized: boolean;
  createdDate: Date;
  lastModifiedDate?: Date;
  createdBy: string;
  lastModifiedBy?: string;
  finalizedDate?: Date;
  finalizedBy?: string;
  
  // Navigation properties
  purchaseContract?: PurchaseContractSummaryDto;
  salesContract?: SalesContractSummaryDto;
  charges: SettlementChargeDto[];
  
  // Computed properties for UI
  canBeModified: boolean;
  requiresRecalculation: boolean;
  netCharges: number;
  displayStatus: string;
  formattedTotalAmount: string;
  formattedCargoValue: string;
  formattedTotalCharges: string;
}

export interface ContractSettlementListDto {
  id: string;
  contractId: string;
  contractNumber: string;
  externalContractNumber: string;
  documentNumber?: string;
  documentType: string;
  documentDate: Date;
  actualQuantityMT: number;
  actualQuantityBBL: number;
  totalSettlementAmount: number;
  settlementCurrency: string;
  status: string;
  isFinalized: boolean;
  createdDate: Date;
  createdBy: string;
  chargesCount: number;
  formattedAmount: string;
  displayStatus: string;
}

export interface ContractSettlementSummaryDto {
  id: string;
  contractId: string;
  contractNumber: string;
  documentNumber?: string;
  documentType: string;
  totalSettlementAmount: number;
  settlementCurrency: string;
  status: string;
  isFinalized: boolean;
  documentDate: Date;
  formattedAmount: string;
}

// Settlement Charge DTOs
export interface SettlementChargeDto {
  id: string;
  settlementId: string;
  chargeType: string;
  chargeTypeDisplayName: string;
  description: string;
  amount: number;
  currency: string;
  incurredDate?: Date;
  referenceDocument?: string;
  notes?: string;
  createdDate: Date;
  createdBy: string;
  
  // Computed properties for UI
  formattedAmount: string;
  formattedIncurredDate: string;
  isNegativeCharge: boolean;
  isPositiveCharge: boolean;
}

export interface SettlementChargeListDto {
  id: string;
  settlementId: string;
  chargeType: string;
  chargeTypeDisplayName: string;
  description: string;
  amount: number;
  currency: string;
  incurredDate?: Date;
  referenceDocument?: string;
  createdDate: Date;
  formattedAmount: string;
  chargeTypeCode: string;
}

export interface ChargeTypeBreakdownDto {
  chargeType: string;
  chargeTypeDisplayName: string;
  totalAmount: number;
  currency: string;
  count: number;
  averageAmount: number;
  formattedTotalAmount: string;
  formattedAverageAmount: string;
}

export interface SettlementChargeStatisticsDto {
  settlementId: string;
  totalChargesAmount: number;
  currency: string;
  totalChargesCount: number;
  positiveChargesTotal: number;
  negativeChargesTotal: number;
  positiveChargesCount: number;
  negativeChargesCount: number;
  chargeTypeBreakdown: ChargeTypeBreakdownDto[];
  formattedTotalAmount: string;
  formattedPositiveTotal: string;
  formattedNegativeTotal: string;
  netCharges: number;
  formattedNetCharges: string;
}

// Creation and Update DTOs
export interface CreateSettlementDto {
  contractId: string;
  documentNumber?: string;
  documentType: DocumentType;
  documentDate: Date;
  actualQuantityMT: number;
  actualQuantityBBL: number;
  createdBy: string;
  notes?: string;
  exchangeRate?: number;
  exchangeRateNote?: string;
  settlementCurrency: string;
  overrideCalculationQuantityMT?: number;
  overrideCalculationQuantityBBL?: number;
  quantityCalculationNote?: string;
  autoCalculatePrices: boolean;
  autoTransitionStatus: boolean;
}

export interface CreateSettlementWithContextDto extends CreateSettlementDto {
  externalContractNumber?: string;
  contractNumber?: string;
  expectedContractType?: string;
  tradingPartnerId?: string;
  productId?: string;
  initialCharges: CreateInitialChargeDto[];
}

export interface CreateInitialChargeDto {
  chargeType: ChargeType;
  description: string;
  amount: number;
  currency: string;
  incurredDate?: Date;
  referenceDocument?: string;
  notes?: string;
}

export interface UpdateSettlementDto {
  documentNumber?: string;
  documentType?: DocumentType;
  documentDate?: Date;
  actualQuantityMT?: number;
  actualQuantityBBL?: number;
  notes?: string;
}

export interface CreateSettlementResultDto {
  isSuccessful: boolean;
  settlementId?: string;
  errorMessage?: string;
  warnings: string[];
  validationErrors: string[];
  settlement?: ContractSettlementDto;
  calculationSummary?: SettlementCalculationSummaryDto;
}

export interface SettlementCalculationSummaryDto {
  pricesCalculated: boolean;
  quantitiesCalculated: boolean;
  chargesCalculated: boolean;
  benchmarkPrice: number;
  benchmarkPriceFormula?: string;
  pricingPeriodStart?: Date;
  pricingPeriodEnd?: Date;
  calculationQuantityMT: number;
  calculationQuantityBBL: number;
  quantityCalculationNote?: string;
  initialChargesAdded: number;
  totalInitialCharges: number;
  currency: string;
}

// Charge Management DTOs
export interface AddChargeDto {
  settlementId: string;
  chargeType: ChargeType;
  description: string;
  amount: number;
  referenceDocument?: string;
  addedBy: string;
}

export interface UpdateChargeDto {
  settlementId: string;
  chargeId: string;
  chargeType?: ChargeType;
  description?: string;
  amount?: number;
  referenceDocument?: string;
  updatedBy: string;
}

export interface ChargeOperationResultDto {
  isSuccessful: boolean;
  chargeId?: string;
  errorMessage?: string;
  validationErrors: string[];
  charge?: SettlementChargeDto;
  updatedTotals?: SettlementTotalsDto;
}

export interface SettlementTotalsDto {
  cargoValue: number;
  totalCharges: number;
  totalSettlementAmount: number;
  currency: string;
  chargesCount: number;
}

// Contract Summary DTOs (used in settlements)
export interface PurchaseContractSummaryDto {
  id: string;
  contractNumber: string;
  externalContractNumber?: string;
  supplierName: string;
  productName: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  pricingType: string;
  laycanStart: Date;
  laycanEnd: Date;
  status: string;
}

export interface SalesContractSummaryDto {
  id: string;
  contractNumber: string;
  customerName: string;
  productName: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  pricingType: string;
  laycanStart: Date;
  laycanEnd: Date;
  status: string;
}

// Search and Filtering DTOs
export interface SettlementSearchFilters {
  startDate?: Date;
  endDate?: Date;
  status?: ContractSettlementStatus;
  contractId?: string;
  externalContractNumber?: string;
  documentNumber?: string;
  pageNumber: number;
  pageSize: number;
}

export interface SettlementSearchResult {
  data: ContractSettlementListDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Paginated result wrapper
export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Utility types for forms
export interface SettlementFormData {
  contractId: string;
  externalContractNumber?: string;
  documentNumber?: string;
  documentType: DocumentType;
  documentDate: Date;
  actualQuantityMT: number;
  actualQuantityBBL: number;
  notes?: string;
  charges: ChargeFormData[];
}

export interface ChargeFormData {
  id?: string;
  chargeType: ChargeType;
  description: string;
  amount: number;
  currency: string;
  incurredDate?: Date;
  referenceDocument?: string;
  notes?: string;
}

// UI-specific types
export interface SettlementTableRow extends ContractSettlementListDto {
  actions?: React.ReactNode;
}

export interface ChargeTableRow extends SettlementChargeDto {
  actions?: React.ReactNode;
}

// Display helper types
export interface DisplayEnumValue {
  value: number | string;
  label: string;
  description?: string;
}

// Constants for display
export const DocumentTypeLabels: Record<DocumentType, string> = {
  [DocumentType.BillOfLading]: 'Bill of Lading',
  [DocumentType.QuantityCertificate]: 'Quantity Certificate',
  [DocumentType.QualityCertificate]: 'Quality Certificate',
  [DocumentType.Other]: 'Other'
};

export const ContractSettlementStatusLabels: Record<ContractSettlementStatus, string> = {
  [ContractSettlementStatus.Draft]: 'Draft',
  [ContractSettlementStatus.DataEntered]: 'Data Entered',
  [ContractSettlementStatus.Calculated]: 'Calculated',
  [ContractSettlementStatus.Reviewed]: 'Reviewed',
  [ContractSettlementStatus.Approved]: 'Approved',
  [ContractSettlementStatus.Finalized]: 'Finalized',
  [ContractSettlementStatus.Cancelled]: 'Cancelled'
};

export const ChargeTypeLabels: Record<ChargeType, string> = {
  [ChargeType.Demurrage]: 'Demurrage',
  [ChargeType.Despatch]: 'Despatch',
  [ChargeType.InspectionFee]: 'Inspection Fee',
  [ChargeType.PortCharges]: 'Port Charges',
  [ChargeType.FreightCost]: 'Freight Cost',
  [ChargeType.InsurancePremium]: 'Insurance Premium',
  [ChargeType.BankCharges]: 'Bank Charges',
  [ChargeType.StorageFee]: 'Storage Fee',
  [ChargeType.AgencyFee]: 'Agency Fee',
  [ChargeType.Other]: 'Other'
};

export const QuantityUnitLabels: Record<QuantityUnit, string> = {
  [QuantityUnit.MT]: 'MT',
  [QuantityUnit.BBL]: 'BBL',
  [QuantityUnit.GAL]: 'GAL',
  [QuantityUnit.LOTS]: 'LOTS'
};

// Helper functions
export const getDocumentTypeLabel = (type: DocumentType): string => {
  return DocumentTypeLabels[type] || 'Unknown';
};

export const getSettlementStatusLabel = (status: ContractSettlementStatus): string => {
  return ContractSettlementStatusLabels[status] || 'Unknown';
};

export const getChargeTypeLabel = (type: ChargeType): string => {
  return ChargeTypeLabels[type] || 'Unknown';
};

export const getQuantityUnitLabel = (unit: QuantityUnit): string => {
  return QuantityUnitLabels[unit] || 'Unknown';
};

// Status color mappings for UI
export const getSettlementStatusColor = (status: ContractSettlementStatus | string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  const statusNum = typeof status === 'string' ? 
    Object.values(ContractSettlementStatus).find(s => ContractSettlementStatusLabels[s as ContractSettlementStatus] === status) as ContractSettlementStatus :
    status;

  switch (statusNum) {
    case ContractSettlementStatus.Draft:
      return 'default';
    case ContractSettlementStatus.DataEntered:
      return 'info';
    case ContractSettlementStatus.Calculated:
      return 'primary';
    case ContractSettlementStatus.Reviewed:
      return 'secondary';
    case ContractSettlementStatus.Approved:
      return 'warning';
    case ContractSettlementStatus.Finalized:
      return 'success';
    case ContractSettlementStatus.Cancelled:
      return 'error';
    default:
      return 'default';
  }
};

export const getChargeTypeColor = (type: ChargeType): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (type) {
    case ChargeType.Demurrage:
      return 'error';
    case ChargeType.Despatch:
      return 'success';
    case ChargeType.InspectionFee:
      return 'info';
    case ChargeType.PortCharges:
      return 'warning';
    case ChargeType.FreightCost:
      return 'primary';
    case ChargeType.InsurancePremium:
      return 'secondary';
    case ChargeType.BankCharges:
      return 'default';
    case ChargeType.StorageFee:
      return 'warning';
    case ChargeType.AgencyFee:
      return 'info';
    case ChargeType.Other:
      return 'default';
    default:
      return 'default';
  }
};