import { useQuery } from '@tanstack/react-query'
import type {
  DashboardOverview,
  TradingMetrics,
  PerformanceAnalytics,
  MarketInsights,
  OperationalStatus
} from '@/types'

// Mock data for testing frontend components
const mockOverviewData: DashboardOverview = {
  totalPosition: 158.9,
  totalPositionCurrency: 'USD',
  dailyPnL: 125.3,
  dailyPnLCurrency: 'USD',
  var95: 2.1,
  var95Currency: 'USD',
  unrealizedPnL: 89.7,
  unrealizedPnLCurrency: 'USD',
  realizationRatio: 78.5,
  activeContracts: 24,
  pendingShipments: 8,
  lastUpdated: new Date().toISOString()
}

const mockTradingMetrics: TradingMetrics = {
  totalVolume: 125000,
  volumeUnit: 'MT',
  tradingFrequency: 28,
  avgDealSize: 4200,
  avgDealSizeCurrency: 'USD',
  productDistribution: [
    { productType: 'Brent', volumePercentage: 35.5, pnlContribution: 285 },
    { productType: 'WTI', volumePercentage: 25.2, pnlContribution: 180 },
    { productType: 'MGO', volumePercentage: 18.8, pnlContribution: 125 },
    { productType: 'Gasoil', volumePercentage: 12.3, pnlContribution: 95 },
    { productType: 'Fuel Oil', volumePercentage: 8.2, pnlContribution: 65 }
  ],
  counterpartyConcentration: [
    { counterpartyName: 'Shell Trading', exposurePercentage: 22.5, creditRating: 'AA-' },
    { counterpartyName: 'BP Oil', exposurePercentage: 18.3, creditRating: 'A+' },
    { counterpartyName: 'Total Energy', exposurePercentage: 15.7, creditRating: 'A' },
    { counterpartyName: 'Exxon Mobil', exposurePercentage: 12.9, creditRating: 'AA' },
    { counterpartyName: 'Other Partners', exposurePercentage: 30.6, creditRating: 'A-' }
  ],
  lastUpdated: new Date().toISOString()
}

const mockPerformanceAnalytics: PerformanceAnalytics = {
  monthlyPnL: [
    { month: '2024-07', pnl: 450, cumulativePnL: 450 },
    { month: '2024-08', pnl: -180, cumulativePnL: 270 },
    { month: '2024-09', pnl: 320, cumulativePnL: 590 },
    { month: '2024-10', pnl: 275, cumulativePnL: 865 },
    { month: '2024-11', pnl: -95, cumulativePnL: 770 },
    { month: '2024-12', pnl: 185, cumulativePnL: 955 },
    { month: '2025-01', pnl: 125, cumulativePnL: 1080 }
  ],
  sharpeRatio: 1.85,
  maxDrawdown: -12.3,
  winRate: 68.5,
  avgWinSize: 245,
  avgLossSize: -135,
  volatility: 18.7,
  lastUpdated: new Date().toISOString()
}

const mockMarketInsights: MarketInsights = {
  benchmarkPrices: [
    { benchmark: 'Brent', currentPrice: 82.45, change24h: 1.25, changePercent24h: 1.54, currency: 'USD' },
    { benchmark: 'WTI', currentPrice: 78.92, change24h: 0.87, changePercent24h: 1.11, currency: 'USD' },
    { benchmark: 'Dubai', currentPrice: 81.33, change24h: -0.45, changePercent24h: -0.55, currency: 'USD' },
    { benchmark: 'Urals', currentPrice: 76.18, change24h: 0.32, changePercent24h: 0.42, currency: 'USD' }
  ],
  volatility: [
    { product: 'Brent', impliedVolatility: 24.5, historicalVolatility: 22.8, volatilityTrend: 'Rising' },
    { product: 'WTI', impliedVolatility: 26.2, historicalVolatility: 24.1, volatilityTrend: 'Stable' },
    { product: 'MGO', impliedVolatility: 18.7, historicalVolatility: 19.3, volatilityTrend: 'Falling' }
  ],
  correlations: [
    { product1: 'Brent', product2: 'WTI', correlation: 0.87, trend: 'Stable' },
    { product1: 'Brent', product2: 'MGO', correlation: 0.65, trend: 'Rising' },
    { product1: 'WTI', product2: 'Gasoil', correlation: 0.73, trend: 'Falling' }
  ],
  marketSentiment: 'Bullish',
  riskFactors: [
    'Geopolitical tensions in Middle East',
    'OPEC+ production cuts',
    'Global economic slowdown concerns',
    'USD strength impact on commodities'
  ],
  lastUpdated: new Date().toISOString()
}

