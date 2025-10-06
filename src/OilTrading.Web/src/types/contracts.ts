export interface PurchaseContract {
  id: string
  contractNumber: string
  supplierId: string
  productId: string
  traderId: string
  quantity: number
  quantityUnit: string
  pricingType: PricingType
  price: number
  currency: string
  pricingFormula?: string
  contractDate: string
  deliveryStartDate: string
  deliveryEndDate: string
  deliveryLocation: string
  deliveryTerms: string
  status: ContractStatus
  paymentTerms: string
  estimatedValue: number
  riskProfile: string
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface SalesContract {
  id: string
  contractNumber: string
  customerId: string
  productId: string
  traderId: string
  linkedPurchaseContractId?: string
  quantity: number
  quantityUnit: string
  pricingType: PricingType
  price: number
  currency: string
  pricingFormula?: string
  contractDate: string
  deliveryStartDate: string
  deliveryEndDate: string
  deliveryLocation: string
  deliveryTerms: string
  status: ContractStatus
  paymentTerms: string
  estimatedValue: number
  profitMargin: number
  riskProfile: string
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface TradingPartner {
  id: string
  name: string
  code: string
  type: TradingPartnerType
  contactEmail: string
  contactPhone: string
  address: string
  country: string
  taxId: string
  isActive: boolean
  creditLimit: number
  creditRating: string
  createdAt: string
  updatedAt?: string
}

export interface Product {
  id: string
  name: string
  code: string
  type: ProductType
  grade: string
  specification: string
  unitOfMeasure: string
  density: number
  origin: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  isActive: boolean
  lastLoginAt?: string
  fullName: string
  createdAt: string
  updatedAt?: string
}

export enum PricingType {
  Fixed = 1,
  Floating = 2,
  Formula = 3,
  Index = 4
}

export enum ContractStatus {
  Draft = 1,
  PendingApproval = 2,
  Approved = 3,
  Active = 4,
  Completed = 5,
  Cancelled = 6,
  Suspended = 7
}

export enum TradingPartnerType {
  Supplier = 1,
  Customer = 2,
  Both = 3
}

export enum ProductType {
  CrudeOil = 1,
  RefinedProducts = 2,
  NaturalGas = 3,
  Petrochemicals = 4
}

export enum UserRole {
  Trader = 1,
  RiskManager = 2,
  Administrator = 3,
  Viewer = 4
}