export interface DashboardOverview {
  totalPosition: number
  totalPositionCurrency: string
  dailyPnL: number
  dailyPnLCurrency: string
  var95: number
  var95Currency: string
  unrealizedPnL: number
  unrealizedPnLCurrency: string
  realizationRatio: number
  activeContracts: number
  pendingShipments: number
  lastUpdated: string
}

export interface TradingMetrics {
  totalVolume: number
  volumeUnit: string
  tradingFrequency: number
  avgDealSize: number
  avgDealSizeCurrency: string
  productDistribution: ProductDistribution[]
  counterpartyConcentration: CounterpartyConcentration[]
  lastUpdated: string
}

export interface ProductDistribution {
  productType: string
  volumePercentage: number
  pnlContribution: number
}

export interface CounterpartyConcentration {
  counterpartyName: string
  exposurePercentage: number
  creditRating: string
}

export interface PerformanceAnalytics {
  monthlyPnL: MonthlyPnL[]
  sharpeRatio: number
  maxDrawdown: number
  winRate: number
  avgWinSize: number
  avgLossSize: number
  volatility: number
  lastUpdated: string
}

export interface MonthlyPnL {
  month: string
  pnl: number
  cumulativePnL: number
}

export interface MarketInsights {
  benchmarkPrices: BenchmarkPrice[]
  volatility: MarketVolatility[]
  correlations: MarketCorrelation[]
  marketSentiment: string
  riskFactors: string[]
  lastUpdated: string
}

export interface BenchmarkPrice {
  benchmark: string
  currentPrice: number
  change24h: number
  changePercent24h: number
  currency: string
}

export interface MarketVolatility {
  product: string
  impliedVolatility: number
  historicalVolatility: number
  volatilityTrend: string
}

export interface MarketCorrelation {
  product1: string
  product2: string
  correlation: number
  trend: string
}

export interface OperationalStatus {
  activeContracts: number
  pendingContracts: number
  completedContractsThisMonth: number
  shipmentStatus: ShipmentStatus[]
  riskAlerts: RiskAlert[]
  upcomingDeliveries: UpcomingDelivery[]
  lastUpdated: string
}

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
  alertType: string
  severity: 'High' | 'Medium' | 'Low'
  message: string
  timestamp: string
}

export interface UpcomingDelivery {
  contractNumber: string
  counterparty: string
  deliveryDate: string
  quantity: number
  unit: string
  product: string
  status: string
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