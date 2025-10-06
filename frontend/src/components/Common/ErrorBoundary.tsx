import React, { Component, ErrorInfo, ReactNode } from 'react'
import {
  Box,
  Container,
  Typography,
  Button,
  Paper,
  Alert,
  AlertTitle
} from '@mui/material'
import {
  Refresh as RefreshIcon,
  Home as HomeIcon,
  BugReport as BugReportIcon
} from '@mui/icons-material'
import { AppError } from '@/types'
import { handleComponentError } from '@/utils/errorUtils'
import ErrorDisplay from './ErrorDisplay'

interface Props {
  children: ReactNode
  fallback?: (error: AppError, retry: () => void) => ReactNode
  enableRetry?: boolean
  enableReporting?: boolean
  level?: 'page' | 'component' | 'critical'
}

interface State {
  hasError: boolean
  error: AppError | null
  errorId: string | null
}

class ErrorBoundary extends Component<Props, State> {
  private retryTimeoutId: number | null = null

  constructor(props: Props) {
    super(props)
    this.state = {
      hasError: false,
      error: null,
      errorId: null
    }
  }

  static getDerivedStateFromError(_error: Error): State {
    // Update state so the next render will show the fallback UI
    return {
      hasError: true,
      error: null, // Will be set in componentDidCatch
      errorId: `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    const appError = handleComponentError(error, errorInfo)
    
    this.setState({ error: appError })
    
    // Log to console
    console.error('ErrorBoundary caught an error:', appError)
    
    // Report to error tracking service
    if (this.props.enableReporting && typeof window !== 'undefined') {
      if ((window as any).errorTracker) {
        (window as any).errorTracker.captureException(appError)
      }
    }
  }

  handleRetry = () => {
    this.setState({
      hasError: false,
      error: null,
      errorId: null
    })
  }

  handleGoHome = () => {
    window.location.href = '/'
  }

  handleReload = () => {
    window.location.reload()
  }

  componentWillUnmount() {
    if (this.retryTimeoutId) {
      clearTimeout(this.retryTimeoutId)
    }
  }

  render() {
    const { hasError, error } = this.state
    const { 
      children, 
      fallback, 
      enableRetry = true, 
      level = 'component' 
    } = this.props

    if (hasError && error) {
      // Use custom fallback if provided
      if (fallback) {
        return fallback(error, this.handleRetry)
      }

      // Different layouts based on error level
      if (level === 'critical' || level === 'page') {
        return (
          <Container maxWidth="md" sx={{ mt: 4 }}>
            <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
              <BugReportIcon sx={{ fontSize: 64, color: 'error.main', mb: 2 }} />
              
              <Typography variant="h4" gutterBottom color="error">
                {level === 'critical' ? 'Critical System Error' : 'Page Error'}
              </Typography>
              
              <Typography variant="body1" color="text.secondary" paragraph>
                {error.userFriendlyMessage || 
                 'An unexpected error occurred. We apologize for the inconvenience.'}
              </Typography>
              
              <Alert severity="error" sx={{ mb: 3, textAlign: 'left' }}>
                <AlertTitle>Error Details</AlertTitle>
                <Typography variant="body2">
                  <strong>Error Code:</strong> {error.code}
                </Typography>
                <Typography variant="body2">
                  <strong>Trace ID:</strong> {error.traceId}
                </Typography>
                <Typography variant="body2">
                  <strong>Timestamp:</strong> {new Date(error.timestamp).toLocaleString()}
                </Typography>
              </Alert>
              
              <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
                {enableRetry && (
                  <Button
                    variant="contained"
                    startIcon={<RefreshIcon />}
                    onClick={this.handleRetry}
                    color="primary"
                  >
                    Try Again
                  </Button>
                )}
                
                <Button
                  variant="outlined"
                  startIcon={<RefreshIcon />}
                  onClick={this.handleReload}
                  color="primary"
                >
                  Reload Page
                </Button>
                
                <Button
                  variant="outlined"
                  startIcon={<HomeIcon />}
                  onClick={this.handleGoHome}
                  color="primary"
                >
                  Go Home
                </Button>
              </Box>
              
              {process.env.NODE_ENV === 'development' && (
                <Box sx={{ mt: 3, textAlign: 'left' }}>
                  <Typography variant="h6" gutterBottom>
                    Development Details:
                  </Typography>
                  <Paper 
                    variant="outlined" 
                    sx={{ 
                      p: 2, 
                      backgroundColor: 'grey.50',
                      maxHeight: 300,
                      overflow: 'auto'
                    }}
                  >
                    <Typography 
                      component="pre" 
                      variant="caption" 
                      sx={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}
                    >
                      {JSON.stringify(error, null, 2)}
                    </Typography>
                  </Paper>
                </Box>
              )}
            </Paper>
          </Container>
        )
      }

      // Component-level error display
      return (
        <Box sx={{ p: 2 }}>
          <ErrorDisplay
            error={error}
            onRetry={enableRetry ? this.handleRetry : undefined}
            showTitle={true}
          />
        </Box>
      )
    }

    return children
  }
}

// Higher-order component for easy wrapping
export const withErrorBoundary = <P extends object>(
  WrappedComponent: React.ComponentType<P>,
  errorBoundaryProps?: Omit<Props, 'children'>
) => {
  const WithErrorBoundaryComponent = (props: P) => (
    <ErrorBoundary {...errorBoundaryProps}>
      <WrappedComponent {...props} />
    </ErrorBoundary>
  )

  WithErrorBoundaryComponent.displayName = 
    `withErrorBoundary(${WrappedComponent.displayName || WrappedComponent.name})`

  return WithErrorBoundaryComponent
}

// Hook for manual error boundary triggering
export const useErrorBoundary = () => {
  const [error, setError] = React.useState<Error | null>(null)

  const captureError = React.useCallback((error: Error) => {
    setError(error)
  }, [])

  const resetError = React.useCallback(() => {
    setError(null)
  }, [])

  React.useEffect(() => {
    if (error) {
      throw error
    }
  }, [error])

  return { captureError, resetError }
}

export default ErrorBoundary
