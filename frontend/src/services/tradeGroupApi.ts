import {
  TradeGroupDto,
  TradeGroupDetailsDto,
  CreateTradeGroupDto,
  UpdateTradeGroupDto,
  AssignContractToTradeGroupDto,
  PortfolioRiskWithTradeGroupsDto,
  TradeGroupTagDto,
  StrategyType
} from '../types/tradeGroups';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

// TradeGroup Management API
export const tradeGroupApi = {
  // Create trade group
  createTradeGroup: async (dto: CreateTradeGroupDto): Promise<string> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(dto),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to create trade group');
    }
    return response.json(); // Returns the trade group ID
  },

  // Get all trade groups
  getAllTradeGroups: async (): Promise<TradeGroupDto[]> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups`);
    if (!response.ok) {
      throw new Error('Failed to fetch trade groups');
    }
    return response.json();
  },

  // Get trade group details by ID
  getTradeGroup: async (id: string): Promise<TradeGroupDetailsDto> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${id}`);
    if (!response.ok) {
      throw new Error('Failed to fetch trade group details');
    }
    return response.json();
  },

  // Update trade group
  updateTradeGroup: async (id: string, dto: UpdateTradeGroupDto): Promise<TradeGroupDto> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(dto),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to update trade group');
    }
    return response.json();
  },

  // Close trade group
  closeTradeGroup: async (id: string): Promise<boolean> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${id}/close`, {
      method: 'POST',
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to close trade group');
    }
    return response.json();
  },

  // Assign contract to trade group
  assignContractToTradeGroup: async (
    tradeGroupId: string, 
    dto: AssignContractToTradeGroupDto
  ): Promise<boolean> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${tradeGroupId}/contracts`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(dto),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to assign contract to trade group');
    }
    return response.json();
  },

  // Get trade group risk metrics
  getTradeGroupRisk: async (id: string): Promise<any> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${id}/risk`);
    if (!response.ok) {
      throw new Error('Failed to fetch trade group risk metrics');
    }
    return response.json();
  },

  // Get portfolio risk summary with trade groups
  getPortfolioRiskWithTradeGroups: async (): Promise<PortfolioRiskWithTradeGroupsDto> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/portfolio-risk`);
    if (!response.ok) {
      throw new Error('Failed to fetch portfolio risk with trade groups');
    }
    return response.json();
  },

  // Add tag to trade group
  addTagToTradeGroup: async (tradeGroupId: string, tagId: string, notes?: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${tradeGroupId}/tags`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ tagId, notes }),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to add tag to trade group');
    }
  },

  // Remove tag from trade group
  removeTagFromTradeGroup: async (tradeGroupId: string, tagId: string, reason?: string): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${tradeGroupId}/tags/${tagId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ reason }),
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Failed to remove tag from trade group');
    }
  },

  // Get all tags for trade group
  getTradeGroupTags: async (tradeGroupId: string): Promise<TradeGroupTagDto[]> => {
    const response = await fetch(`${API_BASE_URL}/trade-groups/${tradeGroupId}/tags`);
    if (!response.ok) {
      throw new Error('Failed to fetch trade group tags');
    }
    return response.json();
  },
};

// TradeGroup utility functions
export const tradeGroupUtils = {
  // Format currency
  formatCurrency: (amount: number, currency: string = 'USD'): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  },

  // Format quantity
  formatQuantity: (quantity: number): string => {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(quantity);
  },

  // Format percentage
  formatPercentage: (value: number): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'percent',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value / 100);
  },

  // Calculate P&L color
  getPnLColor: (pnl: number): string => {
    if (pnl > 0) return '#059669'; // Green
    if (pnl < 0) return '#DC2626'; // Red
    return '#6B7280'; // Gray
  },

  // Get risk level color
  getRiskLevelColor: (riskLevel: string): string => {
    switch (riskLevel?.toLowerCase()) {
      case 'low': return '#059669'; // Green
      case 'medium': return '#D97706'; // Yellow
      case 'high': return '#DC2626'; // Red
      case 'veryhigh': return '#7C2D12'; // Dark Red
      default: return '#6B7280'; // Gray
    }
  },

  // Calculate VaR level
  getVaRLevel: (var95: number, totalValue: number): 'Low' | 'Medium' | 'High' | 'Critical' => {
    const varPercentage = Math.abs(var95) / totalValue * 100;
    if (varPercentage < 1) return 'Low';
    if (varPercentage < 3) return 'Medium';
    if (varPercentage < 5) return 'High';
    return 'Critical';
  },

  // Check if risk warning is needed
  needsRiskWarning: (tradeGroup: TradeGroupDetailsDto): boolean => {
    const varLevel = tradeGroupUtils.getVaRLevel(tradeGroup.riskMetrics.portfolioVaR95, tradeGroup.totalValue);
    return varLevel === 'High' || varLevel === 'Critical' || 
           (typeof tradeGroup.maxAllowedLoss === 'number' && tradeGroup.netPnL < -Math.abs(tradeGroup.maxAllowedLoss));
  },

  // Get strategy type icon
  getStrategyIcon: (strategyType: StrategyType | string | number): string => {
    // Convert enum number to string if needed
    let strategyString: string;
    if (typeof strategyType === 'number') {
      strategyString = StrategyType[strategyType] || '';
    } else {
      strategyString = String(strategyType || '');
    }
    
    switch (strategyString.toLowerCase()) {
      case 'directional': return '📈';
      case 'calendarspread': return '📅';
      case 'intercommodityspread': return '🔄';
      case 'locationspread': return '🌍';
      case 'basishedge': return '🛡️';
      case 'crosshedge': return '⚖️';
      case 'averagepricecontract': return '📊';
      case 'arbitrage': return '⚡';
      case 'crackspread': return '🔥';
      default: return '📋';
    }
  },

  // Calculate portfolio diversification score
  calculateDiversificationScore: (tradeGroups: TradeGroupDto[]): number => {
    if (tradeGroups.length === 0) return 0;
    
    const strategyTypes = new Set(tradeGroups.map(tg => tg.strategyType));
    const uniqueStrategies = strategyTypes.size;
    const totalGroups = tradeGroups.length;
    
    // Base diversification score: Strategy type diversity
    const strategyDiversity = uniqueStrategies / Math.min(totalGroups, 9); // Maximum 9 strategy types
    
    // Consider risk distribution
    const lowRiskCount = tradeGroups.filter(tg => tg.expectedRiskLevel === 1).length;
    const mediumRiskCount = tradeGroups.filter(tg => tg.expectedRiskLevel === 2).length;
    const highRiskCount = tradeGroups.filter(tg => tg.expectedRiskLevel === 3).length;
    
    const riskDistribution = 1 - Math.max(lowRiskCount, mediumRiskCount, highRiskCount) / totalGroups;
    
    return Math.min(100, (strategyDiversity * 0.7 + riskDistribution * 0.3) * 100);
  }
};