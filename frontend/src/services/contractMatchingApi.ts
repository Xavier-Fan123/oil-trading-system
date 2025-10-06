import axios from 'axios';
import { StandardApiError, ApiResult } from '@/types';
import { normalizeError, logError } from '@/utils/errorUtils';

const API_BASE_URL = 'http://localhost:5000/api'; // Force correct baseURL

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// TypeScript interfaces for Contract Matching
export interface AvailablePurchase {
  id: string;
  contractNumber: string;
  contractQuantity: number;
  matchedQuantity: number;
  availableQuantity: number;
  productName: string;
  tradingPartnerName: string;
}

export interface UnmatchedSales {
  id: string;
  contractNumber: string;
  contractQuantity: number;
  productName: string;
  tradingPartnerName: string;
}

export interface CreateMatchingRequest {
  purchaseContractId: string;
  salesContractId: string;
  quantity: number;
  notes?: string;
  matchedBy?: string;
}

export interface PurchaseMatching {
  id: string;
  matchedQuantity: number;
  matchedDate: string;
  notes?: string;
  salesContractNumber: string;
  salesContractQuantity: number;
  salesTradingPartner: string;
}

export interface EnhancedNetPosition {
  productId: string;
  productName: string;
  productType: number;
  totalPurchased: number;
  totalSold: number;
  totalMatched: number;
  netPosition: number;
  naturalHedge: number;
  netExposure: number;
  hedgeRatio: number;
}

export interface ContractMatchingResponse {
  message: string;
  matchingId?: string;
  matchedQuantity?: number;
  remainingAvailable?: number;
}

