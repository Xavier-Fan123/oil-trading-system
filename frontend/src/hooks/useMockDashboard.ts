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
  totalPositions: 42,
  totalExposure: 158900000,
  netExposure: 45200000,
  longPositions: 24,
  shortPositions: 15,
  flatPositions: 3,
  dailyPnL: 125300,
  unrealizedPnL: 89700,
  vaR95: 2100000,
  vaR99: 3500000,
  portfolioVolatility: 18.7,
  activePurchaseContracts: 15,
  activeSalesContracts: 9,
  pendingContracts: 8,
  marketDataPoints: 1250,
  lastMarketUpdate: new Date().toISOString(),
  alertCount: 3,
  calculatedAt: new Date().toISOString()
}

const mockTradingMetrics: TradingMetrics = {
  period: '2025-01',
  totalTrades: 28,
  totalVolume: 125000,
  averageTradeSize: 4464.3,
  purchaseVolume: 75000,
  salesVolume: 50000,
  paperVolume: 0,
  longPaperVolume: 0,
  shortPaperVolume: 0,
  productBreakdown: {
    'Brent': 35.5,
    'WTI': 25.2,
    'MGO': 18.8,
    'Gasoil': 12.3,
    'Fuel Oil': 8.2
  },
  counterpartyBreakdown: {
    'Shell Trading': 22.5,
    'BP Oil': 18.3,
    'Total Energy': 15.7,
    'Exxon Mobil': 12.9,
    'Other Partners': 30.6
  },
  tradeFrequency: 1.4,
  volumeByProduct: {
    'Brent': 44375,
    'WTI': 31500,
    'MGO': 23500,
    'Gasoil': 15375,
    'Fuel Oil': 10250
  },
  calculatedAt: new Date().toISOString()
}

const mockPerformanceAnalytics: PerformanceAnalytics = {
  period: '2025-01',
  totalPnL: 1080000,
  realizedPnL: 955000,
  unrealizedPnL: 125000,
  bestPerformingProduct: 'Brent',
  worstPerformingProduct: 'Fuel Oil',
  totalReturn: 12.5,
  annualizedReturn: 15.8,
  sharpeRatio: 1.85,
  maxDrawdown: -12.3,
  winRate: 68.5,
  profitFactor: 2.1,
  vaRUtilization: 0.62,
  volatilityAdjustedReturn: 0.87,
  dailyPnLHistory: [
    { date: '2025-01-20', dailyPnL: 45000, cumulativePnL: 955000 },
    { date: '2025-01-21', dailyPnL: -18000, cumulativePnL: 937000 },
    { date: '2025-01-22', dailyPnL: 32000, cumulativePnL: 969000 },
    { date: '2025-01-23', dailyPnL: 27500, cumulativePnL: 996500 },
    { date: '2025-01-24', dailyPnL: -9500, cumulativePnL: 987000 },
    { date: '2025-01-27', dailyPnL: 18500, cumulativePnL: 1005500 },
    { date: '2025-01-28', dailyPnL: 12500, cumulativePnL: 1018000 }
  ],
  productPerformance: [
    { product: 'Brent', exposure: 44375000, pnL: 285000, return: 0.64 },
    { product: 'WTI', exposure: 31500000, pnL: 180000, return: 0.57 },
    { product: 'MGO', exposure: 23500000, pnL: 125000, return: 0.53 },
    { product: 'Gasoil', exposure: 15375000, pnL: 95000, return: 0.62 },
    { product: 'Fuel Oil', exposure: 10250000, pnL: 65000, return: 0.63 }
  ],
  calculatedAt: new Date().toISOString()
}

const mockMarketInsights: MarketInsights = {
  marketDataCount: 1250,
  lastUpdate: new Date().toISOString(),
  keyPrices: [
    { product: 'Brent', price: 82.45, change: 1.25, changePercent: 1.54, lastUpdate: new Date().toISOString() },
    { product: 'WTI', price: 78.92, change: 0.87, changePercent: 1.11, lastUpdate: new Date().toISOString() },
    { product: 'Dubai', price: 81.33, change: -0.45, changePercent: -0.55, lastUpdate: new Date().toISOString() },
    { product: 'MGO', price: 850.00, change: 5.20, changePercent: 0.62, lastUpdate: new Date().toISOString() }
  ],
  volatilityIndicators: {
    'Brent': 24.5,
    'WTI': 26.2,
    'MGO': 18.7
  },
  correlationMatrix: {
    'Brent': { 'Brent': 1.0, 'WTI': 0.87, 'MGO': 0.65 },
    'WTI': { 'Brent': 0.87, 'WTI': 1.0, 'MGO': 0.73 },
    'MGO': { 'Brent': 0.65, 'WTI': 0.73, 'MGO': 1.0 }
  },
  technicalIndicators: {
    'RSI_Brent': 58.3,
    'RSI_WTI': 55.1,
    'SMA20_Brent': 81.20,
    'SMA50_Brent': 79.85
  },
  marketTrends: [
    { product: 'Brent', trend: 'Bullish', strength: 0.72 },
    { product: 'WTI', trend: 'Bullish', strength: 0.65 },
    { product: 'MGO', trend: 'Sideways', strength: 0.45 }
  ],
  sentimentIndicators: {
    'overallSentiment': 0.65,
    'bullishRatio': 0.72,
    'bearishRatio': 0.28
  },
  calculatedAt: new Date().toISOString()
}

const mockOperationalStatus: OperationalStatus = {
  activeShipments: 3,
  pendingDeliveries: 5,
  completedDeliveries: 15,
  contractsAwaitingExecution: 8,
  contractsInLaycan: 4,
  upcomingLaycans: [
    {
      contractNumber: 'PC-2025-001',
      contractType: 'Purchase',
      laycanStart: new Date(Date.now() + 3 * 24 * 60 * 60 * 1000).toISOString(),
      laycanEnd: new Date(Date.now() + 5 * 24 * 60 * 60 * 1000).toISOString(),
      product: 'Brent',
      quantity: 25000
    },
    {
      contractNumber: 'PC-2025-002',
      contractType: 'Purchase',
      laycanStart: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
      laycanEnd: new Date(Date.now() + 9 * 24 * 60 * 60 * 1000).toISOString(),
      product: 'WTI',
      quantity: 35000
    },
    {
      contractNumber: 'SC-2025-003',
      contractType: 'Sales',
      laycanStart: new Date(Date.now() + 10 * 24 * 60 * 60 * 1000).toISOString(),
      laycanEnd: new Date(Date.now() + 12 * 24 * 60 * 60 * 1000).toISOString(),
      product: 'MGO',
      quantity: 20000
    }
  ],
  systemHealth: {
    databaseStatus: 'Healthy',
    cacheStatus: 'Healthy',
    marketDataStatus: 'Healthy',
    overallStatus: 'Healthy'
  },
  cacheHitRatio: 0.92,
  lastDataRefresh: new Date().toISOString(),
  calculatedAt: new Date().toISOString()
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
