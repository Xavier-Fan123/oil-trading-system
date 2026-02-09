export enum ContractStatus {
  Draft = 0,
  PendingApproval = 1,
  Active = 2,
  Completed = 3,
  Cancelled = 4
}

export enum DeliveryTerms {
  FOB = 1,
  CIF = 2,
  CFR = 3,
  DAP = 4,
  DDP = 5,
  DES = 6,
  DDU = 7,
  STS = 8,
  ITT = 9,
  EXW = 10
}

export enum SettlementType {
  TT = 1,
  LC = 2,
  CAD = 3,
  SBLC = 4,
  DP = 5
}

export enum PricingType {
  Fixed = 1,
  Floating = 2,
  Formula = 3
}

export enum QuantityUnit {
  MT = 1,
  BBL = 2,
  GAL = 3,
  LOTS = 4
}

export enum ContractType {
  CARGO = 1,
  EXW = 2,
  DEL = 3
}

export interface Money {
  amount: number;
  currency: string;
}

export interface Quantity {
  value: number;
  unit: QuantityUnit;
}

export enum QuantityCalculationMode {
  UseActualQuantities = 1,
  UseMTForAll = 2,
  UseBBLForAll = 3,
  UseContractSpecified = 4
}

export const QuantityCalculationModeLabels = {
  [QuantityCalculationMode.UseActualQuantities]: 'Use Actual Quantities (Mixed Units)',
  [QuantityCalculationMode.UseMTForAll]: 'Use MT for All Calculations',
  [QuantityCalculationMode.UseBBLForAll]: 'Use BBL for All Calculations',
  [QuantityCalculationMode.UseContractSpecified]: 'Use Contract Specified Quantity'
};

export const QuantityUnitLabels = {
  [QuantityUnit.MT]: 'MT',
  [QuantityUnit.BBL]: 'BBL',
  [QuantityUnit.GAL]: 'GAL'
};

export interface PriceFormula {
  formula: string;
  method: number;
  indexName?: string;
  fixedPrice?: number;
  premium?: Money;
  discount?: Money;
  basePrice?: Money;
  isFixedPrice: boolean;
  // Mixed-unit pricing support
  benchmarkUnit?: QuantityUnit;
  adjustmentUnit?: QuantityUnit;
  adjustment?: Money;
  calculationMode?: QuantityCalculationMode;
  contractualConversionRatio?: number;
}

export interface ContractNumber {
  value: string;
}

export interface TradingPartner {
  id: string;
  companyName: string;
  companyCode: string;
  partnerType: number;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  taxNumber?: string;
  creditLimit: number;
  creditLimitValidUntil: Date;
  paymentTermDays: number;
  currentExposure: number;
  creditUtilization: number;
  isActive: boolean;
  isBlocked: boolean;
  blockReason?: string;
  isCreditExceeded?: boolean;
  // For backward compatibility with components using these field names
  name?: string;
  code?: string;
}

export interface Product {
  id: string;
  code: string;
  name: string;
  productName: string;
  type: number;
  grade: string;
  specification: string;
  unitOfMeasure: string;
  density: number;
  origin: string;
  isActive: boolean;
}

export interface PriceBenchmark {
  id: string;
  benchmarkName: string;
  benchmarkType: string;
  productCategory: string;
  currency: string;
  unit: string;
  description?: string;
  dataSource?: string;
  isActive: boolean;
}

export interface PurchaseContract {
  id: string;
  contractNumber: ContractNumber;
  externalContractNumber?: string; // External/Manual contract number
  contractType: ContractType;
  status: ContractStatus;

  // Supplier Information (nested object from backend)
  supplier: TradingPartner;

  // Product Information (nested object from backend)
  product: Product;

  // Trader Information
  traderId: string;
  traderName?: string;
  traderEmail?: string;

  // Price Benchmark Information
  priceBenchmarkId?: string;
  priceBenchmarkName?: string;
  priceBenchmarkType?: string;

  // Linked Contracts
  benchmarkContractId?: string;
  benchmarkContractNumber?: string;

  // Quantity Information
  quantity: number;
  quantityUnit: QuantityUnit;
  tonBarrelRatio: number;

  // Pricing Information
  pricingType: PricingType;
  pricingFormula?: string;
  contractValue?: number;  // Backend returns contractValue
  fixedPrice?: number;     // Frontend alias for contractValue
  contractValueCurrency?: string;
  pricingPeriodStart?: Date;
  pricingPeriodEnd?: Date;
  isPriceFinalized?: boolean;
  premium?: number;
  discount?: number;

