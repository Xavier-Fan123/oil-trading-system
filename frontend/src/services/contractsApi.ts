import axios from 'axios';
import {
  PurchaseContract,
  CreatePurchaseContractDto,
  UpdatePurchaseContractDto,
  PagedResult,
  PurchaseContractListDto,
  ContractFilters,
  TradingPartner,
  Product
} from '@/types/contracts';
import { 
  parseApiDateFields, 
  formatApiDateFields, 
  formatApiDate 
} from '@/utils/dateUtils';

// Use relative path to leverage Vite proxy configuration
const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Purchase Contracts API
export const purchaseContractsApi = {
  getAll: async (filters?: ContractFilters): Promise<PagedResult<PurchaseContractListDto>> => {
    const params = new URLSearchParams();
    if (filters?.status !== undefined) params.append('status', filters.status.toString());
    if (filters?.supplierId) params.append('supplierId', filters.supplierId);
    if (filters?.productId) params.append('productId', filters.productId);
    if (filters?.laycanStart) {
      const formattedDate = formatApiDate(filters.laycanStart);
      if (formattedDate) params.append('laycanStart', formattedDate);
    }
    if (filters?.laycanEnd) {
      const formattedDate = formatApiDate(filters.laycanEnd);
      if (formattedDate) params.append('laycanEnd', formattedDate);
    }
    if (filters?.pageNumber) params.append('pageNumber', filters.pageNumber.toString());
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

    const response = await api.get(`/purchase-contracts?${params.toString()}`);
    
    // Parse date fields in the response
    const result = response.data;
    if (result.items) {
      result.items = result.items.map((item: any) => 
        parseApiDateFields(item, ['laycanStart', 'laycanEnd', 'createdAt', 'updatedAt'])
      );
    }
    
    return result;
  },

  getById: async (id: string): Promise<PurchaseContract> => {
    const response = await api.get(`/purchase-contracts/${id}`);
    
    // Parse date fields in the response
    return parseApiDateFields(response.data, [
      'laycanStart', 'laycanEnd', 'pricingPeriodStart', 'pricingPeriodEnd',
      'createdAt', 'updatedAt'
    ]);
  },

  create: async (contract: CreatePurchaseContractDto, options?: { forceCreate?: boolean }): Promise<string> => {
    // Format date fields for API request
    const formattedContract = formatApiDateFields(contract, [
      'laycanStart', 'laycanEnd', 'pricingPeriodStart', 'pricingPeriodEnd'
    ]);

    try {
      const response = await api.post('/purchase-contracts', formattedContract);
      return response.data;
    } catch (error: any) {
      // If we get a risk violation and haven't tried to override yet
      if (error.response?.status === 400 &&
          error.response?.data?.error === 'Risk limit violation' &&
          error.response?.data?.riskDetails?.allowOverride &&
          !options?.forceCreate) {
        // Retry with risk override header
        const response = await api.post('/purchase-contracts', formattedContract, {
          headers: {
            'X-Risk-Override': 'true'
          }
        });
        return response.data;
      }
      throw error;
    }
  },

  update: async (id: string, contract: UpdatePurchaseContractDto, options?: { forceUpdate?: boolean }): Promise<void> => {
    // Format date fields for API request
    const formattedContract = formatApiDateFields(contract, [
      'laycanStart', 'laycanEnd', 'pricingPeriodStart', 'pricingPeriodEnd'
    ]);

    try {
      await api.put(`/purchase-contracts/${id}`, formattedContract);
    } catch (error: any) {
      // If we get a risk violation and haven't tried to override yet
      if (error.response?.status === 400 &&
          error.response?.data?.error === 'Risk limit violation' &&
          error.response?.data?.riskDetails?.allowOverride &&
          !options?.forceUpdate) {
        // Retry with risk override header
        await api.put(`/purchase-contracts/${id}`, formattedContract, {
          headers: {
            'X-Risk-Override': 'true'
          }
        });
        return;
      }
      throw error;
    }
  },

  activate: async (id: string): Promise<void> => {
    await api.post(`/purchase-contracts/${id}/activate`);
  },

  getAvailableQuantity: async (id: string) => {
    const response = await api.get(`/purchase-contracts/${id}/available-quantity`);
    return response.data;
  }
};


// Trading Partners API
export const tradingPartnersApi = {
  getAll: async (): Promise<TradingPartner[]> => {
    const response = await api.get('/trading-partners');
    return response.data;
  }
};

// Products API - Real implementation using ProductController
export const productsApi = {
  /**
   * GET /api/products
   * Retrieves all active products from the backend
   */
  getAll: async (filters?: {
    type?: number;
    code?: string;
    name?: string;
    isActive?: boolean;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<Product[]> => {
    const params = new URLSearchParams();
    if (filters?.type !== undefined) params.append('type', filters.type.toString());
    if (filters?.code) params.append('code', filters.code);
    if (filters?.name) params.append('name', filters.name);
    if (filters?.isActive !== undefined) params.append('isActive', filters.isActive.toString());
    if (filters?.pageNumber) params.append('pageNumber', filters.pageNumber.toString());
    if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

    const url = params.toString() ? `/products?${params.toString()}` : '/products';
    const response = await api.get(url);
    return response.data;
  },

  /**
   * GET /api/products/{id}
   * Retrieves a specific product by ID
   */
  getById: async (id: string): Promise<Product> => {
    const response = await api.get(`/products/${id}`);
    return response.data;
  },

  /**
   * GET /api/products/by-code/{code}
   * Retrieves a specific product by code
   */
  getByCode: async (code: string): Promise<Product> => {
    const response = await api.get(`/products/by-code/${code}`);
    return response.data;
  },

  /**
   * POST /api/products
   * Creates a new product
   */
  create: async (product: {
    code: string;
    name: string;
    type: number;
    grade?: string;
    specification?: string;
    unitOfMeasure?: string;
    density?: number;
    origin?: string;
  }): Promise<Product> => {
    try {
      const response = await api.post('/products', product);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to create product');
      }
      throw error;
    }
  },

  /**
   * PUT /api/products/{id}
   * Updates an existing product
   */
  update: async (id: string, product: {
    name: string;
    grade?: string;
    specification?: string;
    density?: number;
    origin?: string;
  }): Promise<void> => {
    try {
      await api.put(`/products/${id}`, product);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to update product');
      }
      throw error;
    }
  },

  /**
   * DELETE /api/products/{id}
   * Soft deletes a product (sets isActive to false)
   */
  delete: async (id: string): Promise<void> => {
    try {
      await api.delete(`/products/${id}`);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to delete product');
      }
      throw error;
    }
  }
};

export default api;