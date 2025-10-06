import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// TypeScript interfaces for Physical Contracts
export interface PhysicalContractDto {
  id: string;
  contractNumber: string;
  contractType: string;
  status: string;
  supplierId?: string;
  supplierName?: string;
  customerId?: string;
  customerName?: string;
  productId: string;
  productName?: string;
  traderId: string;
  traderName?: string;
  quantity: number;
  quantityUnit: string;
  tonBarrelRatio: number;
  
  // Pricing information
  pricingType: string;
  fixedPrice?: number;
  pricingFormula?: string;
  priceBenchmarkId?: string;
  priceBenchmarkName?: string;
  currency: string;
  
  // Delivery information
  deliveryTerms: string;
  laycanStart: string;
  laycanEnd: string;
  loadPort: string;
  dischargePort: string;
  estimatedDeliveryDate?: string;
  actualDeliveryDate?: string;
  
  // Settlement information
  settlementType: string;
  creditPeriodDays: number;
  prepaymentPercentage: number;
  paymentTerms?: string;
  
  // Quality and inspection
  qualitySpecifications?: string;
  inspectionAgency?: string;
  qualityTestResults?: string;
  
  // Logistics
  vesselName?: string;
  vesselIMO?: string;
  blNumber?: string;
  liftingStatus?: string;
  dischargeStatus?: string;
  
  // Financial
  totalValue?: number;
  invoiceAmount?: number;
  paidAmount?: number;
  remainingAmount?: number;
  
  // Documents
  billOfLadingReceived?: boolean;
  invoiceIssued?: boolean;
  paymentReceived?: boolean;
  certificatesReceived?: boolean;
  
  // Audit
  notes?: string;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface PhysicalContractListDto {
  id: string;
  contractNumber: string;
  status: string;
  supplierName?: string;
  customerName?: string;
  productName: string;
  quantity: number;
  quantityUnit: string;
  laycanStart: string;
  laycanEnd: string;
  totalValue?: number;
  deliveryStatus: string;
  createdAt: string;
}

export interface CreatePhysicalContractDto {
  contractType: string; // "Purchase" or "Sales"
  supplierId?: string;
  customerId?: string;
  productId: string;
  traderId: string;
  quantity: number;
  quantityUnit: string;
  tonBarrelRatio: number;
  
  // Pricing
  pricingType: string;
  fixedPrice?: number;
  pricingFormula?: string;
  priceBenchmarkId?: string;
  currency: string;
  
  // Delivery
  deliveryTerms: string;
  laycanStart: string;
  laycanEnd: string;
  loadPort: string;
  dischargePort: string;
  
  // Settlement
  settlementType: string;
  creditPeriodDays: number;
  prepaymentPercentage: number;
  paymentTerms?: string;
  
  // Quality
  qualitySpecifications?: string;
  inspectionAgency?: string;
  
  notes?: string;
  createdBy?: string;
}

export interface UpdatePhysicalContractDto {
  quantity: number;
  quantityUnit: string;
  tonBarrelRatio: number;
  
  // Pricing updates
  pricingType: string;
  fixedPrice?: number;
  pricingFormula?: string;
  priceBenchmarkId?: string;
  
  // Delivery updates
  deliveryTerms: string;
  laycanStart: string;
  laycanEnd: string;
  loadPort: string;
  dischargePort: string;
  estimatedDeliveryDate?: string;
  
  // Settlement updates
  settlementType: string;
  creditPeriodDays: number;
  prepaymentPercentage: number;
  paymentTerms?: string;
  
  // Quality updates
  qualitySpecifications?: string;
  inspectionAgency?: string;
  
