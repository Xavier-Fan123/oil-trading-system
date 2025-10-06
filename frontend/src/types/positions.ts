export enum PositionType {
  Long = 0,
  Short = 1,
  Flat = 2
}

export enum ProductType {
  Brent = 0,
  WTI = 1,
  Dubai = 2,
  MGO = 3,
  Gasoil = 4,
  Gasoline = 5,
  JetFuel = 6,
  Naphtha = 7
}

export interface NetPosition {
  id: string;
  productType: ProductType;
  deliveryMonth: string;
  netQuantity: number;
  longQuantity: number;
  shortQuantity: number;
  unit: string;
  averagePrice: number;
  currentPrice: number;
  unrealizedPnL: number;
  realizedPnL: number;
  totalPnL: number;
  positionValue: number;
  positionType: PositionType;
  currency: string;
  lastUpdated: string;
  riskMetrics?: {
    var95: number;
    var99: number;
    volatility: number;
    beta: number;
  };
}

export interface PositionSummary {
  totalPositions: number;
  netExposure: number;
  longExposure: number;
  shortExposure: number;
  totalPnL: number;
  unrealizedPnL: number;
  realizedPnL: number;
  totalValue: number;
  riskMetrics: {
    portfolioVaR95: number;
    portfolioVaR99: number;
    portfolioVolatility: number;
  };
  lastUpdated: string;
}

export interface PositionFilters {
  productType?: ProductType;
  deliveryMonth?: string;
  positionType?: PositionType;
  minQuantity?: number;
  maxQuantity?: number;
  showFlatPositions?: boolean;
}

export interface PositionAnalytics {
  productBreakdown: Array<{
    productType: string;
    netQuantity: number;
    exposure: number;
    pnl: number;
    percentage: number;
  }>;
  monthlyBreakdown: Array<{
    deliveryMonth: string;
    netQuantity: number;
    exposure: number;
    pnl: number;
    contracts: number;
  }>;
  topPositions: NetPosition[];
  riskConcentration: Array<{
    category: string;
    exposure: number;
    riskContribution: number;
  }>;
}