const mockOperationalStatus: OperationalStatus = {
  activeContracts: 24,
  pendingContracts: 8,
  completedContractsThisMonth: 15,
  shipmentStatus: [
    { 
      shipmentId: 'SH001', 
      status: 'in_transit', 
      vessel: 'Nordic Star', 
      origin: 'Rotterdam', 
      destination: 'Singapore', 
      eta: new Date(Date.now() + 5 * 24 * 60 * 60 * 1000).toISOString(),
      quantity: 50000, 
      unit: 'MT' 
    },
    { 
      shipmentId: 'SH002', 
      status: 'loading', 
      vessel: 'Pacific Glory', 
      origin: 'Houston', 
      destination: 'Tokyo', 
      eta: new Date(Date.now() + 12 * 24 * 60 * 60 * 1000).toISOString(),
      quantity: 75000, 
      unit: 'MT' 
    },
    { 
      shipmentId: 'SH003', 
      status: 'completed', 
      vessel: 'Atlantic Pearl', 
      origin: 'Fujairah', 
      destination: 'Mumbai', 
      eta: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      quantity: 45000, 
      unit: 'MT' 
    }
  ],
  riskAlerts: [
    { 
      alertType: 'Credit Risk', 
      severity: 'High', 
      message: 'Counterparty exposure exceeds 25% limit', 
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString()
    },
    { 
      alertType: 'Market Risk', 
      severity: 'Medium', 
      message: 'Oil price volatility increased 15%', 
      timestamp: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString()
    },
    { 
      alertType: 'Operational', 
      severity: 'Low', 
      message: 'Delayed shipment notification', 
      timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString()
    }
  ],
  upcomingDeliveries: [
    { 
      contractNumber: 'PC-2025-001', 
      counterparty: 'Shell Trading', 
      deliveryDate: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
      quantity: 25000, 
      unit: 'MT', 
      product: 'Brent', 
      status: 'Confirmed' 
    },
    { 
      contractNumber: 'PC-2025-002', 
      counterparty: 'BP Oil', 
      deliveryDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
      quantity: 35000, 
      unit: 'MT', 
      product: 'WTI', 
      status: 'Pending' 
    },
    { 
      contractNumber: 'SC-2025-003', 
      counterparty: 'Total Energy', 
      deliveryDate: new Date(Date.now() + 10 * 24 * 60 * 60 * 1000).toISOString(),
      quantity: 20000, 
      unit: 'MT', 
      product: 'MGO', 
      status: 'Confirmed' 
    }
  ],
  lastUpdated: new Date().toISOString()
}

// Mock async request
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

// Mock hooks that simulate API calls
export const useMockDashboardOverview = () => {
  return useQuery({
    queryKey: ['dashboard', 'overview', 'mock'],
    queryFn: async () => {
      await delay(500) // Mock network delay
      return mockOverviewData
    },
    refetchInterval: 15000,
    staleTime: 10000,
  })
}

export const useMockTradingMetrics = () => {
  return useQuery({
    queryKey: ['dashboard', 'trading-metrics', 'mock'],
    queryFn: async () => {
      await delay(600)
      return mockTradingMetrics
    },
    refetchInterval: 30000,
    staleTime: 20000,
  })
}

export const useMockPerformanceAnalytics = () => {
  return useQuery({
    queryKey: ['dashboard', 'performance-analytics', 'mock'],
    queryFn: async () => {
      await delay(700)
      return mockPerformanceAnalytics
    },
    refetchInterval: 60000,
    staleTime: 45000,
  })
}

export const useMockMarketInsights = () => {
  return useQuery({
    queryKey: ['dashboard', 'market-insights', 'mock'],
    queryFn: async () => {
      await delay(550)
      return mockMarketInsights
    },
    refetchInterval: 20000,
    staleTime: 15000,
  })
}

export const useMockOperationalStatus = () => {
  return useQuery({
    queryKey: ['dashboard', 'operational-status', 'mock'],
    queryFn: async () => {
      await delay(650)
      return mockOperationalStatus
    },
    refetchInterval: 15000,
    staleTime: 10000,
  })
}