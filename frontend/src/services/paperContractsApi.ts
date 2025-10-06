import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// TypeScript interfaces for Paper Contracts
export interface PaperContractDto {
  id: string;
  contractMonth: string;
  productType: string;
  position: string;
  quantity: number;
  lotSize: number;
  entryPrice: number;
  currentPrice?: number;
  tradeDate: string;
  settlementDate?: string;
  status: string;
  realizedPnL?: number;
  unrealizedPnL?: number;
  dailyPnL?: number;
  lastMTMDate?: string;
  
  // Spread information
  isSpread: boolean;
  leg1Product?: string;
  leg2Product?: string;
  spreadValue?: number;
  
  // Risk metrics
  vaRValue?: number;
  volatility?: number;
  
  // Additional info
  tradeReference?: string;
  counterpartyName?: string;
  notes?: string;
  
  // Audit
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface PaperContractListDto {
  id: string;
  contractMonth: string;
  productType: string;
  position: string;
  quantity: number;
  entryPrice: number;
  currentPrice?: number;
  unrealizedPnL?: number;
  status: string;
  tradeDate: string;
}

export interface CreatePaperContractDto {
  contractMonth: string; // "AUG25"
  productType: string;   // "380cst"
  position: string;      // "Long" or "Short"
  quantity: number;
  lotSize?: number;      // Default 1000
  entryPrice: number;
  tradeDate: string;
  tradeReference?: string;
  counterpartyName?: string;
  notes?: string;
  createdBy?: string;
}

export interface ClosePositionDto {
  closingPrice: number;
  closeDate: string;
}

export interface MTMUpdateDto {
  contractId: string;
  currentPrice: number;
  mtmDate: string;
  unrealizedPnL: number;
  dailyPnL?: number;
}

export interface UpdateMTMRequest {
  mtmDate: string;
  contractUpdates: Array<{
    contractId: string;
    currentPrice: number;
  }>;
  updatedBy?: string;
}

export interface PnLSummaryDto {
  totalRealizedPnL: number;
  totalUnrealizedPnL: number;
  totalDailyPnL: number;
  numberOfPositions: number;
  openPositionsCount: number;
  closedPositionsCount: number;
  averageEntryPrice: number;
  averageCurrentPrice: number;
  fromDate: string;
  toDate: string;
  productBreakdown: Array<{
    productType: string;
    realizedPnL: number;
    unrealizedPnL: number;
    positionCount: number;
  }>;
  positionBreakdown: Array<{
    position: string;
    realizedPnL: number;
    unrealizedPnL: number;
    positionCount: number;
  }>;
}

export interface PaperContractError {
  error: string;
  message?: string;
}

/**
 * Paper Contracts API service for managing paper trading contracts
 * Corresponds to PaperContractsController endpoints
 */
export const paperContractsApi = {
  /**
   * GET /api/paper-contracts
   * Retrieves all paper contracts
   */
  getAll: async (): Promise<PaperContractListDto[]> => {
    try {
      const response = await api.get('/paper-contracts');
      return response.data;
    } catch (error) {
      console.error('Error fetching paper contracts:', error);
      throw error;
    }
  },

  /**
   * GET /api/paper-contracts/{id}
   * Retrieves a specific paper contract by ID
   */
  getById: async (id: string): Promise<PaperContractDto> => {
    try {
      const response = await api.get(`/paper-contracts/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching paper contract:', error);
      throw error;
    }
  },

  /**
   * GET /api/paper-contracts/open-positions
   * Retrieves all open paper contract positions
   */
  getOpenPositions: async (): Promise<PaperContractListDto[]> => {
    try {
      const response = await api.get('/paper-contracts/open-positions');
      return response.data;
    } catch (error) {
      console.error('Error fetching open positions:', error);
      throw error;
    }
  },

  /**
   * POST /api/paper-contracts
   * Creates a new paper contract
   */
  create: async (contract: CreatePaperContractDto): Promise<PaperContractDto> => {
    try {
      const response = await api.post('/paper-contracts', contract);
      return response.data;
    } catch (error) {
      console.error('Error creating paper contract:', error);
      // Re-throw with enhanced error information
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to create paper contract');
      }
      throw error;
    }
  },

  /**
   * POST /api/paper-contracts/{id}/close
   * Closes a paper contract position
   */
  closePosition: async (id: string, closeData: ClosePositionDto): Promise<PaperContractDto> => {
    try {
      const response = await api.post(`/paper-contracts/${id}/close`, closeData);
      return response.data;
    } catch (error) {
      console.error('Error closing paper contract position:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to close position');
      }
      throw error;
    }
  },

  /**
   * POST /api/paper-contracts/update-mtm
   * Updates mark-to-market for contracts
   */
  updateMarkToMarket: async (request: UpdateMTMRequest): Promise<MTMUpdateDto[]> => {
    try {
      const response = await api.post('/paper-contracts/update-mtm', request);
      return response.data;
    } catch (error) {
      console.error('Error updating mark-to-market:', error);
      if (axios.isAxiosError(error) && error.response?.data) {
        throw new Error(error.response.data.error || error.response.data.message || 'Failed to update MTM');
      }
      throw error;
    }
  },

  /**
   * GET /api/paper-contracts/pnl-summary
   * Retrieves P&L summary for a date range
   */
  getPnLSummary: async (fromDate?: string, toDate?: string): Promise<PnLSummaryDto> => {
    try {
      const params = new URLSearchParams();
      if (fromDate) params.append('fromDate', fromDate);
      if (toDate) params.append('toDate', toDate);

      const response = await api.get(`/paper-contracts/pnl-summary?${params.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching P&L summary:', error);
      throw error;
    }
  },

  // Utility methods for data validation and processing

  /**
   * Validates a create paper contract request
   */
  validateCreateRequest: (request: CreatePaperContractDto): string[] => {
    const errors: string[] = [];

    if (!request.contractMonth || request.contractMonth.trim() === '') {
      errors.push('Contract month is required');
    }

    if (!request.productType || request.productType.trim() === '') {
      errors.push('Product type is required');
    }

    if (!request.position || request.position.trim() === '') {
      errors.push('Position (Long/Short) is required');
    }

    if (!['Long', 'Short'].includes(request.position)) {
      errors.push('Position must be either "Long" or "Short"');
    }

    if (!request.quantity || request.quantity <= 0) {
      errors.push('Quantity must be greater than zero');
    }

    if (!request.entryPrice || request.entryPrice <= 0) {
      errors.push('Entry price must be greater than zero');
    }

    if (!request.tradeDate) {
      errors.push('Trade date is required');
    }

    return errors;
  },

  /**
   * Validates a close position request
   */
  validateCloseRequest: (request: ClosePositionDto): string[] => {
    const errors: string[] = [];

    if (!request.closingPrice || request.closingPrice <= 0) {
      errors.push('Closing price must be greater than zero');
    }

    if (!request.closeDate) {
      errors.push('Close date is required');
    }

    return errors;
  },

  /**
   * Calculates P&L for a position
   */
  calculatePnL: (contract: PaperContractDto, currentPrice?: number): {
    unrealizedPnL: number;
    percentageReturn: number;
  } => {
    if (!currentPrice) {
      currentPrice = contract.currentPrice || contract.entryPrice;
    }

    const priceChange = currentPrice - contract.entryPrice;
    const multiplier = contract.position === 'Long' ? 1 : -1;
    const unrealizedPnL = priceChange * multiplier * contract.quantity;
    const percentageReturn = (priceChange / contract.entryPrice) * 100 * multiplier;

    return {
      unrealizedPnL,
      percentageReturn
    };
  },

  /**
   * Filters positions by various criteria
   */
  filterPositions: (positions: PaperContractListDto[], filters: {
    status?: string;
    productType?: string;
    position?: string;
    contractMonth?: string;
    minPnL?: number;
    maxPnL?: number;
  }): PaperContractListDto[] => {
    return positions.filter(position => {
      if (filters.status && position.status !== filters.status) return false;
      if (filters.productType && position.productType !== filters.productType) return false;
      if (filters.position && position.position !== filters.position) return false;
      if (filters.contractMonth && position.contractMonth !== filters.contractMonth) return false;
      if (filters.minPnL !== undefined && (position.unrealizedPnL || 0) < filters.minPnL) return false;
      if (filters.maxPnL !== undefined && (position.unrealizedPnL || 0) > filters.maxPnL) return false;
      return true;
    });
  },

  /**
   * Groups positions by product type
   */
  groupByProductType: (positions: PaperContractListDto[]): { [productType: string]: PaperContractListDto[] } => {
    return positions.reduce((groups, position) => {
      const productType = position.productType;
      if (!groups[productType]) {
        groups[productType] = [];
      }
      groups[productType].push(position);
      return groups;
    }, {} as { [productType: string]: PaperContractListDto[] });
  },

  /**
   * Calculates total exposure by position
   */
  calculateTotalExposure: (positions: PaperContractListDto[]): {
    longExposure: number;
    shortExposure: number;
    netExposure: number;
    totalPnL: number;
  } => {
    let longExposure = 0;
    let shortExposure = 0;
    let totalPnL = 0;

    positions.forEach(position => {
      const exposure = position.quantity * position.entryPrice;
      if (position.position === 'Long') {
        longExposure += exposure;
      } else {
        shortExposure += exposure;
      }
      totalPnL += position.unrealizedPnL || 0;
    });

    return {
      longExposure,
      shortExposure,
      netExposure: longExposure - shortExposure,
      totalPnL
    };
  }
};

export default paperContractsApi;