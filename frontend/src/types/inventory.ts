// Inventory Types
export interface InventoryLocation {
  id: string;
  locationCode: string;
  locationName: string;
  locationType: string;
  country: string;
  region: string;
  address?: string;
  coordinates?: string;
  isActive: boolean;
  operatorName?: string;
  contactInfo?: string;
  totalCapacity: number;
  availableCapacity: number;
  usedCapacity: number;
  capacityUnit: string;
  supportedProducts?: string[];
  handlingServices?: string[];
  hasRailAccess: boolean;
  hasRoadAccess: boolean;
  hasSeaAccess: boolean;
  hasPipelineAccess: boolean;
  createdAt: string;
  updatedAt: string;
  inventoryPositionsCount: number;
  totalInventoryValue: number;
}

export interface InventoryPosition {
  id: string;
  locationId: string;
  locationName: string;
  locationCode: string;
  productId: string;
  productName: string;
  productCode: string;
  quantity: number;
  quantityUnit: string;
  averageCost: number;
  currency: string;
  totalValue: number;
  lastUpdated: string;
  grade?: string;
  batchReference?: string;
  sulfur?: number;
  api?: number;
  viscosity?: number;
  qualityNotes?: string;
  receivedDate?: string;
  status: string;
  statusNotes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface InventoryMovement {
  id: string;
  fromLocationId: string;
  fromLocationName: string;
  fromLocationCode: string;
  toLocationId: string;
  toLocationName: string;
  toLocationCode: string;
  productId: string;
  productName: string;
  productCode: string;
  quantity: number;
  quantityUnit: string;
  movementType: string;
  movementDate: string;
  plannedDate?: string;
  status: string;
  movementReference: string;
  transportMode?: string;
  vesselName?: string;
  transportReference?: string;
  transportCost?: number;
  handlingCost?: number;
  totalCost?: number;
  costCurrency?: string;
  initiatedBy?: string;
  approvedBy?: string;
  approvedAt?: string;
  notes?: string;
  purchaseContractId?: string;
  purchaseContractNumber?: string;
  salesContractId?: string;
  salesContractNumber?: string;
  shippingOperationId?: string;
  shippingOperationReference?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateInventoryLocationRequest {
  locationCode: string;
  locationName: string;
  locationType: InventoryLocationType;
  country: string;
  region: string;
  address?: string;
  coordinates?: string;
  operatorName?: string;
  contactInfo?: string;
  totalCapacity: number;
  capacityUnit: QuantityUnit;
  supportedProducts?: string[];
  handlingServices?: string[];
  hasRailAccess: boolean;
  hasRoadAccess: boolean;
  hasSeaAccess: boolean;
  hasPipelineAccess: boolean;
}

export interface UpdateInventoryLocationRequest extends CreateInventoryLocationRequest {
  id: string;
  isActive: boolean;
}

export interface CreateInventoryPositionRequest {
  locationId: string;
  productId: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  averageCost: number;
  currency: string;
  grade?: string;
  batchReference?: string;
  sulfur?: number;
  api?: number;
  viscosity?: number;
  qualityNotes?: string;
  receivedDate?: string;
  status: InventoryStatus;
  statusNotes?: string;
}

export interface UpdateInventoryPositionRequest extends CreateInventoryPositionRequest {
  id: string;
}

export interface CreateInventoryMovementRequest {
  fromLocationId: string;
  toLocationId: string;
  productId: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  movementType: InventoryMovementType;
  movementDate: string;
  plannedDate?: string;
  transportMode?: string;
  vesselName?: string;
  transportReference?: string;
  transportCost?: number;
  handlingCost?: number;
  costCurrency?: string;
  notes?: string;
  purchaseContractId?: string;
  salesContractId?: string;
  shippingOperationId?: string;
}

export interface UpdateInventoryMovementRequest extends CreateInventoryMovementRequest {
  id: string;
  status: InventoryMovementStatus;
  approvedBy?: string;
  approvedAt?: string;
}

export interface InventorySummary {
  totalLocations: number;
  activeLocations: number;
  totalProducts: number;
  totalInventoryQuantity: number;
  totalInventoryValue: number;
  currency: string;
  pendingMovements: number;
  lastUpdated: string;
}

export interface LocationSummary {
  locationId: string;
  locationName: string;
  locationCode: string;
  locationType: string;
  utilizationPercentage: number;
  productCount: number;
  totalValue: number;
  currency: string;
}

// Enums
export enum InventoryLocationType {
  Terminal = 'Terminal',
  Tank = 'Tank',
  Refinery = 'Refinery',
  Port = 'Port',
  Pipeline = 'Pipeline',
  Storage = 'Storage',
  Floating = 'Floating'
}

export enum InventoryStatus {
  Available = 'Available',
  Reserved = 'Reserved',
  InTransit = 'InTransit',
  Quality = 'Quality',
  Blocked = 'Blocked',
  Contaminated = 'Contaminated',
  Aged = 'Aged'
}

export enum InventoryMovementType {
  Receipt = 'Receipt',
  Shipment = 'Shipment',
  Transfer = 'Transfer',
  Blending = 'Blending',
  Loss = 'Loss',
  Adjustment = 'Adjustment'
}

export enum InventoryMovementStatus {
  Planned = 'Planned',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  Failed = 'Failed'
}

export enum QuantityUnit {
  MT = 'MT',
  BBL = 'BBL'
}

// Filters
export interface InventoryFilters {
  locationId?: string;
  productId?: string;
  status?: InventoryStatus;
  locationType?: InventoryLocationType;
  country?: string;
  movementType?: InventoryMovementType;
  movementStatus?: InventoryMovementStatus;
  dateFrom?: Date;
  dateTo?: Date;
  pageNumber?: number;
  pageSize?: number;
}