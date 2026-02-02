// ============================================================================
// Dashboard Types - Matching Backend C# DTOs (camelCase JSON serialization)
// ============================================================================

export interface DashboardOverview {
  totalPositions: number
  totalExposure: number
  netExposure: number
  longPositions: number
  shortPositions: number
  flatPositions: number
  dailyPnL: number
  unrealizedPnL: number
  vaR95: number
  vaR99: number
  portfolioVolatility: number
  activePurchaseContracts: number
  activeSalesContracts: number
  pendingContracts: number
  marketDataPoints: number
  lastMarketUpdate: string
  alertCount: number
  calculatedAt: string
}

export interface TradingMetrics {
  period: string
  totalTrades: number
  totalVolume: number
  averageTradeSize: number
  purchaseVolume: number
  salesVolume: number
  paperVolume: number
  longPaperVolume: number
  shortPaperVolume: number
  productBreakdown: Record<string, number>
  counterpartyBreakdown: Record<string, number>
  tradeFrequency: number
  volumeByProduct: Record<string, number>
  calculatedAt: string
}

// Legacy alias kept for backward compatibility
export interface ProductDistribution {
  productType: string
  volumePercentage: number
  pnlContribution: number
}

// Legacy alias kept for backward compatibility
export interface CounterpartyConcentration {
  counterpartyName: string
  exposurePercentage: number
  creditRating: string
}

export interface DailyPnLEntry {
  date: string
  dailyPnL: number
  cumulativePnL: number
}

export interface ProductPerformanceEntry {
  product: string
  exposure: number
  pnL: number
  return: number
}

export interface PerformanceAnalytics {
  period: string
  totalPnL: number
  realizedPnL: number
  unrealizedPnL: number
  bestPerformingProduct: string
  worstPerformingProduct: string
  totalReturn: number
  annualizedReturn: number
  sharpeRatio: number
  maxDrawdown: number
  winRate: number
  profitFactor: number
  vaRUtilization: number
  volatilityAdjustedReturn: number
  dailyPnLHistory: DailyPnLEntry[]
  productPerformance: ProductPerformanceEntry[]
  calculatedAt: string
}

// Legacy alias kept for backward compatibility
export interface MonthlyPnL {
  month: string
  pnl: number
  cumulativePnL: number
}

export interface KeyPriceEntry {
  product: string
  price: number
  change: number
  changePercent: number
  lastUpdate: string
}

export interface MarketTrendEntry {
  product: string
  trend: string
  strength: number
}

export interface MarketInsights {
  marketDataCount: number
  lastUpdate: string
  keyPrices: KeyPriceEntry[]
  volatilityIndicators: Record<string, number>
  correlationMatrix: Record<string, Record<string, number>>
  technicalIndicators: Record<string, number>
  marketTrends: MarketTrendEntry[]
  sentimentIndicators: Record<string, number>
  calculatedAt: string
}

// Legacy alias kept for backward compatibility
export interface BenchmarkPrice {
  benchmark: string
  currentPrice: number
  change24h: number
  changePercent24h: number
  currency: string
}

// Legacy alias kept for backward compatibility
export interface MarketVolatility {
  product: string
  impliedVolatility: number
  historicalVolatility: number
  volatilityTrend: string
}

// Legacy alias kept for backward compatibility
export interface MarketCorrelation {
  product1: string
  product2: string
  correlation: number
  trend: string
}

export interface SystemHealth {
  databaseStatus: string
  cacheStatus: string
  marketDataStatus: string
  overallStatus: string
}

export interface UpcomingLaycan {
  contractNumber: string
  contractType: string
  laycanStart: string
  laycanEnd: string
  product: string
  quantity: number
}

export interface OperationalStatus {
  activeShipments: number
  pendingDeliveries: number
  completedDeliveries: number
  contractsAwaitingExecution: number
  contractsInLaycan: number
  upcomingLaycans: UpcomingLaycan[]
  systemHealth: SystemHealth
  cacheHitRatio: number
  lastDataRefresh: string
  calculatedAt: string
}

// Legacy alias kept for backward compatibility
export interface ShipmentStatus {
  shipmentId: string
  status: string
  vessel: string
  origin: string
  destination: string
  eta: string
  quantity: number
  unit: string
}

export interface RiskAlert {
  type: string
  severity: string
  message: string
  timestamp: string
}

// Legacy alias kept for backward compatibility
export interface UpcomingDelivery {
  contractNumber: string
  counterparty: string
  deliveryDate: string
  quantity: number
  unit: string
  product: string
  status: string
}

