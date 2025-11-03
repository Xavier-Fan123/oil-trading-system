import axios from 'axios';
import {
  SalesContract,
  CreateSalesContractDto,
  UpdateSalesContractDto,
  SalesContractListDto,
  SalesContractFilters,
  SalesContractSummary
} from '@/types/salesContracts';
import {
  PagedResult
} from '@/types/contracts';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Sales Contracts API
export const salesContractsApi = {
  // Get all sales contracts with filtering and pagination
  getAll: async (filters?: SalesContractFilters): Promise<PagedResult<SalesContractListDto>> => {
    const params = new URLSearchParams();
    if (filters?.status !== undefined) params.append('status', filters.status.toString());
    if (filters?.customerId) params.append('customerId', filters.customerId);
    if (filters?.productId) params.append('productId', filters.productId);
    if (filters?.laycanStart) params.append('laycanStart', filters.laycanStart.toISOString());
    if (filters?.laycanEnd) params.append('laycanEnd', filters.laycanEnd.toISOString());
    if (filters?.minValue) params.append('minValue', filters.minValue.toString());
    if (filters?.maxValue) params.append('maxValue', filters.maxValue.toString());
    if (filters?.pageNumber) params.append('pageNumber', filters.pageNumber.toString());
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

    const response = await api.get(`/sales-contracts?${params.toString()}`);
    return response.data;
  },

  // Get single sales contract by ID
  getById: async (id: string): Promise<SalesContract> => {
    const response = await api.get(`/sales-contracts/${id}`);
    return response.data;
  },

  // Create new sales contract
  create: async (contract: CreateSalesContractDto, options?: { forceCreate?: boolean }): Promise<string> => {
    try {
      const response = await api.post('/sales-contracts', contract);
      return response.data;
    } catch (error: any) {
      // If we get a risk violation and haven't tried to override yet
      if (error.response?.status === 400 &&
          error.response?.data?.error === 'Risk limit violation' &&
          error.response?.data?.riskDetails?.allowOverride &&
          !options?.forceCreate) {
        // Retry with risk override header
        const response = await api.post('/sales-contracts', contract, {
          headers: {
            'X-Risk-Override': 'true'
          }
        });
        return response.data;
      }

      console.error('Sales contract creation failed', {
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        config: {
          method: error.config?.method,
          url: error.config?.url,
          data: error.config?.data
        }
      });
      throw error;
    }
  },

  // Update existing sales contract
  update: async (id: string, contract: UpdateSalesContractDto, options?: { forceUpdate?: boolean }): Promise<void> => {
    try {
      await api.put(`/sales-contracts/${id}`, contract);
    } catch (error: any) {
      // If we get a risk violation and haven't tried to override yet
      if (error.response?.status === 400 &&
          error.response?.data?.error === 'Risk limit violation' &&
          error.response?.data?.riskDetails?.allowOverride &&
          !options?.forceUpdate) {
        // Retry with risk override header
        await api.put(`/sales-contracts/${id}`, contract, {
          headers: {
            'X-Risk-Override': 'true'
          }
        });
        return;
      }
      throw error;
    }
  },

  // Delete sales contract
  delete: async (id: string): Promise<void> => {
    await api.delete(`/sales-contracts/${id}`);
  },

  // Get sales contract summary
  getSummary: async (): Promise<SalesContractSummary> => {
    const response = await api.get('/sales-contracts/summary');
    return response.data;
  },

  // Approve sales contract
  approve: async (id: string): Promise<void> => {
    await api.post(`/sales-contracts/${id}/approve`);
  },

  // Reject sales contract
  reject: async (id: string, reason: string): Promise<void> => {
    await api.post(`/sales-contracts/${id}/reject`, { reason });
  },

  // Activate sales contract
  activate: async (id: string): Promise<void> => {
    await api.post(`/sales-contracts/${id}/activate`);
  }
};


export default salesContractsApi;