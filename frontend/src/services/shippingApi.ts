import api from './api';
import type {
  ShippingOperationDto,
  ShippingOperationSummaryDtoPagedResult,
  CreateShippingOperationDto,
  UpdateShippingOperationDto,
  RecordLiftingOperationDto,
  CancelShippingOperationDto,
  ShippingFilters,
} from '@/types/shipping';

export const shippingApi = {
  // Get all shipping operations with filtering and pagination
  getAll: async (filters?: ShippingFilters): Promise<ShippingOperationSummaryDtoPagedResult> => {
    const params = new URLSearchParams();
    
    if (filters?.page) params.append('Page', filters.page.toString());
    if (filters?.pageSize) params.append('PageSize', filters.pageSize.toString());
    if (filters?.shippingNumber) params.append('ShippingNumber', filters.shippingNumber);
    if (filters?.contractId) params.append('ContractId', filters.contractId);
    if (filters?.vesselName) params.append('VesselName', filters.vesselName);
    if (filters?.status) params.append('Status', filters.status.toString());
    if (filters?.loadPort) params.append('LoadPort', filters.loadPort);
    if (filters?.dischargePort) params.append('DischargePort', filters.dischargePort);
    if (filters?.startDate) params.append('StartDate', filters.startDate);
    if (filters?.endDate) params.append('EndDate', filters.endDate);

    const query = params.toString() ? `?${params.toString()}` : '';
    const response = await api.get(`/shipping-operations${query}`);
    return response.data;
  },

  // Get shipping operation by ID
  getById: async (id: string): Promise<ShippingOperationDto> => {
    const response = await api.get(`/shipping-operations/${id}`);
    return response.data;
  },

  // Create new shipping operation
  create: async (operation: CreateShippingOperationDto): Promise<ShippingOperationDto> => {
    const response = await api.post('/shipping-operations', operation);
    return response.data;
  },

  // Update shipping operation
  update: async (id: string, operation: UpdateShippingOperationDto): Promise<ShippingOperationDto> => {
    const response = await api.put(`/shipping-operations/${id}`, operation);
    return response.data;
  },

  // Delete shipping operation
  delete: async (id: string): Promise<void> => {
    await api.delete(`/shipping-operations/${id}`);
  },

  // Get shipping operations by contract ID
  getByContractId: async (contractId: string): Promise<ShippingOperationDto[]> => {
    const response = await api.get(`/shipping-operations/by-contract/${contractId}`);
    return response.data;
  },

  // Record lifting operation
  recordLifting: async (liftingData: RecordLiftingOperationDto): Promise<void> => {
    await api.post('/shipping-operations/record-lifting', liftingData);
  },

  // Start loading operation
  startLoading: async (operationId: string): Promise<void> => {
    await api.post(`/shipping-operations/${operationId}/start-loading`);
  },

  // Complete loading operation
  completeLoading: async (operationId: string): Promise<void> => {
    await api.post(`/shipping-operations/${operationId}/complete-loading`);
  },

  // Complete discharge operation
  completeDischarge: async (operationId: string): Promise<void> => {
    await api.post(`/shipping-operations/${operationId}/complete-discharge`);
  },

  // Cancel shipping operation
  cancel: async (operationId: string, reason: string): Promise<void> => {
    const cancelData: CancelShippingOperationDto = { reason };
    await api.post(`/shipping-operations/${operationId}/cancel`, cancelData);
  },

  // Utility functions
  formatQuantity: (quantity: number, unit: string): string => {
    return `${quantity.toLocaleString()} ${unit}`;
  },

  formatDuration: (startDate: string, endDate?: string): string => {
    const start = new Date(startDate);
    const end = endDate ? new Date(endDate) : new Date();
    const diffMs = end.getTime() - start.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    
    if (diffDays > 0) {
      return `${diffDays}d ${diffHours}h`;
    }
    return `${diffHours}h`;
  },

  calculateDemurrage: (plannedDays: number, actualDays: number, ratePerDay: number): number => {
    const extraDays = Math.max(0, actualDays - plannedDays);
    return extraDays * ratePerDay;
  },

  getStatusColor: (status: number) => {
    switch (status) {
      case 1: return 'default'; // Planned
      case 2: return 'info';    // InTransit
      case 3: return 'warning'; // Loading
      case 4: return 'primary'; // Loaded
      case 5: return 'warning'; // Discharging
      case 6: return 'success'; // Completed
      case 7: return 'error';   // Cancelled
      default: return 'default';
    }
  },

  getStatusLabel: (status: number) => {
    switch (status) {
      case 1: return 'Planned';
      case 2: return 'In Transit';
      case 3: return 'Loading';
      case 4: return 'Loaded';
      case 5: return 'Discharging';
      case 6: return 'Completed';
      case 7: return 'Cancelled';
      default: return 'Unknown';
    }
  },
};