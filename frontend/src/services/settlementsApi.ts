import axios, { AxiosError } from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Settlement DTOs
 */
export interface CreatePurchaseSettlementRequest {
  purchaseContractId: string;
  externalContractNumber?: string;
  documentNumber: string;
  documentType: string;
  documentDate: string;
}

export interface CreateSalesSettlementRequest {
  salesContractId: string;
  externalContractNumber?: string;
  documentNumber: string;
  documentType: string;
  documentDate: string;
}

export interface CalculateSettlementRequest {
  calculationQuantityMT: number;
  calculationQuantityBBL: number;
  benchmarkAmount: number;
  adjustmentAmount: number;
  calculationNote?: string;
}

export interface Settlement {
  id: string;
  contractId: string;
  settlementNumber: string;
  status: 'Draft' | 'Calculated' | 'Approved' | 'Finalized';
  calculationQuantityMT: number;
  calculationQuantityBBL: number;
  benchmarkAmount: number;
  adjustmentAmount: number;
  totalAmount: number;
  currency: string;
  documentNumber?: string;
  documentType?: string;
  documentDate?: string;
  createdBy: string;
  createdDate: string;
  approvedBy?: string;
  approvedDate?: string;
  finalizedBy?: string;
  finalizedDate?: string;
}

export interface SettlementListResponse {
  items: Settlement[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  id?: string;
  status?: number;
  error?: string;
  message?: string;
  data?: T;
}

/**
 * Purchase Settlement API endpoints
 */
export const settlementApi = {
  // Purchase Settlements
  createPurchaseSettlement: async (
    request: CreatePurchaseSettlementRequest
  ): Promise<Settlement> => {
    try {
      const response = await api.post<ApiResponse<Settlement>>(
        '/purchase-settlements',
        request
      );
      return response.data as any;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  getPurchaseSettlement: async (settlementId: string): Promise<Settlement> => {
    try {
      const response = await api.get<Settlement>(
        `/purchase-settlements/${settlementId}`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  getPurchaseSettlementsByContract: async (
    contractId: string
  ): Promise<Settlement[]> => {
    try {
      const response = await api.get<Settlement[]>(
        `/purchase-settlements/contract/${contractId}`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  calculatePurchaseSettlement: async (
    settlementId: string,
    request: CalculateSettlementRequest
  ): Promise<Settlement> => {
    try {
      const response = await api.post<Settlement>(
        `/purchase-settlements/${settlementId}/calculate`,
        request
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  approvePurchaseSettlement: async (settlementId: string): Promise<Settlement> => {
    try {
      const response = await api.post<Settlement>(
        `/purchase-settlements/${settlementId}/approve`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  finalizePurchaseSettlement: async (
    settlementId: string
  ): Promise<Settlement> => {
    try {
      const response = await api.post<Settlement>(
        `/purchase-settlements/${settlementId}/finalize`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  // Sales Settlements
  createSalesSettlement: async (
    request: CreateSalesSettlementRequest
  ): Promise<Settlement> => {
    try {
      const response = await api.post<ApiResponse<Settlement>>(
        '/sales-settlements',
        request
      );
      return response.data as any;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  getSalesSettlement: async (settlementId: string): Promise<Settlement> => {
    try {
      const response = await api.get<Settlement>(
        `/sales-settlements/${settlementId}`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  getSalesSettlementsByContract: async (
    contractId: string
  ): Promise<Settlement[]> => {
    try {
      const response = await api.get<Settlement[]>(
        `/sales-settlements/contract/${contractId}`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  calculateSalesSettlement: async (
    settlementId: string,
    request: CalculateSettlementRequest
  ): Promise<Settlement> => {
    try {
      const response = await api.post<Settlement>(
        `/sales-settlements/${settlementId}/calculate`,
        request
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  approveSalesSettlement: async (settlementId: string): Promise<Settlement> => {
    try {
      const response = await api.post<Settlement>(
        `/sales-settlements/${settlementId}/approve`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  finalizeSalesSettlement: async (settlementId: string): Promise<Settlement> => {
    try {
      const response = await api.post<Settlement>(
        `/sales-settlements/${settlementId}/finalize`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },
};

/**
 * Error handling utility
 */
function handleApiError(error: unknown): Error {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<any>;
    const status = axiosError.response?.status;
    const data = axiosError.response?.data;

    if (status === 404) {
      return new Error(data?.message || 'Settlement not found');
    }

    if (status === 400) {
      const details = data?.details || [];
      const message = Array.isArray(details)
        ? details.join(', ')
        : data?.message || 'Invalid request';
      return new Error(message);
    }

    if (status === 422) {
      return new Error(
        data?.message || 'Cannot perform this operation on the settlement'
      );
    }

    return new Error(
      axiosError.message || 'An error occurred while processing the settlement'
    );
  }

  return error instanceof Error ? error : new Error('Unknown error occurred');
}

export default settlementApi;
