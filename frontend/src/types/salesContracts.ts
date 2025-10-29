// Import enums from purchase contracts
import {
  ContractStatus,
  ContractType,
  DeliveryTerms,
  SettlementType,
  PricingType,
  QuantityUnit
} from './contracts';

// Re-export enums so they can be imported from this module
export {
  ContractStatus,
  ContractType,
  DeliveryTerms,
  SettlementType,
  PricingType,
  QuantityUnit
};

// DTO interfaces that match the backend
export interface CustomerDto {
  id: string;
  name: string;
  code: string;
}

export interface ContractProductDto {
  id: string;
  name: string;
  code: string;
}

export interface ContractNumber {
  value: string;
}

export interface SalesContract {
  id: string;
  contractNumber: ContractNumber;
  externalContractNumber?: string;
  contractType: ContractType;
  status: ContractStatus;

  // Customer Information (nested object from backend)
  customer: CustomerDto;
  // For backwards compatibility, extract ids
  customerId?: string;
  customerName?: string;

  // Product Information (nested object from backend)
  product: ContractProductDto;
  // For backwards compatibility, extract ids
  productId?: string;
  productName?: string;

  // Trader Information
  traderId: string;
  traderName?: string;

  // Linked Purchase Contract
  linkedPurchaseContractId?: string;
  linkedPurchaseContractNumber?: string;

  // Quantity Information
  quantity: number;
  quantityUnit: QuantityUnit;
  tonBarrelRatio: number;

  // Price Benchmark Information
  priceBenchmarkId?: string;
  priceBenchmarkName?: string;

  // Pricing Information
  pricingType: PricingType;
  pricingFormula?: string;
  contractValue?: number;  // Backend returns contractValue, not fixedPrice
  contractValueCurrency?: string;
  profitMargin?: number;
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

  // Business Metrics
  estimatedProfit?: number;
  margin?: number;
  riskMetrics?: {
    var95: number;
    exposure: number;
  };

  // Audit Information
  createdAt: Date;
  createdBy: string;
  updatedAt?: Date;
  updatedBy?: string;
}

export interface CreateSalesContractDto {
  externalContractNumber?: string;
  contractType: ContractType;
  customerId: string;
  productId: string;
  traderId: string;
  priceBenchmarkId?: string;
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
}

export interface UpdateSalesContractDto extends Partial<CreateSalesContractDto> {
  status?: ContractStatus;
}

export interface SalesContractListDto {
  id: string;
  contractNumber: string;
  externalContractNumber?: string;  // User-provided contract number
  status: ContractStatus;
  customerId: string;
  customerName: string;
  productId: string;
  productName: string;
  quantity: number;
  quantityUnit: QuantityUnit | string;  // Backend returns as string due to JsonStringEnumConverter
  contractValue?: number;  // Total contract value (from backend)
  estimatedProfit?: number;
  margin?: number;
  laycanStart: Date | string;
  laycanEnd: Date | string;
  createdAt: Date | string;
  updatedAt?: Date | string;
}

export interface SalesContractFilters {
  status?: ContractStatus;
  customerId?: string;
  productId?: string;
  laycanStart?: Date;
  laycanEnd?: Date;
  minValue?: number;
  maxValue?: number;
  pageNumber?: number;
  pageSize?: number;
}

export interface SalesContractSummary {
  totalContracts: number;
  totalValue: number;
  estimatedProfit: number;
  contractsByStatus: Array<{
    status: ContractStatus;
    count: number;
    value: number;
  }>;
  topCustomers: Array<{
    customerId: string;
    customerName: string;
    contractCount: number;
    totalValue: number;
  }>;
  monthlyBreakdown: Array<{
    month: string;
    contracts: number;
    value: number;
    profit: number;
  }>;
}

// Removed duplicate PagedResult - use the one from contracts.ts