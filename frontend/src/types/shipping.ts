// Shipping Operations Types

export interface ShippingOperation {
  id: string;
  shippingNumber: string;
  contractId: string;
  contractNumber: string;
  vesselName: string;
  imoNumber?: string;
  plannedQuantity: number;
  plannedQuantityUnit: string;
  actualQuantity?: number;
  actualQuantityUnit?: string;
  loadPort?: string;
  dischargePort?: string;
  status: string;
  laycanStart?: string;
  laycanEnd?: string;
  norDate?: string;
  billOfLadingDate?: string;
  dischargeDate?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ShippingOperationDto {
  id: string;
  shippingNumber: string;
  contractId: string;
  contractNumber: string;
  vesselName: string;
  imoNumber?: string;
  plannedQuantity: number;
  plannedQuantityUnit: string;
  actualQuantity?: number;
  actualQuantityUnit?: string;
  loadPort?: string;
  dischargePort?: string;
  status: string;
  laycanStart?: string;
  laycanEnd?: string;
  norDate?: string;
  billOfLadingDate?: string;
  dischargeDate?: string;
  notes?: string;
  createdBy?: string;
  updatedBy?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ShippingOperationSummaryDto {
  id: string;
  shippingNumber?: string;
  contractId: string;
  contractNumber: string;
  vesselName: string;
  imoNumber?: string;
  loadPort?: string;
  dischargePort?: string;
  status: string;
  plannedQuantity: number;
  plannedQuantityUnit: string;
  actualQuantity?: number;
  actualQuantityUnit?: string;
  laycanStart?: string;
  laycanEnd?: string;
  norDate?: string;
  billOfLadingDate?: string;
  dischargeDate?: string;
  loadPortETA?: string;
  dischargePortETA?: string;
  createdAt: string;
}

export interface ShippingOperationSummaryDtoPagedResult {
  items: ShippingOperationSummaryDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateShippingOperationDto {
  contractId: string;
  vesselName: string;
  imoNumber?: string;
  chartererName?: string;
  vesselCapacity?: number;
  shippingAgent?: string;
  plannedQuantity: number;
  plannedQuantityUnit: string;
  laycanStart?: string;
  laycanEnd?: string;
  loadPort?: string;
  dischargePort?: string;
  notes?: string;
}

export interface UpdateShippingOperationDto {
  vesselName?: string;
  imoNumber?: string;
  plannedQuantity?: number;
  plannedQuantityUnit?: string;
  actualQuantity?: number;
  actualQuantityUnit?: string;
  laycanStart?: string;
  laycanEnd?: string;
  norDate?: string;
  billOfLadingDate?: string;
  dischargeDate?: string;
  notes?: string;
}

export interface RecordLiftingOperationDto {
  shippingOperationId: string;
  norDate: string;
  actualQuantity: number;
  quantityUnit: string;
  demurrageDays?: number;
  notes?: string;
}

export interface CancelShippingOperationDto {
  reason: string;
}

export enum ShippingStatus {
  Planned = 1,
  InTransit = 2,
  Loading = 3,
  Loaded = 4,
  Discharging = 5,
  Completed = 6,
  Cancelled = 7,
}

export interface Quantity {
  value: number;
  unit: string;
}

// Shipping filter options
export interface ShippingFilters {
  page?: number;
  pageSize?: number;
  shippingNumber?: string;
  contractId?: string;
  vesselName?: string;
  status?: ShippingStatus;
  loadPort?: string;
  dischargePort?: string;
  startDate?: string;
  endDate?: string;
}

// Status options for UI
export const SHIPPING_STATUS_OPTIONS = [
  { value: ShippingStatus.Planned, label: 'Planned', color: 'default' },
  { value: ShippingStatus.InTransit, label: 'In Transit', color: 'info' },
  { value: ShippingStatus.Loading, label: 'Loading', color: 'warning' },
  { value: ShippingStatus.Loaded, label: 'Loaded', color: 'primary' },
  { value: ShippingStatus.Discharging, label: 'Discharging', color: 'warning' },
  { value: ShippingStatus.Completed, label: 'Completed', color: 'success' },
  { value: ShippingStatus.Cancelled, label: 'Cancelled', color: 'error' },
] as const;

// Common ports for dropdowns
export const COMMON_PORTS = [
  'Rotterdam, Netherlands',
  'Houston, TX, USA',
  'Singapore',
  'Fujairah, UAE',
  'Antwerp, Belgium',
  'Mumbai, India',
  'Bremen, Germany',
  'Le Havre, France',
  'Algeciras, Spain',
  'Jebel Ali, UAE',
  'Shanghai, China',
  'Busan, South Korea',
  'Los Angeles, CA, USA',
  'Long Beach, CA, USA',
  'Hamburg, Germany',
] as const;

// Unit options - IMPORTANT: Backend only supports MT and BBL
// Do NOT add other units unless backend is updated to support them
export const QUANTITY_UNITS = [
  { value: 'MT', label: 'Metric Tons (MT)' },
  { value: 'BBL', label: 'Barrels (BBL)' },
] as const;
