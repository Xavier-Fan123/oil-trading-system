export interface Product {
  id: string;
  productCode: string;
  productName: string;
  type: ProductType;
  grade?: string;
  specification?: string;
  unitOfMeasure: string;
  density?: number;
  origin?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  purchaseContracts?: PurchaseContract[];
  salesContracts?: SalesContract[];
}

export interface PurchaseContract {
  id: string;
  contractNumber: ContractNumber;
  contractType: ContractType;
  status: ContractStatus;
  quantity: Quantity;
  priceFormula: PriceFormula;
  deliveryTerms: DeliveryTerms;
  settlementType: SettlementType;
  contractDate: string;
  laycanStart?: string;
  laycanEnd?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  product?: Product;
  tradingPartner?: TradingPartner;
  createdBy?: User;
}

export interface SalesContract {
  id: string;
  contractNumber: ContractNumber;
  contractType: ContractType;
  status: ContractStatus;
  quantity: Quantity;
  priceFormula: PriceFormula;
  deliveryTerms: DeliveryTerms;
  settlementType: SettlementType;
  contractDate: string;
  laycanStart?: string;
  laycanEnd?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  product?: Product;
  tradingPartner?: TradingPartner;
  createdBy?: User;
}

export interface TradingPartner {
  id: string;
  companyCode: string;
  companyName: string;
  type: TradingPartnerType;
  contactEmail?: string;
  contactPhone?: string;
  address?: string;
  country?: string;
  taxId?: string;
  isActive: boolean;
  creditLimit?: number;
  creditRating?: string;
}

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
}

export interface ContractNumber {
  value: string;
}

export interface Quantity {
  value: number;
  unit: QuantityUnit;
}

export interface Money {
  amount: number;
  currency: string;
}

export interface PriceFormula {
  formula: string;
  method: PricingMethod;
  indexName?: string;
  fixedPrice?: number;
  premium?: Money;
  discount?: Money;
}

// Enums
export enum ProductType {
  CrudeOil = 'CRUDE_OIL',
  RefinedProducts = 'REFINED_PRODUCTS',
  NaturalGas = 'NATURAL_GAS',
  Petrochemicals = 'PETROCHEMICALS'
}

export enum ContractType {
  Spot = 'SPOT',
  Term = 'TERM',
  Cargo = 'CARGO'
}

export enum ContractStatus {
  Draft = 'DRAFT',
  PendingApproval = 'PENDING_APPROVAL',
  Active = 'ACTIVE',
  Completed = 'COMPLETED',
  Cancelled = 'CANCELLED'
}

export enum QuantityUnit {
  MT = 'MT',
  BBL = 'BBL',
  GAL = 'GAL',
  LT = 'LT'
}

export enum PricingMethod {
  Fixed = 'FIXED',
  IndexBased = 'INDEX_BASED',
  Formula = 'FORMULA'
}

export enum DeliveryTerms {
  FOB = 'FOB',
  CFR = 'CFR',
  CIF = 'CIF',
  EXW = 'EXW',
  DAP = 'DAP',
  DDP = 'DDP'
}

export enum SettlementType {
  TT = 'TT',
  LC = 'LC',
  DPA = 'DPA',
  CAD = 'CAD'
}

export enum TradingPartnerType {
  Supplier = 'SUPPLIER',
  Customer = 'CUSTOMER',
  Both = 'BOTH'
}

export enum UserRole {
  Trader = 'TRADER',
  MiddleOffice = 'MIDDLE_OFFICE',
  BackOffice = 'BACK_OFFICE',
  RiskManager = 'RISK_MANAGER',
  Administrator = 'ADMINISTRATOR'
}

// Input types for mutations
export interface CreateProductInput {
  productCode: string;
  productName: string;
  type: ProductType;
  grade?: string;
  specification?: string;
  unitOfMeasure: string;
  density?: number;
  origin?: string;
}

export interface UpdateProductInput {
  id: string;
  productCode?: string;
  productName?: string;
  type?: ProductType;
  grade?: string;
  specification?: string;
  unitOfMeasure?: string;
  density?: number;
  origin?: string;
  isActive?: boolean;
}

export interface DeleteProductInput {
  id: string;
}