  // Delivery Information
  deliveryTerms: DeliveryTerms;
  laycanStart: Date;
  laycanEnd: Date;
  loadPort: string;
  dischargePort: string;

  // Payment Information
  settlementType: SettlementType;
  paymentTerms?: string;
  creditPeriodDays: number;
  prepaymentPercentage: number;

  // Additional Information
  incoterms?: string;
  qualitySpecifications?: string;
  inspectionAgency?: string;
  notes?: string;

  // Quantity Tolerance
  quantityTolerancePercent?: number;
  quantityToleranceOption?: string;

  // Broker & Commission
  brokerName?: string;
  brokerCommission?: number;
  brokerCommissionType?: string;

  // Demurrage & Laytime
  laytimeHours?: number;
  demurrageRate?: number;
  despatchRate?: number;

  // Business Metrics
  estimatedProfit?: number;
  margin?: number;
  riskMetrics?: {
    var95: number;
    exposure: number;
  };

  // Shipping Operations
  shippingOperations?: any[];

  // Pricing Events
  pricingEvents?: any[];

  // Audit Information
  createdAt: Date;
  createdBy: string;
  updatedAt?: Date;
  updatedBy?: string;

  // Linked Contracts
  linkedSalesContracts?: SalesContract[];
}

export interface SalesContract {
  id: string;
  contractNumber: ContractNumber;
  contractType: ContractType;
  status: ContractStatus;
  customer: TradingPartner;
  product: Product;
  traderId: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  tonBarrelRatio: number;
  pricingType: PricingType;
  fixedPrice?: number;
  pricingFormula?: string;
  pricingPeriodStart?: Date;
  pricingPeriodEnd?: Date;
  deliveryTerms: DeliveryTerms;
  laycanStart: Date;
  laycanEnd: Date;
  loadPort: string;
  dischargePort: string;
  settlementType: SettlementType;
  creditPeriodDays: number;
  prepaymentPercentage: number;
  paymentTerms?: string;
  qualitySpecifications?: string;
  inspectionAgency?: string;
  notes?: string;
  linkedPurchaseContractId?: string;
  createdAt: Date;
  createdBy: string;
  updatedAt?: Date;
  updatedBy?: string;
}

export interface CreatePurchaseContractDto {
  externalContractNumber?: string; // External/Manual contract number
  contractType: ContractType;
  supplierId: string;
  productId: string;
  traderId: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  tonBarrelRatio: number;
  priceBenchmarkId?: string;
  pricingType: PricingType;
  fixedPrice?: number;
  pricingFormula?: string;
  pricingPeriodStart?: Date;
  pricingPeriodEnd?: Date;
  // Mixed-unit pricing support
  benchmarkUnit?: QuantityUnit;
  adjustmentUnit?: QuantityUnit;
  adjustmentAmount?: number;
  adjustmentCurrency?: string;
  calculationMode?: QuantityCalculationMode;
  contractualConversionRatio?: number;
  deliveryTerms: DeliveryTerms;
  laycanStart: Date;
  laycanEnd: Date;
  loadPort: string;
  dischargePort: string;
  settlementType: SettlementType;
  creditPeriodDays: number;
  prepaymentPercentage: number;
  paymentTerms?: string;
  qualitySpecifications?: string;
  inspectionAgency?: string;
  notes?: string;
  // Quantity Tolerance
  quantityTolerancePercent?: number;
  quantityToleranceOption?: string;
  // Broker & Commission
  brokerName?: string;
  brokerCommission?: number;
  brokerCommissionType?: string;
  // Demurrage & Laytime
  laytimeHours?: number;
  demurrageRate?: number;
  despatchRate?: number;
  createdBy: string;
}

