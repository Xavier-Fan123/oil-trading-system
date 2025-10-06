import { 
  StandardApiError, 
  ApiError, 
  AppError, 
  ErrorSeverity, 
  ErrorCodes, 
  ErrorContext,
  ErrorDisplayConfig
} from '@/types'

/**
 * Determines if an error is the new standardized format
 */
export function isStandardApiError(error: any): error is StandardApiError {
  return error && typeof error === 'object' && 'code' in error && 'traceId' in error
}

/**
 * Determines if an error is the legacy format
 */
export function isLegacyApiError(error: any): error is ApiError {
  return error && typeof error === 'object' && 'message' in error && 'statusCode' in error && !('code' in error)
}

/**
 * Converts any error to a standardized format
 */
export function normalizeError(error: any): StandardApiError {
  if (isStandardApiError(error)) {
    return error
  }
  
  if (isLegacyApiError(error)) {
    return {
      code: getErrorCodeFromStatusCode(error.statusCode),
      message: error.message,
      timestamp: error.timestamp,
      traceId: generateTraceId(),
      statusCode: error.statusCode
    }
  }
  
  // Handle generic Error objects
  if (error instanceof Error) {
    return {
      code: ErrorCodes.InternalServerError,
      message: error.message,
      timestamp: new Date().toISOString(),
      traceId: generateTraceId(),
      statusCode: 500,
      details: {
        type: error.constructor.name,
        stack: error.stack
      }
    }
  }
  
  // Handle unknown error types
  return {
    code: ErrorCodes.InternalServerError,
    message: typeof error === 'string' ? error : 'Unknown error occurred',
    timestamp: new Date().toISOString(),
    traceId: generateTraceId(),
    statusCode: 500,
    details: error
  }
}

/**
 * Maps HTTP status codes to appropriate error codes
 */
function getErrorCodeFromStatusCode(statusCode: number): string {
  switch (statusCode) {
    case 400: return ErrorCodes.InvalidInput
    case 401: return ErrorCodes.Unauthorized
    case 403: return ErrorCodes.Forbidden
    case 404: return ErrorCodes.NotFound
    case 408: return ErrorCodes.RequestTimeout
    case 409: return ErrorCodes.Conflict
    case 422: return ErrorCodes.ValidationFailed
    case 429: return ErrorCodes.RateLimitExceeded
    case 500: return ErrorCodes.InternalServerError
    case 502: return ErrorCodes.ExternalServiceError
    case 503: return ErrorCodes.ServiceUnavailable
    default: return ErrorCodes.InternalServerError
  }
}

/**
 * Generates a client-side trace ID for error tracking
 */
