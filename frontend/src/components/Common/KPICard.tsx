import React from 'react'
import { Card, CardContent, Typography, Box, Skeleton } from '@mui/material'
import { TrendingUp, TrendingDown } from '@mui/icons-material'

interface KPICardProps {
  title: string
  value: string | number
  change?: number
  changePercent?: number
  isLoading?: boolean
  color?: 'primary' | 'secondary' | 'success' | 'error' | 'warning' | 'info'
  suffix?: string
  prefix?: string
}

export const KPICard: React.FC<KPICardProps> = ({
  title,
  value,
  change,
  changePercent,
  isLoading = false,
  color = 'primary',
  suffix = '',
  prefix = '',
}) => {
  const formatValue = (val: string | number): string => {
    if (typeof val === 'number') {
      if (Math.abs(val) >= 1000000) {
        return `${(val / 1000000).toFixed(1)}M`
      } else if (Math.abs(val) >= 1000) {
        return `${(val / 1000).toFixed(1)}K`
      }
      return val.toLocaleString()
    }
    return val
  }

  const isPositive = change !== undefined ? change >= 0 : changePercent !== undefined ? changePercent >= 0 : null

  if (isLoading) {
    return (
      <Card sx={{ height: '100%' }}>
        <CardContent>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            <Skeleton width="60%" />
          </Typography>
          <Typography variant="h4" component="div">
            <Skeleton width="80%" />
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
            <Skeleton width="40%" />
          </Box>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {title}
        </Typography>
        <Typography variant="h4" component="div" color={`${color}.main`}>
          {prefix}{formatValue(value)}{suffix}
        </Typography>
        
        {(change !== undefined || changePercent !== undefined) && (
          <Box sx={{ display: 'flex', alignItems: 'center', mt: 1 }}>
            {isPositive ? (
              <TrendingUp sx={{ color: 'success.main', mr: 0.5, fontSize: 16 }} />
            ) : (
              <TrendingDown sx={{ color: 'error.main', mr: 0.5, fontSize: 16 }} />
            )}
            <Typography
              variant="body2"
              color={isPositive ? 'success.main' : 'error.main'}
            >
              {changePercent !== undefined 
                ? `${changePercent > 0 ? '+' : ''}${changePercent.toFixed(2)}%`
                : `${change! > 0 ? '+' : ''}${formatValue(change!)}`
              }
            </Typography>
          </Box>
        )}
      </CardContent>
    </Card>
  )
}