export interface UpdatePurchaseContractDto {
  externalContractNumber?: string; // External/Manual contract number
  quantity: number;
  quantityUnit: QuantityUnit;
  tonBarrelRatio: number;
  priceBenchmarkId?: string;
  pricingType: PricingType;
  fixedPrice?: number;
  pricingFormula?: string;
  pricingPeriodStart?: Date;
  pricingPeriodEnd?: Date;
  // Mixed-unit pricing support
  benchmarkUnit?: QuantityUnit;
  adjustmentUnit?: QuantityUnit;
  adjustmentAmount?: number;
  adjustmentCurrency?: string;
  calculationMode?: QuantityCalculationMode;
  contractualConversionRatio?: number;
  deliveryTerms: DeliveryTerms;
  laycanStart: Date;
  laycanEnd: Date;
  loadPort: string;
  dischargePort: string;
  settlementType: SettlementType;
  creditPeriodDays: number;
  prepaymentPercentage: number;
  paymentTerms?: string;
  qualitySpecifications?: string;
  inspectionAgency?: string;
  notes?: string;
  // Quantity Tolerance
  quantityTolerancePercent?: number;
  quantityToleranceOption?: string;
  // Broker & Commission
  brokerName?: string;
  brokerCommission?: number;
  brokerCommissionType?: string;
  // Demurrage & Laytime
  laytimeHours?: number;
  demurrageRate?: number;
  despatchRate?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PurchaseContractListDto {
  id: string;
  contractNumber: string;
  externalContractNumber?: string; // External/Manual contract number
  contractType?: ContractType;
  status: ContractStatus;
  supplierId?: string;
  supplierName: string;
  productId?: string;
  productName: string;
  traderName?: string;
  quantity: number;
  quantityUnit: QuantityUnit | string; // Backend returns as string due to JsonStringEnumConverter
  contractValue?: number;
  contractValueCurrency?: string;
  laycanStart: Date;
  laycanEnd: Date;
  loadPort?: string;
  dischargePort?: string;
  isPriceFinalized?: boolean;
  pricingStatus?: string; // "Unpriced" | "PartiallyPriced" | "FullyPriced"
  linkedSalesContractsCount?: number;
  shippingOperationsCount?: number;
  createdAt: Date;
  updatedAt?: Date;
}

export interface ContractFilters {
  status?: ContractStatus;
  supplierId?: string;
  productId?: string;
  laycanStart?: Date;
  laycanEnd?: Date;
  tagIds?: string[];
  pageNumber?: number;
  pageSize?: number;
}

// Tag-related types
export enum TagCategory {
  RiskLevel = 1,
  TradingStrategy = 2,
  PositionManagement = 3,
  RiskControl = 4,
  Compliance = 5,
  MarketCondition = 6,
  ProductClass = 7,
  Region = 8,
  Priority = 9,
  Customer = 10,
  Custom = 99
}

export interface Tag {
  id: string;
  name: string;
  description?: string;
  color: string;
  category: TagCategory;
  categoryDisplayName: string;
  priority: number;
  isActive: boolean;
  usageCount: number;
  lastUsedAt?: Date;
  mutuallyExclusiveTags?: string;
  maxUsagePerEntity?: number;
  allowedContractStatuses?: string;
  createdAt: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
}

export interface TagSummary {
  id: string;
  name: string;
  color: string;
  category: TagCategory;
  categoryDisplayName: string;
  usageCount: number;
  isActive: boolean;
}

export interface CreateTagDto {
  name: string;
  description?: string;
  color?: string;
  category: TagCategory;
  priority?: number;
}

export interface UpdateTagDto {
  name?: string;
  description?: string;
  color?: string;
  priority?: number;
}

export interface ContractTag {
  id: string;
  contractId: string;
  contractType: string;
  tagId: string;
  tagName: string;
  tagColor: string;
  tagCategory: TagCategory;
  notes?: string;
  assignedBy: string;
  assignedAt: Date;
}

export interface AddContractTagDto {
  tagId: string;
  notes?: string;
}

export interface TagValidationResult {
  isValid: boolean;
  errorMessage?: string;
  warnings: string[];
  conflictingTags: string[];
}

export interface TagUsageStatistics {
  totalTags: number;
  activeTags: number;
  unusedTags: number;
  tagsByCategory: { [key: string]: number };
  mostUsedTags: TagUsageInfo[];
  recentlyUsedTags: TagUsageInfo[];
}

export interface TagUsageInfo {
  tagId: string;
  tagName: string;
  categoryDisplayName: string;
  usageCount: number;
  lastUsedAt?: Date;
}

export interface PredefinedTagInfo {
  category: TagCategory;
  categoryDisplayName: string;
  categoryDescription: string;
  defaultColor: string;
  predefinedNames: string[];
}

// Removed duplicate ContractFiltersExtended - use ContractFilters instead