  notes?: string;
  updatedBy?: string;
}

export interface PhysicalContractDeliveryDto {
  id: string;
  vesselName?: string;
  vesselIMO?: string;
  blNumber?: string;
  actualQuantity?: number;
  qualityTestResults?: string;
  deliveryDate: string;
  liftingCompleted: boolean;
  dischargeCompleted: boolean;
  billOfLadingReceived: boolean;
  certificatesReceived: boolean;
  updatedBy?: string;
}

export interface PhysicalContractInvoiceDto {
  id: string;
  invoiceNumber: string;
  invoiceAmount: number;
  currency: string;
  invoiceDate: string;
  dueDate: string;
  paymentStatus: string;
  paidAmount?: number;
  paymentDate?: string;
  notes?: string;
  createdBy?: string;
}

export interface PhysicalContractSummaryDto {
  totalContracts: number;
  totalValue: number;
  pendingDeliveries: number;
  completedDeliveries: number;
  outstandingInvoices: number;
  totalPendingPayments: number;
  averageDeliveryTime: number;
  contractsByStatus: Array<{
    status: string;
    count: number;
    totalValue: number;
  }>;
  contractsByProduct: Array<{
    productName: string;
    count: number;
    totalQuantity: number;
    totalValue: number;
  }>;
  monthlyVolume: Array<{
    month: string;
    volume: number;
    value: number;
  }>;
}

export interface PhysicalContractError {
  error: string;
  message?: string;
}

/**
 * Physical Contracts API service for managing physical delivery contracts
 * Corresponds to PhysicalContractController endpoints
 */
export const physicalContractsApi = {
  /**
   * GET /api/physical-contracts
   * Retrieves all physical contracts
   */
  getAll: async (filters?: {
    status?: string;
    contractType?: string;
    productId?: string;
    traderId?: string;
    laycanStart?: string;
    laycanEnd?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PhysicalContractListDto[]> => {
    try {
      const params = new URLSearchParams();
      if (filters?.status) params.append('status', filters.status);
      if (filters?.contractType) params.append('contractType', filters.contractType);
      if (filters?.productId) params.append('productId', filters.productId);
      if (filters?.traderId) params.append('traderId', filters.traderId);
      if (filters?.laycanStart) params.append('laycanStart', filters.laycanStart);
      if (filters?.laycanEnd) params.append('laycanEnd', filters.laycanEnd);
      if (filters?.pageNumber) params.append('pageNumber', filters.pageNumber.toString());
      if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

      const response = await api.get(`/physical-contracts?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching physical contracts:', error);
      throw error;
    }
  },

  /**
   * GET /api/physical-contracts/{id}
   * Retrieves a specific physical contract by ID
   */
  getById: async (id: string): Promise<PhysicalContractDto> => {
    try {
      const response = await api.get(`/physical-contracts/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching physical contract:', error);
      throw error;
    }
  },

  /**
   * POST /api/physical-contracts
   * Creates a new physical contract
   */
  create: async (contract: CreatePhysicalContractDto): Promise<PhysicalContractDto> => {
    try {
      const response = await api.post('/physical-contracts', contract);
      return response.data;
    } catch (error) {
      console.error('Error creating physical contract:', error);
      // Re-throw with enhanced error information
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to create physical contract');
      }
      throw error;
    }
  },

  /**
   * PUT /api/physical-contracts/{id}
   * Updates an existing physical contract
   */
  update: async (id: string, contract: UpdatePhysicalContractDto): Promise<PhysicalContractDto> => {
    try {
      const response = await api.put(`/physical-contracts/${id}`, contract);
      return response.data;
    } catch (error) {
      console.error('Error updating physical contract:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to update physical contract');
      }
      throw error;
    }
  },

  /**
   * DELETE /api/physical-contracts/{id}
   * Deletes (soft delete) a physical contract
   */
  delete: async (id: string): Promise<void> => {
    try {
      await api.delete(`/physical-contracts/${id}`);
    } catch (error) {
      console.error('Error deleting physical contract:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to delete physical contract');
      }
      throw error;
    }
  },

  /**
   * POST /api/physical-contracts/{id}/delivery
   * Updates delivery information for a contract
   */
  updateDelivery: async (id: string, delivery: PhysicalContractDeliveryDto): Promise<PhysicalContractDto> => {
    try {
      const response = await api.post(`/physical-contracts/${id}/delivery`, delivery);
      return response.data;
    } catch (error) {
      console.error('Error updating delivery information:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to update delivery');
      }
      throw error;
    }
  },

