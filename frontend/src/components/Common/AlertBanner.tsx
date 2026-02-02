import React from 'react'
import { Alert, AlertTitle, Box, Chip } from '@mui/material'
import { Warning, Error, Info } from '@mui/icons-material'
import type { RiskAlert } from '@/types'

interface AlertBannerProps {
  alerts: RiskAlert[]
  maxDisplay?: number
}

export const AlertBanner: React.FC<AlertBannerProps> = ({
  alerts,
  maxDisplay = 3
}) => {
  if (!alerts.length) return null

  const sortedAlerts = alerts
    .sort((a, b) => {
      const severityOrder: Record<string, number> = { High: 3, Medium: 2, Low: 1 }
      return (severityOrder[b.severity] || 0) - (severityOrder[a.severity] || 0)
    })
    .slice(0, maxDisplay)

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'High':
        return <Error />
      case 'Medium':
        return <Warning />
      default:
        return <Info />
    }
  }

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'High':
        return 'error'
      case 'Medium':
        return 'warning'
      default:
        return 'info'
    }
  }

  return (
    <Box sx={{ mb: 2 }}>
      {sortedAlerts.map((alert, index) => (
        <Alert
          key={index}
          severity={getSeverityColor(alert.severity) as any}
          icon={getSeverityIcon(alert.severity)}
          sx={{ mb: 1 }}
          action={
            <Chip
              label={alert.type}
              size="small"
              variant="outlined"
              color={getSeverityColor(alert.severity) as any}
            />
          }
        >
          <AlertTitle>{alert.severity} Risk Alert</AlertTitle>
          {alert.message}
        </Alert>
      ))}

      {alerts.length > maxDisplay && (
        <Alert severity="info" sx={{ mt: 1 }}>
          +{alerts.length - maxDisplay} more alerts. View all in Risk Management.
        </Alert>
      )}
    </Box>
  )
}

export default AlertBanner