// TradeGroup related types for frontend

export enum StrategyType {
  Directional = 1,
  CalendarSpread = 2,
  IntercommoditySpread = 3,
  LocationSpread = 4,
  BasisHedge = 5,
  CrossHedge = 6,
  AveragePriceContract = 7,
  Arbitrage = 8,
  CrackSpread = 9,
  Custom = 99
}

export enum TradeGroupStatus {
  Active = 1,
  Closed = 2,
  Suspended = 3
}

export enum RiskLevel {
  Low = 1,
  Medium = 2,
  High = 3,
  VeryHigh = 4
}

export interface TradeGroupDto {
  id: string;
  groupName: string;
  strategyType: StrategyType;
  description?: string;
  status: TradeGroupStatus;
  expectedRiskLevel?: RiskLevel;
  maxAllowedLoss?: number;
  targetProfit?: number;
  createdAt: string;
  createdBy: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface TradeGroupDetailsDto extends TradeGroupDto {
  paperContracts: PaperContractSummary[];
  purchaseContracts: PurchaseContractSummary[];
  salesContracts: SalesContractSummary[];
  associatedTags: TradeGroupTagDto[];
  netPnL: number;
  totalValue: number;
  contractCount: number;
  riskMetrics: TradeGroupRiskMetrics;
}

export interface CreateTradeGroupDto {
  groupName: string;
  strategyType: string; // Will be converted to enum on backend
  description?: string;
  expectedRiskLevel?: string; // Will be converted to enum on backend
  maxAllowedLoss?: number;
  targetProfit?: number;
}

export interface UpdateTradeGroupDto {
  groupName?: string;
  description?: string;
  expectedRiskLevel?: string;
  maxAllowedLoss?: number;
  targetProfit?: number;
}

export interface AssignContractToTradeGroupDto {
  contractId: string;
  contractType: string; // "PaperContract", "PurchaseContract", "SalesContract"
}

export interface TradeGroupTagDto {
  id: string;
  tradeGroupId: string;
  tagId: string;
  tagName: string;
  tagCategory: string;
  tagColor: string;
  assignedAt: string;
  assignedBy: string;
  isActive: boolean;
  notes?: string;
}

export interface TradeGroupRiskMetrics {
  portfolioVaR95: number;
  portfolioVaR99: number;
  concentrationRisk: number;
  correlationRisk: number;
  leverageRatio: number;
  sharpeRatio: number;
  maxDrawdown: number;
  volatility: number;
  beta: number;
}

// Contract summary interfaces for TradeGroup details
export interface PaperContractSummary {
  id: string;
  contractMonth: string;
  productType: string;
  position: string; // "Long" or "Short"
  quantity: number;
  entryPrice: number;
  currentPrice?: number;
  unrealizedPnL: number;
  status: string;
}

export interface PurchaseContractSummary {
  id: string;
  contractNumber: string;
  supplierName: string;
  productName: string;
  quantity: number;
  status: string;
  laycanStart: string;
  laycanEnd: string;
}

export interface SalesContractSummary {
  id: string;
  contractNumber: string;
  customerName: string;
  productName: string;
  quantity: number;
  status: string;
  laycanStart: string;
  laycanEnd: string;
}

// Portfolio risk summary with trade groups
export interface PortfolioRiskWithTradeGroupsDto {
  totalTradeGroups: number;
  activeTradeGroups: number;
  totalValue: number;
  totalPnL: number;
  portfolioVaR95: number;
  portfolioVaR99: number;
  tradeGroupSummaries: TradeGroupSummaryDto[];
  riskConcentration: RiskConcentrationDto[];
  correlationMatrix: CorrelationMatrixDto[];
}

export interface TradeGroupSummaryDto {
  id: string;
  groupName: string;
  strategyType: StrategyType;
  status: TradeGroupStatus;
  contractCount: number;
  netPnL: number;
  totalValue: number;
  riskLevel: RiskLevel;
  var95: number;
  lastUpdated: string;
}

export interface RiskConcentrationDto {
  category: string;
  exposure: number;
  percentage: number;
  riskContribution: number;
}

export interface CorrelationMatrixDto {
  tradeGroup1Id: string;
  tradeGroup1Name: string;
  tradeGroup2Id: string;
  tradeGroup2Name: string;
  correlation: number;
}

// Helper interfaces for UI
export interface TradeGroupFilters {
  strategyType?: StrategyType;
  status?: TradeGroupStatus;
  riskLevel?: RiskLevel;
  minPnL?: number;
  maxPnL?: number;
  createdBy?: string;
  fromDate?: Date;
  toDate?: Date;
}

export interface StrategyTypeInfo {
  value: StrategyType;
  label: string;
  description: string;
  riskLevel: RiskLevel;
  isSpread: boolean;
  isHedge: boolean;
  color: string;
}

export interface RiskLevelInfo {
  value: RiskLevel;
  label: string;
  color: string;
  description: string;
}

// Strategy type helpers
export const strategyTypeHelpers = {
  getDisplayName: (strategy: StrategyType): string => {
    switch (strategy) {
      case StrategyType.Directional: return 'Directional';
      case StrategyType.CalendarSpread: return 'Calendar Spread';
      case StrategyType.IntercommoditySpread: return 'Intercommodity Spread';
      case StrategyType.LocationSpread: return 'Location Spread';
      case StrategyType.BasisHedge: return 'Basis Hedge';
      case StrategyType.CrossHedge: return 'Cross Hedge';
      case StrategyType.AveragePriceContract: return 'Average Price Contract';
      case StrategyType.Arbitrage: return 'Arbitrage';
      case StrategyType.CrackSpread: return 'Crack Spread';
      case StrategyType.Custom: return 'Custom';
      default: return 'Unknown';
    }
  },

  getDescription: (strategy: StrategyType): string => {
    switch (strategy) {
      case StrategyType.Directional: return 'Single directional position, long or short on specific product';
      case StrategyType.CalendarSpread: return 'Price spread trading between different months of same product';
      case StrategyType.IntercommoditySpread: return 'Price spread trading between related products';
      case StrategyType.LocationSpread: return 'Price spread trading between different locations of same product';
      case StrategyType.BasisHedge: return 'Basis hedging between physical inventory and futures';
      case StrategyType.CrossHedge: return 'Cross hedging between related but different products';
      case StrategyType.AveragePriceContract: return 'Average price contract strategy';
      case StrategyType.Arbitrage: return 'Arbitrage opportunities between different markets';
      case StrategyType.CrackSpread: return 'Crack spread between crude oil and refined products';
      case StrategyType.Custom: return 'Custom trading strategy';
      default: return 'Unknown strategy type';
    }
  },

  getSuggestedRiskLevel: (strategy: StrategyType): RiskLevel => {
    switch (strategy) {
      case StrategyType.Directional: return RiskLevel.High;
      case StrategyType.CalendarSpread: return RiskLevel.Medium;
      case StrategyType.IntercommoditySpread: return RiskLevel.Medium;
      case StrategyType.LocationSpread: return RiskLevel.Medium;
      case StrategyType.BasisHedge: return RiskLevel.Low;
      case StrategyType.CrossHedge: return RiskLevel.Medium;
      case StrategyType.AveragePriceContract: return RiskLevel.Low;
      case StrategyType.Arbitrage: return RiskLevel.Low;
      case StrategyType.CrackSpread: return RiskLevel.Medium;
      case StrategyType.Custom: return RiskLevel.Medium;
      default: return RiskLevel.Medium;
    }
  },

  getColor: (strategy: StrategyType): string => {
    switch (strategy) {
      case StrategyType.Directional: return '#EF4444'; // Red
      case StrategyType.CalendarSpread: return '#8B5CF6'; // Purple
      case StrategyType.IntercommoditySpread: return '#06B6D4'; // Cyan
      case StrategyType.LocationSpread: return '#10B981'; // Emerald
      case StrategyType.BasisHedge: return '#059669'; // Green
      case StrategyType.CrossHedge: return '#0891B2'; // Teal
      case StrategyType.AveragePriceContract: return '#0284C7'; // Blue
      case StrategyType.Arbitrage: return '#DC2626'; // Dark Red
      case StrategyType.CrackSpread: return '#EA580C'; // Orange
      case StrategyType.Custom: return '#6B7280'; // Gray
      default: return '#6B7280';
    }
  },

  isSpreadStrategy: (strategy: StrategyType): boolean => {
    return strategy === StrategyType.CalendarSpread ||
           strategy === StrategyType.IntercommoditySpread ||
           strategy === StrategyType.LocationSpread ||
           strategy === StrategyType.CrackSpread;
  },

  isHedgeStrategy: (strategy: StrategyType): boolean => {
    return strategy === StrategyType.BasisHedge ||
           strategy === StrategyType.CrossHedge;
  },

  getAllStrategies: (): StrategyTypeInfo[] => {
    return [
      {
        value: StrategyType.Directional,
        label: 'Directional',
        description: 'Single directional position, long or short on specific product',
        riskLevel: RiskLevel.High,
        isSpread: false,
        isHedge: false,
        color: '#EF4444'
      },
      {
        value: StrategyType.CalendarSpread,
        label: 'Calendar Spread',
        description: 'Price spread trading between different months of same product',
        riskLevel: RiskLevel.Medium,
        isSpread: true,
        isHedge: false,
        color: '#8B5CF6'
      },
      {
        value: StrategyType.IntercommoditySpread,
        label: 'Intercommodity Spread',
        description: 'Price spread trading between related products',
        riskLevel: RiskLevel.Medium,
        isSpread: true,
        isHedge: false,
        color: '#06B6D4'
      },
      {
        value: StrategyType.LocationSpread,
        label: 'Location Spread',
        description: 'Price spread trading between different locations of same product',
        riskLevel: RiskLevel.Medium,
        isSpread: true,
        isHedge: false,
        color: '#10B981'
      },
      {
        value: StrategyType.BasisHedge,
        label: 'Basis Hedge',
        description: 'Basis hedging between physical inventory and futures',
        riskLevel: RiskLevel.Low,
        isSpread: false,
        isHedge: true,
        color: '#059669'
      },
      {
        value: StrategyType.CrossHedge,
        label: 'Cross Hedge',
        description: 'Cross hedging between related but different products',
        riskLevel: RiskLevel.Medium,
        isSpread: false,
        isHedge: true,
        color: '#0891B2'
      },
      {
        value: StrategyType.AveragePriceContract,
        label: 'Average Price Contract',
        description: 'Average price contract strategy',
        riskLevel: RiskLevel.Low,
        isSpread: false,
        isHedge: false,
        color: '#0284C7'
      },
      {
        value: StrategyType.Arbitrage,
        label: 'Arbitrage',
        description: 'Arbitrage opportunities between different markets',
        riskLevel: RiskLevel.Low,
        isSpread: false,
        isHedge: false,
        color: '#DC2626'
      },
      {
        value: StrategyType.CrackSpread,
        label: 'Crack Spread',
        description: 'Crack spread between crude oil and refined products',
        riskLevel: RiskLevel.Medium,
        isSpread: true,
        isHedge: false,
        color: '#EA580C'
      }
    ];
  }
};

// Risk level helpers
export const riskLevelHelpers = {
  getDisplayName: (level: RiskLevel): string => {
    switch (level) {
      case RiskLevel.Low: return 'Low Risk';
      case RiskLevel.Medium: return 'Medium Risk';
      case RiskLevel.High: return 'High Risk';
      case RiskLevel.VeryHigh: return 'Very High Risk';
      default: return 'Unknown';
    }
  },

  getColor: (level: RiskLevel): string => {
    switch (level) {
      case RiskLevel.Low: return '#059669'; // Green
      case RiskLevel.Medium: return '#D97706'; // Yellow
      case RiskLevel.High: return '#DC2626'; // Red
      case RiskLevel.VeryHigh: return '#7C2D12'; // Dark Red
      default: return '#6B7280';
    }
  },

  getDescription: (level: RiskLevel): string => {
    switch (level) {
      case RiskLevel.Low: return 'Low risk strategy, typically arbitrage or hedging';
      case RiskLevel.Medium: return 'Medium risk strategy, spread trading etc';
      case RiskLevel.High: return 'High risk strategy, directional positions etc';
      case RiskLevel.VeryHigh: return 'Very high risk strategy, requires strict monitoring';
      default: return 'Unknown risk level';
    }
  }
};

// Trade group status helpers
export const tradeGroupStatusHelpers = {
  getDisplayName: (status: TradeGroupStatus): string => {
    switch (status) {
      case TradeGroupStatus.Active: return 'Active';
      case TradeGroupStatus.Closed: return 'Closed';
      case TradeGroupStatus.Suspended: return 'Suspended';
      default: return 'Unknown';
    }
  },

  getColor: (status: TradeGroupStatus): string => {
    switch (status) {
      case TradeGroupStatus.Active: return '#059669'; // Green
      case TradeGroupStatus.Closed: return '#6B7280'; // Gray
      case TradeGroupStatus.Suspended: return '#D97706'; // Yellow
      default: return '#6B7280';
    }
  }
};