export interface KpiSummary {
  totalExposure: number
  dailyPnL: number
  vaR95: number
  portfolioCount: number
  exposureUtilization: number
  riskUtilization: number
  calculatedAt: string
}

// Legacy ApiError interface (deprecated)
export interface ApiError {
  message: string
  statusCode: number
  timestamp: string
}

// New standardized error interface
export interface StandardApiError {
  code: string
  message: string
  details?: string | object
  timestamp: string
  traceId: string
  statusCode: number
  path?: string
  validationErrors?: Record<string, string[]>
}

// Error severity levels
export enum ErrorSeverity {
  Low = 'low',
  Medium = 'medium',
  High = 'high',
  Critical = 'critical'
}

// Standard error codes (matching backend)
export const ErrorCodes = {
  // Validation errors (400 range)
  ValidationFailed: 'VALIDATION_FAILED',
  InvalidInput: 'INVALID_INPUT',
  MissingRequiredField: 'MISSING_REQUIRED_FIELD',
  InvalidFormat: 'INVALID_FORMAT',
  ValueOutOfRange: 'VALUE_OUT_OF_RANGE',

  // Authentication errors (401)
  Unauthorized: 'UNAUTHORIZED',
  InvalidCredentials: 'INVALID_CREDENTIALS',
  TokenExpired: 'TOKEN_EXPIRED',
  TokenInvalid: 'TOKEN_INVALID',

  // Authorization errors (403)
  Forbidden: 'FORBIDDEN',
  InsufficientPermissions: 'INSUFFICIENT_PERMISSIONS',
  AccessDenied: 'ACCESS_DENIED',

  // Not found errors (404)
  NotFound: 'NOT_FOUND',
  ResourceNotFound: 'RESOURCE_NOT_FOUND',
  ContractNotFound: 'CONTRACT_NOT_FOUND',
  UserNotFound: 'USER_NOT_FOUND',

  // Business logic errors (422)
  BusinessRuleViolation: 'BUSINESS_RULE_VIOLATION',
  InvalidBusinessOperation: 'INVALID_BUSINESS_OPERATION',
  ContractStateInvalid: 'CONTRACT_STATE_INVALID',
  InsufficientQuantity: 'INSUFFICIENT_QUANTITY',
  DuplicateEntry: 'DUPLICATE_ENTRY',
  ContractAlreadyMatched: 'CONTRACT_ALREADY_MATCHED',
  InvalidContractStatus: 'INVALID_CONTRACT_STATUS',
  PricingPeriodInvalid: 'PRICING_PERIOD_INVALID',
  LaycanPeriodInvalid: 'LAYCAN_PERIOD_INVALID',

  // Server errors (500 range)
  InternalServerError: 'INTERNAL_SERVER_ERROR',
  ServiceUnavailable: 'SERVICE_UNAVAILABLE',
  DatabaseError: 'DATABASE_ERROR',
  ExternalServiceError: 'EXTERNAL_SERVICE_ERROR',
  ConfigurationError: 'CONFIGURATION_ERROR',

  // Rate limiting (429)
  RateLimitExceeded: 'RATE_LIMIT_EXCEEDED',

  // Conflict (409)
  Conflict: 'CONFLICT',
  ResourceConflict: 'RESOURCE_CONFLICT',

  // Timeout (408)
  RequestTimeout: 'REQUEST_TIMEOUT',
  OperationTimeout: 'OPERATION_TIMEOUT',

  // Network errors
  NetworkError: 'NETWORK_ERROR',
  ConnectionError: 'CONNECTION_ERROR'
} as const

export type ErrorCode = typeof ErrorCodes[keyof typeof ErrorCodes]

// Error context for enhanced error tracking
export interface ErrorContext {
  userId?: string
  sessionId?: string
  userAgent?: string
  timestamp: string
  url: string
  action?: string
  component?: string
  additionalData?: Record<string, any>
}

// Enhanced error for internal application use
export interface AppError extends StandardApiError {
  severity: ErrorSeverity
  userFriendlyMessage?: string
  recoverable: boolean
  context?: ErrorContext
  originalError?: Error
}

// Error display configuration
export interface ErrorDisplayConfig {
  showDetails: boolean
  showTraceId: boolean
  showTimestamp: boolean
  allowRetry: boolean
  retryAction?: () => void
  contactSupport?: boolean
}

// Result wrapper for API calls
export interface ApiResult<T> {
  success: boolean
  data?: T
  error?: StandardApiError
}

// Form validation error structure
export interface FormValidationError {
  field: string
  messages: string[]
}

// Bulk validation errors
export interface BulkValidationErrors {
  [fieldName: string]: string[]
}