  /**
   * POST /api/physical-contracts/{id}/invoice
   * Creates an invoice for a contract
   */
  createInvoice: async (id: string, invoice: Omit<PhysicalContractInvoiceDto, 'id'>): Promise<PhysicalContractInvoiceDto> => {
    try {
      const response = await api.post(`/physical-contracts/${id}/invoice`, invoice);
      return response.data;
    } catch (error) {
      console.error('Error creating invoice:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to create invoice');
      }
      throw error;
    }
  },

  /**
   * GET /api/physical-contracts/summary
   * Gets summary statistics for physical contracts
   */
  getSummary: async (fromDate?: string, toDate?: string): Promise<PhysicalContractSummaryDto> => {
    try {
      const params = new URLSearchParams();
      if (fromDate) params.append('fromDate', fromDate);
      if (toDate) params.append('toDate', toDate);

      const response = await api.get(`/physical-contracts/summary?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching contract summary:', error);
      throw error;
    }
  },

  /**
   * GET /api/physical-contracts/pending-deliveries
   * Gets contracts with pending deliveries
   */
  getPendingDeliveries: async (): Promise<PhysicalContractListDto[]> => {
    try {
      const response = await api.get('/physical-contracts/pending-deliveries');
      return response.data;
    } catch (error) {
      console.error('Error fetching pending deliveries:', error);
      throw error;
    }
  },

  /**
   * GET /api/physical-contracts/overdue-payments
   * Gets contracts with overdue payments
   */
  getOverduePayments: async (): Promise<PhysicalContractListDto[]> => {
    try {
      const response = await api.get('/physical-contracts/overdue-payments');
      return response.data;
    } catch (error) {
      console.error('Error fetching overdue payments:', error);
      throw error;
    }
  },

  // Utility methods for data validation and processing

  /**
   * Validates a create physical contract request
   */
  validateCreateRequest: (request: CreatePhysicalContractDto): string[] => {
    const errors: string[] = [];

    if (!request.contractType || request.contractType.trim() === '') {
      errors.push('Contract type is required');
    }

    if (!['Purchase', 'Sales'].includes(request.contractType)) {
      errors.push('Contract type must be either "Purchase" or "Sales"');
    }

    if (request.contractType === 'Purchase' && (!request.supplierId || request.supplierId.trim() === '')) {
      errors.push('Supplier is required for purchase contracts');
    }

    if (request.contractType === 'Sales' && (!request.customerId || request.customerId.trim() === '')) {
      errors.push('Customer is required for sales contracts');
    }

    if (!request.productId || request.productId.trim() === '') {
      errors.push('Product is required');
    }

    if (!request.traderId || request.traderId.trim() === '') {
      errors.push('Trader is required');
    }

    if (!request.quantity || request.quantity <= 0) {
      errors.push('Quantity must be greater than zero');
    }

    if (!request.quantityUnit || request.quantityUnit.trim() === '') {
      errors.push('Quantity unit is required');
    }

    if (!request.pricingType || request.pricingType.trim() === '') {
      errors.push('Pricing type is required');
    }

    if (request.pricingType === 'Fixed' && (!request.fixedPrice || request.fixedPrice <= 0)) {
      errors.push('Fixed price must be greater than zero when pricing type is Fixed');
    }

    if (!request.deliveryTerms || request.deliveryTerms.trim() === '') {
      errors.push('Delivery terms are required');
    }

    if (!request.laycanStart) {
      errors.push('Laycan start date is required');
    }

    if (!request.laycanEnd) {
      errors.push('Laycan end date is required');
    }

    if (request.laycanStart && request.laycanEnd && new Date(request.laycanStart) >= new Date(request.laycanEnd)) {
      errors.push('Laycan start date must be before laycan end date');
    }

    if (!request.loadPort || request.loadPort.trim() === '') {
      errors.push('Load port is required');
    }

    if (!request.dischargePort || request.dischargePort.trim() === '') {
      errors.push('Discharge port is required');
    }

    if (!request.settlementType || request.settlementType.trim() === '') {
      errors.push('Settlement type is required');
    }

    if (request.creditPeriodDays < 0) {
      errors.push('Credit period days cannot be negative');
    }

    if (request.prepaymentPercentage < 0 || request.prepaymentPercentage > 100) {
      errors.push('Prepayment percentage must be between 0 and 100');
    }

    return errors;
  },

  /**
   * Calculates total contract value
   */
  calculateContractValue: (contract: PhysicalContractDto): number => {
    if (contract.fixedPrice && contract.quantity) {
      return contract.fixedPrice * contract.quantity;
    }
    return 0;
  },

  /**
   * Determines delivery status based on contract data
   */
  getDeliveryStatus: (contract: PhysicalContractDto): string => {
    const today = new Date();
    const laycanStart = new Date(contract.laycanStart);
    const laycanEnd = new Date(contract.laycanEnd);

    if (contract.actualDeliveryDate) {
      return 'Delivered';
    }

    if (contract.liftingStatus === 'Completed' && contract.dischargeStatus === 'Completed') {
      return 'Delivered';
    }

    if (contract.liftingStatus === 'InProgress' || contract.dischargeStatus === 'InProgress') {
      return 'In Transit';
    }

    if (today < laycanStart) {
      return 'Pending';
    }

    if (today >= laycanStart && today <= laycanEnd) {
      return 'In Laycan';
    }

    if (today > laycanEnd) {
      return 'Overdue';
    }

    return 'Unknown';
  },

  /**
   * Filters contracts by delivery status
   */
  filterByDeliveryStatus: (contracts: PhysicalContractListDto[], status: string): PhysicalContractListDto[] => {
    return contracts.filter(contract => contract.deliveryStatus === status);
  },

  /**
   * Groups contracts by product
   */
  groupByProduct: (contracts: PhysicalContractListDto[]): { [productName: string]: PhysicalContractListDto[] } => {
    return contracts.reduce((groups, contract) => {
      const productName = contract.productName;
      if (!groups[productName]) {
        groups[productName] = [];
      }
      groups[productName].push(contract);
      return groups;
    }, {} as { [productName: string]: PhysicalContractListDto[] });
  },

  /**
   * Calculates total value for a list of contracts
   */
  calculateTotalValue: (contracts: PhysicalContractListDto[]): number => {
    return contracts.reduce((total, contract) => total + (contract.totalValue || 0), 0);
  }
};

export default physicalContractsApi;