import { useState, useCallback, useEffect } from 'react'
import { StandardApiError, AppError, ErrorContext } from '@/types'
import { 
  normalizeError, 
  createAppError, 
  logError, 
  retryOperation,
  isRecoverableError
} from '@/utils/errorUtils'

interface UseErrorHandlerOptions {
  enableRetry?: boolean
  maxRetries?: number
  retryDelay?: number
  enableLogging?: boolean
  context?: Partial<ErrorContext>
}

interface UseErrorHandlerReturn {
  error: AppError | null
  isLoading: boolean
  clearError: () => void
  handleError: (error: any, userMessage?: string) => void
  executeWithRetry: <T>(operation: () => Promise<T>) => Promise<T>
  executeWithErrorHandling: <T>(operation: () => Promise<T>, userMessage?: string) => Promise<T | null>
}

/**
 * Custom hook for centralized error handling
 */
export const useErrorHandler = (options: UseErrorHandlerOptions = {}): UseErrorHandlerReturn => {
  const {
    enableRetry = true,
    maxRetries = 3,
    retryDelay = 1000,
    enableLogging = true,
    context = {}
  } = options
  
  const [error, setError] = useState<AppError | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  
  const clearError = useCallback(() => {
    setError(null)
  }, [])
  
  const handleError = useCallback((error: any, userMessage?: string) => {
    const normalizedError = normalizeError(error)
    
    // Create enhanced error with context
    const enhancedContext: ErrorContext = {
      timestamp: new Date().toISOString(),
      url: window.location.href,
      userAgent: navigator.userAgent,
      ...context
    }
    
    const appError = createAppError(normalizedError, enhancedContext, userMessage)
    
    setError(appError)
    
    if (enableLogging) {
      logError(normalizedError, enhancedContext)
    }
    
    // Report to error tracking service (if available)
    if (typeof window !== 'undefined' && (window as any).errorTracker) {
      (window as any).errorTracker.captureException(appError)
    }
  }, [context, enableLogging])
  
  const executeWithRetry = useCallback(async <T,>(operation: () => Promise<T>): Promise<T> => {
    if (!enableRetry) {
      return operation()
    }
    
    return retryOperation(operation, maxRetries, retryDelay)
  }, [enableRetry, maxRetries, retryDelay])
  
  const executeWithErrorHandling = useCallback(async <T,>(
    operation: () => Promise<T>,
    userMessage?: string
  ): Promise<T | null> => {
    setIsLoading(true)
    clearError()
    
    try {
      const result = await executeWithRetry(operation)
      return result
    } catch (error) {
      handleError(error, userMessage)
      return null
    } finally {
      setIsLoading(false)
    }
  }, [executeWithRetry, handleError, clearError])
  
  return {
    error,
    isLoading,
    clearError,
    handleError,
    executeWithRetry,
    executeWithErrorHandling
  }
}

/**
 * Hook for handling form validation errors
 */
export const useFormErrorHandler = () => {
  const [validationErrors, setValidationErrors] = useState<Record<string, string[]>>({})
  
  const clearValidationErrors = useCallback(() => {
    setValidationErrors({})
  }, [])
  
  const setFieldError = useCallback((field: string, messages: string[]) => {
    setValidationErrors(prev => ({
      ...prev,
      [field]: messages
    }))
  }, [])
  
  const clearFieldError = useCallback((field: string) => {
    setValidationErrors(prev => {
      const newErrors = { ...prev }
      delete newErrors[field]
      return newErrors
    })
  }, [])
  
  const handleValidationError = useCallback((error: StandardApiError) => {
    if (error.validationErrors) {
      setValidationErrors(error.validationErrors)
    } else {
      clearValidationErrors()
    }
  }, [])
  
  const hasErrors = Object.keys(validationErrors).length > 0
  const getFieldErrors = useCallback((field: string) => validationErrors[field] || [], [validationErrors])
  const hasFieldError = useCallback((field: string) => Boolean(validationErrors[field]), [validationErrors])
  
  return {
    validationErrors,
    hasErrors,
    clearValidationErrors,
    setFieldError,
    clearFieldError,
    handleValidationError,
    getFieldErrors,
    hasFieldError
  }
}

/**
 * Hook for handling async operations with error boundaries
 */
export const useAsyncOperation = <T,>(options: UseErrorHandlerOptions = {}) => {
  const { executeWithErrorHandling, error, isLoading, clearError } = useErrorHandler(options)
  const [data, setData] = useState<T | null>(null)
  
  const execute = useCallback(async (
    operation: () => Promise<T>,
    userMessage?: string
  ) => {
    const result = await executeWithErrorHandling(operation, userMessage)
    if (result !== null) {
      setData(result)
    }
    return result
  }, [executeWithErrorHandling])
  
  const reset = useCallback(() => {
    setData(null)
    clearError()
  }, [clearError])
  
  return {
    data,
    error,
    isLoading,
    execute,
    reset
  }
}

/**
 * Hook for handling API calls with automatic error handling
 */
export const useApiCall = <T,>(options: UseErrorHandlerOptions = {}) => {
  const { executeWithErrorHandling, error, isLoading, clearError } = useErrorHandler(options)
  const [data, setData] = useState<T | null>(null)
  const [hasBeenCalled, setHasBeenCalled] = useState(false)
  
  const call = useCallback(async (
    apiCall: () => Promise<T>,
    userMessage?: string
  ) => {
    setHasBeenCalled(true)
    const result = await executeWithErrorHandling(apiCall, userMessage)
    if (result !== null) {
      setData(result)
    }
    return result
  }, [executeWithErrorHandling])
  
  const reset = useCallback(() => {
    setData(null)
    setHasBeenCalled(false)
    clearError()
  }, [clearError])
  
  // Auto-retry for recoverable errors
  useEffect(() => {
    if (error && isRecoverableError(error) && hasBeenCalled) {
      const timer = setTimeout(() => {
        // Could implement auto-retry logic here
      }, 5000)
      
      return () => clearTimeout(timer)
    }
  }, [error, hasBeenCalled])
  
  return {
    data,
    error,
    isLoading,
    hasBeenCalled,
    call,
    reset
  }
}

export default useErrorHandler