function generateTraceId(): string {
  return `client-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
}

/**
 * Determines error severity based on error code and status
 */
export function getErrorSeverity(error: StandardApiError): ErrorSeverity {
  if (error.statusCode >= 500) {
    return ErrorSeverity.Critical
  }
  
  if (error.statusCode >= 400) {
    switch (error.code) {
      case ErrorCodes.Unauthorized:
      case ErrorCodes.Forbidden:
      case ErrorCodes.BusinessRuleViolation:
        return ErrorSeverity.High
      
      case ErrorCodes.ValidationFailed:
      case ErrorCodes.InvalidInput:
      case ErrorCodes.NotFound:
        return ErrorSeverity.Medium
      
      default:
        return ErrorSeverity.Medium
    }
  }
  
  return ErrorSeverity.Low
}

/**
 * Checks if an error is recoverable (user can retry or fix)
 */
export function isRecoverableError(error: StandardApiError): boolean {
  const recoverableCodes = [
    ErrorCodes.ValidationFailed,
    ErrorCodes.InvalidInput,
    ErrorCodes.MissingRequiredField,
    ErrorCodes.InvalidFormat,
    ErrorCodes.ValueOutOfRange,
    ErrorCodes.RequestTimeout,
    ErrorCodes.OperationTimeout,
    ErrorCodes.NetworkError,
    ErrorCodes.ConnectionError
  ]
  
  return recoverableCodes.includes(error.code as any)
}

/**
 * Creates an enhanced AppError with additional context
 */
export function createAppError(
  error: StandardApiError,
  context?: Partial<ErrorContext>,
  userFriendlyMessage?: string
): AppError {
  const fullContext: ErrorContext = {
    timestamp: new Date().toISOString(),
    url: window.location.href,
    userAgent: navigator.userAgent,
    ...context
  }
  
  return {
    ...error,
    severity: getErrorSeverity(error),
    userFriendlyMessage: userFriendlyMessage || getUserFriendlyMessage(error),
    recoverable: isRecoverableError(error),
    context: fullContext
  }
}

/**
 * Generates user-friendly error messages
 */
export function getUserFriendlyMessage(error: StandardApiError): string {
  switch (error.code) {
    case ErrorCodes.NetworkError:
    case ErrorCodes.ConnectionError:
      return 'Unable to connect to the server. Please check your internet connection and try again.'
    
    case ErrorCodes.Unauthorized:
      return 'Please log in to access this feature.'
    
    case ErrorCodes.Forbidden:
      return 'You do not have permission to perform this action.'
    
    case ErrorCodes.NotFound:
    case ErrorCodes.ResourceNotFound:
      return 'The requested item could not be found.'
    
    case ErrorCodes.ValidationFailed:
      return 'Please check your input and try again.'
    
    case ErrorCodes.BusinessRuleViolation:
      return 'This action violates business rules. Please review and try again.'
    
    case ErrorCodes.RateLimitExceeded:
      return 'Too many requests. Please wait a moment and try again.'
    
    case ErrorCodes.ServiceUnavailable:
      return 'The service is temporarily unavailable. Please try again later.'
    
    case ErrorCodes.InternalServerError:
      return 'An unexpected error occurred. Please try again or contact support.'
    
    case ErrorCodes.RequestTimeout:
    case ErrorCodes.OperationTimeout:
      return 'The request timed out. Please try again.'
    
    default:
      return error.message || 'An unexpected error occurred.'
  }
}

/**
 * Extracts validation errors for form display
 */
export function extractValidationErrors(error: StandardApiError): Record<string, string[]> {
  if (error.validationErrors) {
    return error.validationErrors
  }
  
  // Try to extract from details if structured differently
  if (error.details && typeof error.details === 'object') {
    const details = error.details as any
    if (details.errors && typeof details.errors === 'object') {
      return details.errors
    }
  }
  
  return {}
}

/**
 * Formats error for logging/debugging
 */
export function formatErrorForLogging(error: StandardApiError, context?: ErrorContext): string {
  const logData = {
    code: error.code,
    message: error.message,
    statusCode: error.statusCode,
    traceId: error.traceId,
    timestamp: error.timestamp,
    path: error.path,
    details: error.details,
    context: context || {}
  }
  
  return JSON.stringify(logData, null, 2)
}

/**
 * Creates error display configuration based on error type
 */
export function getErrorDisplayConfig(error: StandardApiError): ErrorDisplayConfig {
  const isProduction = process.env.NODE_ENV === 'production'
  const severity = getErrorSeverity(error)
  
  return {
    showDetails: !isProduction || severity === ErrorSeverity.Low,
    showTraceId: !isProduction || severity >= ErrorSeverity.High,
    showTimestamp: !isProduction,
    allowRetry: isRecoverableError(error),
    contactSupport: severity >= ErrorSeverity.High || !isRecoverableError(error)
  }
}

/**
 * Logs error to console with appropriate level
 */
export function logError(error: StandardApiError, context?: ErrorContext): void {
  const severity = getErrorSeverity(error)
  const logMessage = formatErrorForLogging(error, context)
  
  switch (severity) {
    case ErrorSeverity.Critical:
      console.error('CRITICAL ERROR:', logMessage)
      break
    
    case ErrorSeverity.High:
      console.error('HIGH ERROR:', logMessage)
      break
    
    case ErrorSeverity.Medium:
      console.warn('MEDIUM ERROR:', logMessage)
      break
    
    case ErrorSeverity.Low:
    default:
      console.info('LOW ERROR:', logMessage)
      break
  }
}

/**
 * Retry logic for recoverable errors
 */
export async function retryOperation<T>(
  operation: () => Promise<T>,
  maxRetries: number = 3,
  delayMs: number = 1000
): Promise<T> {
  let lastError: any
  
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await operation()
    } catch (error) {
      lastError = error
      const normalizedError = normalizeError(error)
      
      if (!isRecoverableError(normalizedError) || attempt === maxRetries) {
        throw error
      }
      
      // Exponential backoff
      const delay = delayMs * Math.pow(2, attempt - 1)
      await new Promise(resolve => setTimeout(resolve, delay))
    }
  }
  
  throw lastError
}

/**
 * Error boundary utility for React components
 */
export function handleComponentError(error: Error, errorInfo: any): AppError {
  const context: ErrorContext = {
    timestamp: new Date().toISOString(),
    url: window.location.href,
    userAgent: navigator.userAgent,
    component: errorInfo.componentStack,
    additionalData: {
      errorBoundary: true,
      errorInfo
    }
  }
  
  const standardError = normalizeError(error)
  return createAppError(standardError, context, 'An unexpected error occurred in this component.')
}
