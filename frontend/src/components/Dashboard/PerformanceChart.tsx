import React from 'react'
import {
  Card,
  CardContent,
  Typography,
  Grid,
  Box,
  LinearProgress,
} from '@mui/material'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts'
import { KPICard } from '@/components/Common/KPICard'
import { usePerformanceAnalytics } from '@/hooks/useDashboard'

export const PerformanceChart: React.FC = () => {
  const { data, isLoading, error } = usePerformanceAnalytics()

  if (error) {
    return (
      <Card>
        <CardContent>
          <Typography color="error">Failed to load performance data</Typography>
        </CardContent>
      </Card>
    )
  }

  const formatTooltipValue = (value: number) => {
    return `$${value.toLocaleString()}K`
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Performance Analytics
        </Typography>
        
        {isLoading && <LinearProgress sx={{ mb: 2 }} />}
        
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Typography variant="subtitle1" gutterBottom>
              Monthly P&L Trend
            </Typography>
            <Box sx={{ height: 300 }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={[]}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#2a2d3a" />
                  <XAxis 
                    dataKey="month" 
                    stroke="#b0b0b0"
                    tick={{ fill: '#b0b0b0' }}
                  />
                  <YAxis 
                    stroke="#b0b0b0"
                    tick={{ fill: '#b0b0b0' }}
                    tickFormatter={(value) => `$${value}K`}
                  />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1a1d29',
                      border: '1px solid #2a2d3a',
                      borderRadius: '8px',
                      color: '#ffffff',
                    }}
                    formatter={(value: number, name: string) => [
                      formatTooltipValue(value),
                      name === 'pnl' ? 'Monthly P&L' : 'Cumulative P&L'
                    ]}
                  />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="pnl"
                    stroke="#2196f3"
                    strokeWidth={2}
                    dot={{ fill: '#2196f3', strokeWidth: 2 }}
                    name="Monthly P&L"
                  />
                  <Line
                    type="monotone"
                    dataKey="cumulativePnL"
                    stroke="#4caf50"
                    strokeWidth={2}
                    dot={{ fill: '#4caf50', strokeWidth: 2 }}
                    name="Cumulative P&L"
                  />
                </LineChart>
              </ResponsiveContainer>
            </Box>
          </Grid>
          
          <Grid item xs={12} md={4}>
            <Typography variant="subtitle1" gutterBottom>
              Key Metrics
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <KPICard
                  title="Sharpe Ratio"
                  value={0}
                  isLoading={isLoading}
                  color="primary"
                />
              </Grid>
              
              <Grid item xs={12}>
                <KPICard
                  title="Max Drawdown"
                  value={0}
                  suffix="%"
                  isLoading={isLoading}
                  color="error"
                />
              </Grid>
              
              <Grid item xs={12}>
                <KPICard
                  title="Win Rate"
                  value={0}
                  suffix="%"
                  isLoading={isLoading}
                  color="success"
                />
              </Grid>
              
              <Grid item xs={12}>
                <KPICard
                  title="Avg Win Size"
                  value={0}
                  prefix="$"
                  suffix="K"
                  isLoading={isLoading}
                  color="success"
                />
              </Grid>
              
              <Grid item xs={12}>
                <KPICard
                  title="Avg Loss Size"
                  value={0}
                  prefix="$"
                  suffix="K"
                  isLoading={isLoading}
                  color="error"
                />
              </Grid>
              
              <Grid item xs={12}>
                <KPICard
                  title="Volatility"
                  value={data?.volatility?.toFixed(1) || 0}
                  suffix="%"
                  isLoading={isLoading}
                  color="warning"
                />
              </Grid>
            </Grid>
          </Grid>
        </Grid>
        
        <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">
            Last Updated: N/A
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}