// Filter inputs for queries
export interface ProductFilterInput {
  id?: UuidOperationFilterInput;
  productCode?: StringOperationFilterInput;
  productName?: StringOperationFilterInput;
  type?: ProductTypeOperationFilterInput;
  isActive?: BooleanOperationFilterInput;
  createdAt?: DateTimeOperationFilterInput;
}

export interface PurchaseContractFilterInput {
  id?: UuidOperationFilterInput;
  status?: ContractStatusOperationFilterInput;
  contractType?: ContractTypeOperationFilterInput;
  productId?: UuidOperationFilterInput;
  tradingPartnerId?: UuidOperationFilterInput;
  contractDate?: DateTimeOperationFilterInput;
}

// Sort inputs
export interface ProductSortInput {
  productCode?: SortEnumType;
  productName?: SortEnumType;
  type?: SortEnumType;
  createdAt?: SortEnumType;
}

export interface PurchaseContractSortInput {
  contractDate?: SortEnumType;
  status?: SortEnumType;
  createdAt?: SortEnumType;
}

export enum SortEnumType {
  ASC = 'ASC',
  DESC = 'DESC'
}

// Operation filter inputs
export interface StringOperationFilterInput {
  and?: StringOperationFilterInput[];
  or?: StringOperationFilterInput[];
  eq?: string;
  neq?: string;
  contains?: string;
  ncontains?: string;
  in?: string[];
  nin?: string[];
  startsWith?: string;
  nstartsWith?: string;
  endsWith?: string;
  nendsWith?: string;
}

export interface UuidOperationFilterInput {
  and?: UuidOperationFilterInput[];
  or?: UuidOperationFilterInput[];
  eq?: string;
  neq?: string;
  in?: string[];
  nin?: string[];
}

export interface BooleanOperationFilterInput {
  and?: BooleanOperationFilterInput[];
  or?: BooleanOperationFilterInput[];
  eq?: boolean;
  neq?: boolean;
}

export interface DateTimeOperationFilterInput {
  and?: DateTimeOperationFilterInput[];
  or?: DateTimeOperationFilterInput[];
  eq?: string;
  neq?: string;
  in?: string[];
  nin?: string[];
  gt?: string;
  ngt?: string;
  gte?: string;
  ngte?: string;
  lt?: string;
  nlt?: string;
  lte?: string;
  nlte?: string;
}

export interface ProductTypeOperationFilterInput {
  and?: ProductTypeOperationFilterInput[];
  or?: ProductTypeOperationFilterInput[];
  eq?: ProductType;
  neq?: ProductType;
  in?: ProductType[];
  nin?: ProductType[];
}

export interface ContractStatusOperationFilterInput {
  and?: ContractStatusOperationFilterInput[];
  or?: ContractStatusOperationFilterInput[];
  eq?: ContractStatus;
  neq?: ContractStatus;
  in?: ContractStatus[];
  nin?: ContractStatus[];
}

export interface ContractTypeOperationFilterInput {
  and?: ContractTypeOperationFilterInput[];
  or?: ContractTypeOperationFilterInput[];
  eq?: ContractType;
  neq?: ContractType;
  in?: ContractType[];
  nin?: ContractType[];
}

// Subscription event types
export interface ProductStatusChanged {
  productId: string;
  productCode: string;
  productName: string;
  isActive: boolean;
  changedAt: string;
  changedBy: string;
}

export interface ContractStatusChanged {
  contractId: string;
  contractNumber: string;
  oldStatus: ContractStatus;
  newStatus: ContractStatus;
  changedAt: string;
  changedBy: string;
}

export interface PriceUpdate {
  contractId: string;
  contractNumber: string;
  oldPrice: number;
  newPrice: number;
  currency: string;
  updatedAt: string;
  updatedBy: string;
  priceSource: string;
}

// Response types
export interface MutationResponse<TData = any> {
  success?: boolean;
  data?: TData;
  errors?: MutationError[];
}

export interface MutationError {
  code: string;
  message: string;
  path?: string[];
}

export interface PageInfo {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  startCursor?: string;
  endCursor?: string;
}

export interface Connection<T> {
  nodes: T[];
  pageInfo: PageInfo;
  totalCount: number;
}