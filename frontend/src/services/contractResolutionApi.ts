import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * DTO for contract resolution result
 */
export interface ContractCandidateDto {
  id: string;
  contractNumber: string;
  externalContractNumber: string;
  contractType: 'Purchase' | 'Sales';
  tradingPartnerName: string;
  productName: string;
  quantity: number;
  quantityUnit: string;
  status: string;
  createdAt: string;
}

export interface ContractResolutionResultDto {
  success: boolean;
  contractId?: string;
  contractType?: string;
  errorMessage?: string;
  candidates: ContractCandidateDto[];
}

/**
 * Contract Resolution API
 * Provides methods for resolving external contract numbers to internal GUIDs
 */
export const contractResolutionApi = {
  /**
   * Resolve an external contract number to a contract GUID
   * Supports optional filters for disambiguation
   */
  resolve: async (
    externalContractNumber: string,
    contractType?: string,
    tradingPartnerId?: string,
    productId?: string
  ): Promise<ContractResolutionResultDto> => {
    const params = new URLSearchParams();
    params.append('externalContractNumber', externalContractNumber);

    if (contractType) params.append('contractType', contractType);
    if (tradingPartnerId) params.append('tradingPartnerId', tradingPartnerId);
    if (productId) params.append('productId', productId);

    const response = await api.get(`/contracts/resolve?${params.toString()}`);
    return response.data;
  },

  /**
   * Search for contracts by external number
   * Returns all matching contracts regardless of success/failure
   */
  searchByExternalNumber: async (
    externalContractNumber: string,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<{ data: ContractCandidateDto[]; totalCount: number; pageNumber: number; pageSize: number }> => {
    const params = new URLSearchParams();
    params.append('externalContractNumber', externalContractNumber);
    params.append('pageNumber', pageNumber.toString());
    params.append('pageSize', pageSize.toString());

    const response = await api.get(`/contracts/search-by-external?${params.toString()}`);
    return response.data;
  },

  /**
   * Get a contract by its external number (convenience method)
   * Returns the contract if found, throws error if not found
   */
  getByExternalNumber: async (externalContractNumber: string): Promise<ContractCandidateDto> => {
    const result = await contractResolutionApi.resolve(externalContractNumber);

    if (result.success && result.contractId) {
      // Get the matching candidate
      const candidates = result.candidates || [];
      if (candidates.length === 1) {
        return candidates[0];
      }
    }

    throw new Error(result.errorMessage || 'Contract not found');
  },

  /**
   * Resolve with automatic candidate selection logic
   * If multiple candidates, returns them for user selection
   * If single candidate, returns it
   * If none found, throws error
   */
  resolveWithCandidates: async (
    externalContractNumber: string,
    expectedContractType?: string,
    tradingPartnerId?: string,
    productId?: string
  ): Promise<{
    resolved: boolean;
    contractId?: string;
    candidates?: ContractCandidateDto[];
    error?: string;
  }> => {
    try {
      const result = await contractResolutionApi.resolve(
        externalContractNumber,
        expectedContractType,
        tradingPartnerId,
        productId
      );

      if (result.success && result.contractId) {
        return {
          resolved: true,
          contractId: result.contractId,
        };
      }

      // Return candidates for disambiguation
      if (result.candidates && result.candidates.length > 0) {
        return {
          resolved: false,
          candidates: result.candidates,
          error: result.errorMessage,
        };
      }

      return {
        resolved: false,
        error: result.errorMessage || 'Contract not found',
      };
    } catch (error: any) {
      return {
        resolved: false,
        error: error?.response?.data?.errorMessage || error.message || 'Failed to resolve contract',
      };
    }
  },
};

export default contractResolutionApi;
