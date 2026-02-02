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

  // Transform daily P&L history for the chart
  const chartData = (data?.dailyPnLHistory || []).map(entry => ({
    date: new Date(entry.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    pnl: Math.round(entry.dailyPnL / 1000 * 10) / 10,
    cumulativePnL: Math.round(entry.cumulativePnL / 1000 * 10) / 10,
  }))

  const sharpeRatio = data?.sharpeRatio || 0
  const maxDrawdown = data?.maxDrawdown || 0
  const winRate = data?.winRate || 0
  const profitFactor = data?.profitFactor || 0
  const totalReturn = data?.totalReturn || 0
  const varUtilization = (data?.vaRUtilization || 0) * 100

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
              Daily P&L Trend
            </Typography>
            <Box sx={{ height: 300 }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#2a2d3a" />
                  <XAxis
                    dataKey="date"
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
                      name === 'pnl' ? 'Daily P&L' : 'Cumulative P&L'
                    ]}
                  />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="pnl"
                    stroke="#2196f3"
                    strokeWidth={2}
                    dot={{ fill: '#2196f3', strokeWidth: 2 }}
                    name="Daily P&L"
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
                  value={sharpeRatio.toFixed(2)}
                  isLoading={isLoading}
                  color="primary"
                />
              </Grid>

              <Grid item xs={12}>
                <KPICard
                  title="Max Drawdown"
                  value={Math.round(maxDrawdown / 1000)}
                  prefix="$"
                  suffix="K"
                  isLoading={isLoading}
                  color="error"
                />
              </Grid>

              <Grid item xs={12}>
                <KPICard
                  title="Win Rate"
                  value={winRate.toFixed(1)}
                  suffix="%"
                  isLoading={isLoading}
                  color="success"
                />
              </Grid>

              <Grid item xs={12}>
                <KPICard
                  title="Profit Factor"
                  value={profitFactor.toFixed(2)}
                  isLoading={isLoading}
                  color="success"
                />
              </Grid>

              <Grid item xs={12}>
                <KPICard
                  title="Total Return"
                  value={totalReturn.toFixed(1)}
                  suffix="%"
                  isLoading={isLoading}
                  color={totalReturn >= 0 ? 'success' : 'error'}
                />
              </Grid>

              <Grid item xs={12}>
                <KPICard
                  title="VaR Utilization"
                  value={varUtilization.toFixed(1)}
                  suffix="%"
                  isLoading={isLoading}
                  color={varUtilization > 80 ? 'error' : varUtilization > 60 ? 'warning' : 'success'}
                />
              </Grid>
            </Grid>
          </Grid>
        </Grid>

        <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">
            Period: {data?.period || 'N/A'} | Last Updated: {data?.calculatedAt ? new Date(data.calculatedAt).toLocaleString() : 'N/A'}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}
