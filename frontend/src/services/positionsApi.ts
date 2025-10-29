import axios from 'axios';
import {
  NetPosition,
  PositionSummary,
  PositionFilters,
  PositionAnalytics,
  ProductType,
  PositionType
} from '@/types/positions';

// Position controller uses non-versioned /api routes
const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Dashboard API instance
const dashboardApi = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

console.log('PositionsAPI baseURL:', API_BASE_URL);

// Positions API endpoints
export const positionsApi = {
  // Get current positions
  getCurrentPositions: async (): Promise<NetPosition[]> => {
    const response = await api.get('/position/current');
    const data = response.data;
    
    // Ensure we have an array to work with
    if (!Array.isArray(data)) {
      throw new Error('Invalid response format: expected array of positions');
    }
    
    return data.map((position: any) => ({
      id: position.id,
      productType: position.productType,
      deliveryMonth: position.deliveryMonth,
      netQuantity: position.netQuantity,
      longQuantity: position.longQuantity,
      shortQuantity: position.shortQuantity,
      unit: position.unit,
      averagePrice: position.averagePrice,
      currentPrice: position.currentPrice,
      unrealizedPnL: position.unrealizedPnL,
      realizedPnL: position.realizedPnL,
      totalPnL: position.totalPnL,
      positionValue: position.positionValue,
      positionType: position.positionType,
      currency: position.currency,
      lastUpdated: position.lastUpdated,
      riskMetrics: position.riskMetrics
    }));
  },

  // Get position summary
  getPositionSummary: async (): Promise<PositionSummary> => {
    const response = await dashboardApi.get('/dashboard/overview');
    const data = response.data;
    
    return {
      totalPositions: data.totalPositions,
      netExposure: data.netExposure,
      longExposure: data.longPositions,
      shortExposure: data.shortPositions,
      totalPnL: data.dailyPnL + data.unrealizedPnL,
      unrealizedPnL: data.unrealizedPnL,
      realizedPnL: data.dailyPnL,
      totalValue: data.totalExposure,
      riskMetrics: {
        portfolioVaR95: data.vaR95,
        portfolioVaR99: data.vaR99,
        portfolioVolatility: data.portfolioVolatility,
      },
      lastUpdated: data.calculatedAt
    };
  },

  // Get position analytics
  getPositionAnalytics: async (): Promise<PositionAnalytics> => {
    const positions = await positionsApi.getCurrentPositions();
    return generateAnalyticsFromPositions(positions);
  },

  // Filter positions
  filterPositions: (positions: NetPosition[], filters: PositionFilters): NetPosition[] => {
    return positions.filter(position => {
      if (filters.productType !== undefined && position.productType !== filters.productType) {
        return false;
      }
      if (filters.deliveryMonth && position.deliveryMonth !== filters.deliveryMonth) {
        return false;
      }
      if (filters.positionType !== undefined && position.positionType !== filters.positionType) {
        return false;
      }
      if (filters.minQuantity !== undefined && Math.abs(position.netQuantity) < filters.minQuantity) {
        return false;
      }
      if (filters.maxQuantity !== undefined && Math.abs(position.netQuantity) > filters.maxQuantity) {
        return false;
      }
      if (!filters.showFlatPositions && position.positionType === PositionType.Flat) {
        return false;
      }
      return true;
    });
  }
};


function generateAnalyticsFromPositions(positions: NetPosition[]): PositionAnalytics {
  const productMap = new Map<string, { quantity: number; exposure: number; pnl: number }>();
  const monthMap = new Map<string, { quantity: number; exposure: number; pnl: number; contracts: number }>();

  positions.forEach(pos => {
    const productName = ProductType[pos.productType];
    
    // Product breakdown
    const existing = productMap.get(productName) || { quantity: 0, exposure: 0, pnl: 0 };
    productMap.set(productName, {
      quantity: existing.quantity + pos.netQuantity,
      exposure: existing.exposure + pos.positionValue,
      pnl: existing.pnl + pos.totalPnL
    });

    // Monthly breakdown
    const monthExisting = monthMap.get(pos.deliveryMonth) || { quantity: 0, exposure: 0, pnl: 0, contracts: 0 };
    monthMap.set(pos.deliveryMonth, {
      quantity: monthExisting.quantity + pos.netQuantity,
      exposure: monthExisting.exposure + pos.positionValue,
      pnl: monthExisting.pnl + pos.totalPnL,
      contracts: monthExisting.contracts + 1
    });
  });

  const totalExposure = Array.from(productMap.values()).reduce((sum, item) => sum + Math.abs(item.exposure), 0);

  return {
    productBreakdown: Array.from(productMap.entries()).map(([product, data]) => ({
      productType: product,
      netQuantity: data.quantity,
      exposure: data.exposure,
      pnl: data.pnl,
      percentage: totalExposure > 0 ? (Math.abs(data.exposure) / totalExposure) * 100 : 0
    })),
    monthlyBreakdown: Array.from(monthMap.entries()).map(([month, data]) => ({
      deliveryMonth: month,
      netQuantity: data.quantity,
      exposure: data.exposure,
      pnl: data.pnl,
      contracts: data.contracts
    })),
    topPositions: positions
      .sort((a, b) => Math.abs(b.positionValue) - Math.abs(a.positionValue))
      .slice(0, 10),
    riskConcentration: Array.from(productMap.entries()).map(([product, data]) => ({
      category: product,
      exposure: Math.abs(data.exposure),
      riskContribution: totalExposure > 0 ? (Math.abs(data.exposure) / totalExposure) * 100 : 0
    }))
  };
}

export default positionsApi;