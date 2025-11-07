import axios from 'axios';
import {
  ContractSettlementDto,
  ContractSettlementListDto,
  CreateSettlementDto,
  CreateSettlementResultDto,
  UpdateSettlementDto,
  AddChargeDto,
  UpdateChargeDto,
  ChargeOperationResultDto,
  SettlementChargeDto,
  SettlementSearchFilters,
  PagedResult,
  PaymentDto,
  PaymentHistoryDto,
  PaymentTrackingDto,
  SettlementHistoryDto,
  PaymentStatus,
  PaymentMethod,
  PaymentTerms,
} from '@/types/settlement';

// Re-export types for convenience
export type {
  ContractSettlementDto,
  ContractSettlementListDto,
  CreateSettlementDto,
  CreateSettlementResultDto,
  UpdateSettlementDto,
  AddChargeDto,
  UpdateChargeDto,
  ChargeOperationResultDto,
  SettlementChargeDto,
  SettlementSearchFilters,
  PagedResult,
  PaymentDto,
  PaymentHistoryDto,
  PaymentTrackingDto,
  SettlementHistoryDto,
  PaymentStatus,
  PaymentMethod,
  PaymentTerms,
};

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Settlement query endpoints - NOW ALIGNED WITH BACKEND SETTLEMENT CONTROLLER
export const settlementApi = {
  // Get settlement by settlement ID
  // Backend: GET /api/settlements/{settlementId}
  getById: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.get(`/settlements/${settlementId}`);
    return response.data;
  },

  // Get settlements with filtering options (includes contractId filter)
  // Backend: GET /api/settlements?pageNumber=1&pageSize=20&contractId=xxx&startDate=xxx&status=xxx&etc
  getSettlements: async (filters: SettlementSearchFilters): Promise<PagedResult<ContractSettlementListDto>> => {
    const params = new URLSearchParams();

    if (filters.startDate) {
      params.append('startDate', filters.startDate.toISOString());
    }
    if (filters.endDate) {
      params.append('endDate', filters.endDate.toISOString());
    }
    if (filters.status !== undefined) {
      params.append('status', filters.status.toString());
    }
    if (filters.contractId) {
      params.append('contractId', filters.contractId);
    }
    if (filters.externalContractNumber) {
      params.append('externalContractNumber', filters.externalContractNumber);
    }
    if (filters.documentNumber) {
      params.append('documentNumber', filters.documentNumber);
    }

    params.append('pageNumber', filters.pageNumber.toString());
    params.append('pageSize', filters.pageSize.toString());

    const response = await api.get(`/settlements?${params.toString()}`);
    return response.data;
  },

  // Get settlements by contract ID (convenience method using getSettlements with contractId filter)
  getByContractId: async (contractId: string): Promise<ContractSettlementDto[]> => {
    const result = await settlementApi.getSettlements({
      contractId,
      pageNumber: 1,
      pageSize: 100
    });
    // Convert list DTOs to full DTOs
    return await Promise.all(
      result.data.map(item => settlementApi.getById(item.id))
    );
  },

  // Search settlements by various criteria (convenience method)
  searchSettlements: async (searchTerm: string, pageNumber: number = 1, pageSize: number = 20): Promise<PagedResult<ContractSettlementListDto>> => {
    // Search by external contract number using query filter
    const filters: SettlementSearchFilters = {
      externalContractNumber: searchTerm,
      pageNumber,
      pageSize
    };
    return await settlementApi.getSettlements(filters);
  },

  // Create a new settlement (generic endpoint)
  // Backend: POST /api/settlements
  // Accepts CreateSettlementRequestDto with contractId
  // Backend determines if purchase or sales and routes to appropriate handler
  createSettlement: async (dto: CreateSettlementDto): Promise<CreateSettlementResultDto> => {
    const response = await api.post('/settlements', {
      contractId: dto.contractId,
      externalContractNumber: dto.externalContractNumber,
      documentNumber: dto.documentNumber,
      documentType: dto.documentType,
      documentDate: dto.documentDate,
      actualQuantityMT: dto.actualQuantityMT,
      actualQuantityBBL: dto.actualQuantityBBL,
      createdBy: dto.createdBy,
      notes: dto.notes,
      settlementCurrency: dto.settlementCurrency,
      autoCalculatePrices: dto.autoCalculatePrices,
      autoTransitionStatus: dto.autoTransitionStatus
    });
    return response.data;
  },

  // Create settlement by external contract number
  // Backend: POST /api/settlements/create-by-external-contract
  // Accepts CreateSettlementByExternalContractDto with externalContractNumber
  createByExternalContractNumber: async (dto: any): Promise<CreateSettlementResultDto> => {
    const response = await api.post('/settlements/create-by-external-contract', {
      externalContractNumber: dto.externalContractNumber,
      documentNumber: dto.documentNumber,
      documentType: dto.documentType,
      documentDate: dto.documentDate
    });
    return response.data;
  },

  // Update an existing settlement
  // Backend: PUT /api/settlements/{settlementId}
  updateSettlement: async (settlementId: string, dto: UpdateSettlementDto): Promise<ContractSettlementDto> => {
    const response = await api.put(`/settlements/${settlementId}`, {
      documentNumber: dto.documentNumber,
      documentType: dto.documentType,
      documentDate: dto.documentDate
    });
    return response.data;
  },

  // Calculate settlement amounts (benchmark, adjustment, cargo value)
  // Backend: POST /api/settlements/{settlementId}/calculate
  // Generic endpoint that determines if purchase or sales
  calculateSettlement: async (settlementId: string, request: any): Promise<ContractSettlementDto> => {
    const response = await api.post(`/settlements/${settlementId}/calculate`, request);
    return response.data;
  },

  // Calculate purchase settlement specifically
  // Backend: POST /api/purchase-settlements/{settlementId}/calculate
  calculatePurchaseSettlement: async (settlementId: string, request: any): Promise<ContractSettlementDto> => {
    const response = await api.post(`/purchase-settlements/${settlementId}/calculate`, request);
    return response.data;
  },

  // Calculate sales settlement specifically
  // Backend: POST /api/sales-settlements/{settlementId}/calculate
  calculateSalesSettlement: async (settlementId: string, request: any): Promise<ContractSettlementDto> => {
    const response = await api.post(`/sales-settlements/${settlementId}/calculate`, request);
    return response.data;
  },

  // Approve settlement for finalization
  // Backend: POST /api/settlements/{settlementId}/approve
  // Generic endpoint that determines if purchase or sales
  approveSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/settlements/${settlementId}/approve`);
    return response.data;
  },

  // Approve purchase settlement specifically
  // Backend: POST /api/purchase-settlements/{settlementId}/approve
  approvePurchaseSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/purchase-settlements/${settlementId}/approve`);
    return response.data;
  },

  // Approve sales settlement specifically
  // Backend: POST /api/sales-settlements/{settlementId}/approve
  approveSalesSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/sales-settlements/${settlementId}/approve`);
    return response.data;
  },

  // Finalize settlement (lock for editing)
  // Backend: POST /api/settlements/{settlementId}/finalize
  // Generic endpoint that determines if purchase or sales
  finalizeSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/settlements/${settlementId}/finalize`);
    return response.data;
  },

  // Finalize purchase settlement specifically
  // Backend: POST /api/purchase-settlements/{settlementId}/finalize
  finalizePurchaseSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/purchase-settlements/${settlementId}/finalize`);
    return response.data;
  },

  // Finalize sales settlement specifically
  // Backend: POST /api/sales-settlements/{settlementId}/finalize
  finalizeSalesSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/sales-settlements/${settlementId}/finalize`);
    return response.data;
  },

  // Recalculate settlement (convenience method)
  recalculateSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    return settlementApi.calculateSettlement(settlementId, {});
  }
};

