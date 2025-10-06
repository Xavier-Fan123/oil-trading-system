import React from 'react'
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Collapse,
  IconButton,
  Typography,
  Chip,
  Divider,
  Stack
} from '@mui/material'
import {
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Refresh as RefreshIcon,
  ContactSupport as ContactSupportIcon
} from '@mui/icons-material'
import { StandardApiError, ErrorSeverity, AppError } from '@/types'
import { 
  getErrorSeverity, 
  getUserFriendlyMessage, 
  getErrorDisplayConfig,
  extractValidationErrors 
} from '@/utils/errorUtils'

interface ErrorDisplayProps {
  error: StandardApiError | AppError
  onRetry?: () => void
  onDismiss?: () => void
  showTitle?: boolean
  compact?: boolean
  className?: string
}

export const ErrorDisplay: React.FC<ErrorDisplayProps> = ({
  error,
  onRetry,
  onDismiss,
  showTitle = true,
  compact = false,
  className
}) => {
  const [showDetails, setShowDetails] = React.useState(false)
  
  const severity = getErrorSeverity(error)
  const displayConfig = getErrorDisplayConfig(error)
  const userFriendlyMessage = getUserFriendlyMessage(error)
  const validationErrors = extractValidationErrors(error)
  const hasValidationErrors = Object.keys(validationErrors).length > 0
  
  const getSeverityColor = (severity: ErrorSeverity) => {
    switch (severity) {
      case ErrorSeverity.Critical: return 'error'
      case ErrorSeverity.High: return 'error'
      case ErrorSeverity.Medium: return 'warning'
      case ErrorSeverity.Low: return 'info'
      default: return 'error'
    }
  }
  
  const getSeverityIcon = (severity: ErrorSeverity) => {
    switch (severity) {
      case ErrorSeverity.Critical:
      case ErrorSeverity.High:
        return <ErrorIcon />
      case ErrorSeverity.Medium:
        return <WarningIcon />
      case ErrorSeverity.Low:
        return <InfoIcon />
      default:
        return <ErrorIcon />
    }
  }
  
  const formatTimestamp = (timestamp: string) => {
    try {
      return new Date(timestamp).toLocaleString()
    } catch {
      return timestamp
    }
  }
  
  if (compact) {
    return (
      <Alert 
        severity={getSeverityColor(severity)} 
        onClose={onDismiss}
        className={className}
        action={
          onRetry && displayConfig.allowRetry ? (
            <IconButton size="small" onClick={onRetry} color="inherit">
              <RefreshIcon fontSize="small" />
            </IconButton>
          ) : undefined
        }
      >
        {userFriendlyMessage}
      </Alert>
    )
  }
  
  return (
    <Alert 
      severity={getSeverityColor(severity)} 
      icon={getSeverityIcon(severity)}
      onClose={onDismiss}
      className={className}
    >
      {showTitle && (
        <AlertTitle>
          Error {error.code}
          <Chip 
            label={severity.toUpperCase()} 
            size="small" 
            color={getSeverityColor(severity)}
            sx={{ ml: 1, height: 20 }}
          />
        </AlertTitle>
      )}
      
      <Typography variant="body2" gutterBottom>
        {userFriendlyMessage}
      </Typography>
      
      {hasValidationErrors && (
        <Box mt={1}>
          <Typography variant="subtitle2" gutterBottom>
            Validation Errors:
          </Typography>
          {Object.entries(validationErrors).map(([field, messages]) => (
            <Box key={field} ml={1}>
              <Typography variant="body2" color="error">
                <strong>{field}:</strong> {messages.join(', ')}
              </Typography>
            </Box>
          ))}
        </Box>
      )}
      
      {(displayConfig.showDetails || displayConfig.showTraceId || displayConfig.showTimestamp) && (
        <Box mt={2}>
          <Button
            size="small"
            onClick={() => setShowDetails(!showDetails)}
            startIcon={showDetails ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          >
            {showDetails ? 'Hide Details' : 'Show Details'}
          </Button>
          
          <Collapse in={showDetails}>
            <Box mt={1} p={1} bgcolor="grey.100" borderRadius={1}>
              {displayConfig.showTraceId && error.traceId && (
                <Typography variant="caption" display="block" gutterBottom>
                  <strong>Trace ID:</strong> {error.traceId}
                </Typography>
              )}
              
              {displayConfig.showTimestamp && (
                <Typography variant="caption" display="block" gutterBottom>
                  <strong>Timestamp:</strong> {formatTimestamp(error.timestamp)}
                </Typography>
              )}
              
              {error.path && (
                <Typography variant="caption" display="block" gutterBottom>
                  <strong>Path:</strong> {error.path}
                </Typography>
              )}
              
              <Typography variant="caption" display="block" gutterBottom>
                <strong>Status Code:</strong> {error.statusCode}
              </Typography>
              
              {displayConfig.showDetails && error.details && (
                <Box mt={1}>
                  <Typography variant="caption" display="block" gutterBottom>
                    <strong>Technical Details:</strong>
                  </Typography>
                  <Typography 
                    variant="caption" 
                    component="pre" 
                    sx={{ 
                      whiteSpace: 'pre-wrap', 
                      fontFamily: 'monospace',
                      fontSize: '0.7rem',
                      maxHeight: 200,
                      overflow: 'auto'
                    }}
                  >
                    {typeof error.details === 'string' 
                      ? error.details 
                      : JSON.stringify(error.details, null, 2)
                    }
                  </Typography>
                </Box>
              )}
            </Box>
          </Collapse>
        </Box>
      )}
      
      {(displayConfig.allowRetry || displayConfig.contactSupport) && (
        <Box mt={2}>
          <Divider sx={{ my: 1 }} />
          <Stack direction="row" spacing={1}>
            {onRetry && displayConfig.allowRetry && (
              <Button
                size="small"
                variant="outlined"
                startIcon={<RefreshIcon />}
                onClick={onRetry}
                color={getSeverityColor(severity)}
              >
                Retry
              </Button>
            )}
            
            {displayConfig.contactSupport && (
              <Button
                size="small"
                variant="outlined"
                startIcon={<ContactSupportIcon />}
                href={`mailto:support@oiltrading.com?subject=Error Report - ${error.code}&body=Error Details:%0A%0ATrace ID: ${error.traceId}%0ATimestamp: ${error.timestamp}%0AMessage: ${error.message}`}
                color={getSeverityColor(severity)}
              >
                Contact Support
              </Button>
            )}
          </Stack>
        </Box>
      )}
    </Alert>
  )
}

// Specialized components for common use cases

interface ValidationErrorDisplayProps {
  errors: Record<string, string[]>
  onDismiss?: () => void
}

export const ValidationErrorDisplay: React.FC<ValidationErrorDisplayProps> = ({ 
  errors, 
  onDismiss 
}) => {
  const hasErrors = Object.keys(errors).length > 0
  
  if (!hasErrors) return null
  
  return (
    <Alert severity="warning" onClose={onDismiss}>
      <AlertTitle>Validation Failed</AlertTitle>
      <Typography variant="body2" gutterBottom>
        Please correct the following errors:
      </Typography>
      
      {Object.entries(errors).map(([field, messages]) => (
        <Box key={field} mt={1}>
          <Typography variant="body2" color="error">
            <strong>{field}:</strong> {messages.join(', ')}
          </Typography>
        </Box>
      ))}
    </Alert>
  )
}

interface NetworkErrorDisplayProps {
  onRetry?: () => void
  onDismiss?: () => void
}

export const NetworkErrorDisplay: React.FC<NetworkErrorDisplayProps> = ({ 
  onRetry, 
  onDismiss 
}) => {
  return (
    <Alert 
      severity="error" 
      onClose={onDismiss}
      action={
        onRetry ? (
          <Button size="small" onClick={onRetry} color="inherit">
            <RefreshIcon sx={{ mr: 0.5 }} />
            Retry
          </Button>
        ) : undefined
      }
    >
      <AlertTitle>Connection Error</AlertTitle>
      <Typography variant="body2">
        Unable to connect to the server. Please check your internet connection and try again.
      </Typography>
    </Alert>
  )
}

export default ErrorDisplay