// Contract Matching API service
export const contractMatchingApi = {
  // GET /api/contract-matching/available-purchases
  // Returns purchase contracts with available quantities
  getAvailablePurchases: async (): Promise<ApiResult<AvailablePurchase[]>> => {
    try {
      const response = await api.get('/contract-matching/available-purchases');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      const normalizedError = normalizeError(error);
      logError(normalizedError, {
        timestamp: new Date().toISOString(),
        url: window.location.href,
        userAgent: navigator.userAgent,
        action: 'getAvailablePurchases',
        component: 'contractMatchingApi'
      });
      
      return {
        success: false,
        error: normalizedError
      };
    }
  },

  // GET /api/contract-matching/unmatched-sales
  // Returns sales contracts not yet matched
  getUnmatchedSales: async (): Promise<ApiResult<UnmatchedSales[]>> => {
    try {
      const response = await api.get('/contract-matching/unmatched-sales');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      const normalizedError = normalizeError(error);
      logError(normalizedError, {
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        url: window.location.href,
        action: 'getUnmatchedSales',
        component: 'contractMatchingApi'
      });
      
      return {
        success: false,
        error: normalizedError
      };
    }
  },

  // POST /api/contract-matching/match
  // Creates matching relationships between contracts
  createMatching: async (request: CreateMatchingRequest): Promise<ApiResult<ContractMatchingResponse>> => {
    try {
      // Client-side validation
      const validationErrors = contractMatchingApi.validateMatchingRequest(request);
      if (validationErrors.length > 0) {
        const validationError: StandardApiError = {
          code: 'VALIDATION_FAILED',
          message: 'Request validation failed',
          timestamp: new Date().toISOString(),
          traceId: `client-${Date.now()}`,
          statusCode: 400,
          validationErrors: {
            'request': validationErrors
          }
        };
        
        return {
          success: false,
          error: validationError
        };
      }

      const response = await api.post('/contract-matching/match', request);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      const normalizedError = normalizeError(error);
      logError(normalizedError, {
        timestamp: new Date().toISOString(),
        url: window.location.href,
        userAgent: navigator.userAgent,
        action: 'createMatching',
        component: 'contractMatchingApi',
        additionalData: { request }
      });
      
      return {
        success: false,
        error: normalizedError
      };
    }
  },

  // GET /api/contract-matching/purchase/{id}
  // Gets matching history for a purchase contract
  getPurchaseMatchings: async (purchaseId: string): Promise<PurchaseMatching[]> => {
    try {
      const response = await api.get(`/contract-matching/purchase/${purchaseId}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching purchase matchings:', error);
      throw error;
    }
  },

  // GET /api/contract-matching/enhanced-net-position
  // Gets enhanced position calculation with natural hedging
  getEnhancedNetPosition: async (): Promise<ApiResult<EnhancedNetPosition[]>> => {
    try {
      const response = await api.get('/contract-matching/enhanced-net-position');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      const normalizedError = normalizeError(error);
      logError(normalizedError, {
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        url: window.location.href,
        action: 'getEnhancedNetPosition',
        component: 'contractMatchingApi'
      });
      
      return {
        success: false,
        error: normalizedError
      };
    }
  },

  // Utility method to validate matching request before sending
  validateMatchingRequest: (request: CreateMatchingRequest): string[] => {
    const errors: string[] = [];

    if (!request.purchaseContractId || request.purchaseContractId.trim() === '') {
      errors.push('Purchase contract ID is required');
    }

    if (!request.salesContractId || request.salesContractId.trim() === '') {
      errors.push('Sales contract ID is required');
    }

    if (!request.quantity || request.quantity <= 0) {
      errors.push('Quantity must be greater than zero');
    }

    // Validate GUID format (basic check)
    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (request.purchaseContractId && !guidRegex.test(request.purchaseContractId)) {
      errors.push('Invalid purchase contract ID format');
    }

    if (request.salesContractId && !guidRegex.test(request.salesContractId)) {
      errors.push('Invalid sales contract ID format');
    }

    return errors;
  },

  // Utility method to calculate matching statistics
  calculateMatchingStatistics: (purchases: AvailablePurchase[], sales: UnmatchedSales[]): {
    totalAvailablePurchaseQuantity: number;
    totalUnmatchedSalesQuantity: number;
    purchaseContractCount: number;
    unmatchedSalesCount: number;
    productBreakdown: { [productName: string]: { purchases: number; sales: number } };
  } => {
    const stats = {
      totalAvailablePurchaseQuantity: 0,
      totalUnmatchedSalesQuantity: 0,
      purchaseContractCount: purchases.length,
      unmatchedSalesCount: sales.length,
      productBreakdown: {} as { [productName: string]: { purchases: number; sales: number } }
    };

    // Calculate purchase statistics
    purchases.forEach(purchase => {
      stats.totalAvailablePurchaseQuantity += purchase.availableQuantity;
      
      if (!stats.productBreakdown[purchase.productName]) {
        stats.productBreakdown[purchase.productName] = { purchases: 0, sales: 0 };
      }
      stats.productBreakdown[purchase.productName].purchases += purchase.availableQuantity;
    });

    // Calculate sales statistics
    sales.forEach(sale => {
      stats.totalUnmatchedSalesQuantity += sale.contractQuantity;
      
      if (!stats.productBreakdown[sale.productName]) {
        stats.productBreakdown[sale.productName] = { purchases: 0, sales: 0 };
      }
      stats.productBreakdown[sale.productName].sales += sale.contractQuantity;
    });

    return stats;
  },

  // Utility method to find potential matches between purchases and sales
  findPotentialMatches: (purchases: AvailablePurchase[], sales: UnmatchedSales[]): {
    purchaseId: string;
    salesId: string;
    productName: string;
    maxQuantity: number;
    purchaseContract: string;
    salesContract: string;
  }[] => {
    const potentialMatches: {
      purchaseId: string;
      salesId: string;
      productName: string;
      maxQuantity: number;
      purchaseContract: string;
      salesContract: string;
    }[] = [];

    purchases.forEach(purchase => {
      sales.forEach(sale => {
        // Only match if same product
        if (purchase.productName === sale.productName && purchase.availableQuantity > 0) {
          potentialMatches.push({
            purchaseId: purchase.id,
            salesId: sale.id,
            productName: purchase.productName,
            maxQuantity: Math.min(purchase.availableQuantity, sale.contractQuantity),
            purchaseContract: purchase.contractNumber,
            salesContract: sale.contractNumber
          });
        }
      });
    });

    // Sort by potential quantity (descending)
    return potentialMatches.sort((a, b) => b.maxQuantity - a.maxQuantity);
  }
};

export default contractMatchingApi;