// Payment management endpoints (P2 phase)
export const settlementPaymentApi = {
  // Get all payments for a settlement
  getPayments: async (settlementId: string): Promise<PaymentDto[]> => {
    const response = await api.get(`/settlements/${settlementId}/payments`);
    return response.data;
  },

  // Get a specific payment
  getPayment: async (settlementId: string, paymentId: string): Promise<PaymentDto> => {
    const response = await api.get(`/settlements/${settlementId}/payments/${paymentId}`);
    return response.data;
  },

  // Record a new payment for a settlement
  recordPayment: async (settlementId: string, paymentData: Omit<PaymentDto, 'id' | 'createdDate' | 'createdBy' | 'formattedAmount' | 'formattedDueDate'>): Promise<PaymentDto> => {
    const response = await api.post(`/settlements/${settlementId}/payments`, paymentData);
    return response.data;
  },

  // Update an existing payment
  updatePayment: async (settlementId: string, paymentId: string, paymentData: Partial<PaymentDto>): Promise<PaymentDto> => {
    const response = await api.put(`/settlements/${settlementId}/payments/${paymentId}`, paymentData);
    return response.data;
  },

  // Cancel/delete a payment
  cancelPayment: async (settlementId: string, paymentId: string): Promise<void> => {
    await api.delete(`/settlements/${settlementId}/payments/${paymentId}`);
  },

  // Get payment tracking summary for a settlement
  getPaymentTracking: async (settlementId: string): Promise<PaymentTrackingDto> => {
    const response = await api.get(`/settlements/${settlementId}/payment-tracking`);
    return response.data;
  },

  // Get payment history timeline for a settlement
  getPaymentHistory: async (settlementId: string): Promise<PaymentHistoryDto[]> => {
    const response = await api.get(`/settlements/${settlementId}/payment-history`);
    return response.data;
  },

  // Update payment terms for a settlement
  updatePaymentTerms: async (settlementId: string, paymentTerms: PaymentTerms, paymentMethod: PaymentMethod, expectedPaymentDate?: Date): Promise<ContractSettlementDto> => {
    const response = await api.put(`/settlements/${settlementId}/payment-terms`, {
      paymentTerms,
      paymentMethod,
      expectedPaymentDate
    });
    return response.data;
  },

  // Mark settlement payment as complete
  markPaymentComplete: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/settlements/${settlementId}/mark-payment-complete`);
    return response.data;
  }
};

// Settlement history endpoints (P2 phase)
export const settlementHistoryApi = {
  // Get settlement history timeline
  getHistory: async (settlementId: string): Promise<SettlementHistoryDto[]> => {
    const response = await api.get(`/settlements/${settlementId}/history`);
    return response.data;
  },

  // Get settlement history for a contract (all settlements for that contract)
  getContractHistory: async (contractId: string): Promise<SettlementHistoryDto[]> => {
    const response = await api.get(`/contracts/${contractId}/settlement-history`);
    return response.data;
  }
};

// Settlement charge management endpoints
export const settlementChargeApi = {
  // Get all charges for a settlement
  getCharges: async (settlementId: string): Promise<SettlementChargeDto[]> => {
    const response = await api.get(`/settlements/${settlementId}/charges`);
    return response.data;
  },

  // Add a new charge to a settlement
  addCharge: async (settlementId: string, dto: Omit<AddChargeDto, 'settlementId' | 'addedBy'>): Promise<ChargeOperationResultDto> => {
    const chargeDto: AddChargeDto = {
      ...dto,
      settlementId,
      addedBy: 'CurrentUser' // This would typically come from auth context
    };
    const response = await api.post(`/settlements/${settlementId}/charges`, chargeDto);
    return response.data;
  },

  // Update an existing charge
  updateCharge: async (settlementId: string, chargeId: string, dto: Omit<UpdateChargeDto, 'settlementId' | 'chargeId' | 'updatedBy'>): Promise<ChargeOperationResultDto> => {
    const updateDto: UpdateChargeDto = {
      ...dto,
      settlementId,
      chargeId,
      updatedBy: 'CurrentUser' // This would typically come from auth context
    };
    const response = await api.put(`/settlements/${settlementId}/charges/${chargeId}`, updateDto);
    return response.data;
  },

  // Remove a charge from a settlement
  removeCharge: async (settlementId: string, chargeId: string): Promise<ChargeOperationResultDto> => {
    const response = await api.delete(`/settlements/${settlementId}/charges/${chargeId}`);
    return response.data;
  }
};

// Error handling wrapper functions
export const settlementApiWithErrorHandling = {
  async getByExternalContractNumber(externalContractNumber: string): Promise<ContractSettlementDto | null> {
    try {
      // Search for settlements by external contract number
      const result = await settlementApi.searchSettlements(externalContractNumber, 1, 1);
      if (result.data && result.data.length > 0) {
        // Get full settlement details
        return await settlementApi.getById(result.data[0].id);
      }
      return null;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Settlement not found
      }
      throw error;
    }
  },

  async getByContractId(contractId: string): Promise<ContractSettlementDto[] | null> {
    try {
      return await settlementApi.getByContractId(contractId);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // No settlements found
      }
      throw error;
    }
  },

  async getById(settlementId: string): Promise<ContractSettlementDto | null> {
    try {
      return await settlementApi.getById(settlementId);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Settlement not found
      }
      throw error;
    }
  },

  async searchSettlements(searchTerm: string, pageNumber: number = 1, pageSize: number = 20): Promise<PagedResult<ContractSettlementListDto>> {
    return await settlementApi.searchSettlements(searchTerm, pageNumber, pageSize);
  }
};

// Bulk operations endpoints
export const bulkSettlementApi = {
  // Bulk approve settlements
  // Backend: POST /api/settlements/bulk-approve
  bulkApprove: async (settlementIds: string[]): Promise<{ successCount: number; failureCount: number; details: any[] }> => {
    const response = await api.post('/settlements/bulk-approve', {
      settlementIds,
      approvedBy: 'System'
    });
    return response.data;
  },

  // Bulk finalize settlements
  // Backend: POST /api/settlements/bulk-finalize
  bulkFinalize: async (settlementIds: string[]): Promise<{ successCount: number; failureCount: number; details: any[] }> => {
    const response = await api.post('/settlements/bulk-finalize', {
      settlementIds,
      finalizedBy: 'System'
    });
    return response.data;
  },

  // Bulk export settlements
  // Backend: POST /api/settlements/bulk-export
  bulkExport: async (settlementIds: string[], format: 'excel' | 'csv' | 'pdf'): Promise<Blob> => {
    const response = await api.post('/settlements/bulk-export', {
      settlementIds,
      format
    }, {
      responseType: 'blob'
    });
    return response.data;
  },

  // Helper function to download exported file
  downloadExport: (blob: Blob, filename: string) => {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
};

// Fallback functions with error handling for components
export const getSettlementWithFallback = async (settlementId: string): Promise<ContractSettlementDto | null> => {
  return await settlementApiWithErrorHandling.getById(settlementId);
};

export const searchSettlementsWithFallback = async (searchTerm: string, pageNumber: number = 1, pageSize: number = 20): Promise<PagedResult<ContractSettlementListDto>> => {
  return await settlementApiWithErrorHandling.searchSettlements(searchTerm, pageNumber, pageSize);
};


export default api;