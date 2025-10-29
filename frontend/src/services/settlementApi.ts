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
} from '@/types/settlement';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Settlement query endpoints
export const settlementApi = {
  // Get settlement by external contract number
  getByExternalContractNumber: async (externalContractNumber: string): Promise<ContractSettlementDto> => {
    const response = await api.get(`/settlements/by-external-contract/${encodeURIComponent(externalContractNumber)}`);
    return response.data;
  },

  // Get settlement by contract ID
  getByContractId: async (contractId: string): Promise<ContractSettlementDto> => {
    const response = await api.get(`/settlements/contract/${contractId}`);
    return response.data;
  },

  // Get settlement by settlement ID
  getById: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.get(`/settlements/${settlementId}`);
    return response.data;
  },

  // Get settlements with filtering options
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

  // Search settlements by various criteria (convenience method)
  searchSettlements: async (searchTerm: string, pageNumber: number = 1, pageSize: number = 20): Promise<PagedResult<ContractSettlementListDto>> => {
    // First try to search by external contract number
    try {
      const settlement = await settlementApi.getByExternalContractNumber(searchTerm);
      if (!settlement) {
        // Settlement not found, fall back to partial search
        const filters: SettlementSearchFilters = {
          externalContractNumber: searchTerm,
          pageNumber,
          pageSize
        };
        return await settlementApi.getSettlements(filters);
      }

      return {
        data: [{
          id: settlement.id,
          contractId: settlement.contractId,
          contractNumber: settlement.contractNumber,
          externalContractNumber: settlement.externalContractNumber,
          documentNumber: settlement.documentNumber,
          documentType: settlement.documentType,
          documentDate: settlement.documentDate,
          actualQuantityMT: settlement.actualQuantityMT,
          actualQuantityBBL: settlement.actualQuantityBBL,
          totalSettlementAmount: settlement.totalSettlementAmount,
          settlementCurrency: settlement.settlementCurrency,
          status: settlement.status,
          isFinalized: settlement.isFinalized,
          createdDate: settlement.createdDate,
          createdBy: settlement.createdBy,
          chargesCount: settlement.charges?.length || 0,
          formattedAmount: settlement.formattedTotalAmount,
          displayStatus: settlement.displayStatus
        }],
        totalCount: 1,
        page: 1,
        pageSize: 1,
        totalPages: 1
      };
    } catch (error) {
      // If exact match fails, do a partial search
      const filters: SettlementSearchFilters = {
        externalContractNumber: searchTerm,
        pageNumber,
        pageSize
      };
      return await settlementApi.getSettlements(filters);
    }
  },

  // Create a new settlement
  createSettlement: async (dto: CreateSettlementDto): Promise<CreateSettlementResultDto> => {
    const response = await api.post('/settlements', dto);
    return response.data;
  },

  // Update an existing settlement
  updateSettlement: async (settlementId: string, dto: UpdateSettlementDto): Promise<ContractSettlementDto> => {
    const response = await api.put(`/settlements/${settlementId}`, dto);
    return response.data;
  },

  // Recalculate settlement amounts
  recalculateSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/settlements/${settlementId}/recalculate`);
    return response.data;
  },

  // Finalize a settlement
  finalizeSettlement: async (settlementId: string): Promise<ContractSettlementDto> => {
    const response = await api.post(`/settlements/${settlementId}/finalize`);
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
      return await settlementApi.getByExternalContractNumber(externalContractNumber);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Settlement not found
      }
      throw error;
    }
  },

  async getByContractId(contractId: string): Promise<ContractSettlementDto | null> {
    try {
      return await settlementApi.getByContractId(contractId);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Settlement not found
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

// Fallback functions with error handling for components
export const getSettlementWithFallback = async (settlementId: string): Promise<ContractSettlementDto | null> => {
  return await settlementApiWithErrorHandling.getById(settlementId);
};

export const searchSettlementsWithFallback = async (searchTerm: string, pageNumber: number = 1, pageSize: number = 20): Promise<PagedResult<ContractSettlementListDto>> => {
  return await settlementApiWithErrorHandling.searchSettlements(searchTerm, pageNumber, pageSize);
};


export default api;