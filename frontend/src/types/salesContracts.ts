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

export interface SalesContract {
  id: string;
  contractNumber: string;
  customerId: string;
  customerName: string;
  productId: string;
  productName: string;
  quantity: number;
  unit: QuantityUnit;
  pricePerUnit: number;
  totalValue: number;
  currency: string;
  priceBenchmarkId?: string;
  priceBenchmarkName?: string;
  priceBenchmarkType?: string;
  deliveryTerms: DeliveryTerms;
  laycanStart: string;
  laycanEnd: string;
  deliveryLocation: string;
  settlementType: SettlementType;
  pricingType: PricingType;
  priceFormula?: string;
  status: ContractStatus;
  signedDate: string;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  margin?: number;
  estimatedProfit?: number;
  riskMetrics?: {
    var95: number;
    exposure: number;
  };
  notes?: string;
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
  customerName: string;
  productName: string;
  quantity: number;
  unit: string;
  totalValue: number;
  status: ContractStatus;
  deliveryMonth: string;
  createdAt: string;
  estimatedProfit?: number;
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