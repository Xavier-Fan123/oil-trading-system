import React from 'react'
import {
  ResponsiveContainer,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ComposedChart,
  Bar,
} from 'recharts'
import { Card, CardContent, Typography, Box } from '@mui/material'

interface PnLDataPoint {
  date: string
  dailyPnL: number
  cumulativePnL: number
  unrealizedPnL: number
  volume: number
}

interface PnLChartProps {
  data: PnLDataPoint[]
  isLoading?: boolean
  height?: number
}

export const PnLChart: React.FC<PnLChartProps> = ({ 
  data, 
  isLoading = false, 
  height = 350 
}) => {
  const formatTooltipValue = (value: number, name: string) => {
    if (name.includes('Volume')) {
      return [`${value.toLocaleString()} MT`, name]
    }
    return [`$${value.toLocaleString()}K`, name]
  }

  const formatXAxisLabel = (value: string) => {
    return new Date(value).toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric' 
    })
  }

  if (isLoading) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            P&L Trend Analysis
          </Typography>
          <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Typography color="text.secondary">Loading chart data...</Typography>
          </Box>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          P&L Trend Analysis
        </Typography>
        
        <Box sx={{ height }}>
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2a2d3a" />
              <XAxis 
                dataKey="date" 
                stroke="#b0b0b0"
                tick={{ fill: '#b0b0b0', fontSize: 12 }}
                tickFormatter={formatXAxisLabel}
              />
              <YAxis 
                yAxisId="pnl"
                stroke="#b0b0b0"
                tick={{ fill: '#b0b0b0', fontSize: 12 }}
                tickFormatter={(value) => `$${value}K`}
              />
              <YAxis 
                yAxisId="volume"
                orientation="right"
                stroke="#b0b0b0"
                tick={{ fill: '#b0b0b0', fontSize: 12 }}
                tickFormatter={(value) => `${value}K MT`}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#1a1d29',
                  border: '1px solid #2a2d3a',
                  borderRadius: '8px',
                  color: '#ffffff',
                }}
                formatter={formatTooltipValue}
                labelFormatter={(label) => `Date: ${new Date(label).toLocaleDateString()}`}
              />
              <Legend />
              
              <Bar
                yAxisId="volume"
                dataKey="volume"
                fill="#424242"
                fillOpacity={0.3}
                name="Trading Volume"
              />
              
              <Line
                yAxisId="pnl"
                type="monotone"
                dataKey="dailyPnL"
                stroke="#2196f3"
                strokeWidth={2}
                dot={{ fill: '#2196f3', strokeWidth: 2, r: 3 }}
                activeDot={{ r: 5, stroke: '#2196f3', strokeWidth: 2 }}
                name="Daily P&L"
              />
              
              <Line
                yAxisId="pnl"
                type="monotone"
                dataKey="cumulativePnL"
                stroke="#4caf50"
                strokeWidth={3}
                dot={{ fill: '#4caf50', strokeWidth: 2, r: 3 }}
                activeDot={{ r: 5, stroke: '#4caf50', strokeWidth: 2 }}
                name="Cumulative P&L"
              />
              
              <Line
                yAxisId="pnl"
                type="monotone"
                dataKey="unrealizedPnL"
                stroke="#ff9800"
                strokeWidth={2}
                strokeDasharray="5 5"
                dot={{ fill: '#ff9800', strokeWidth: 2, r: 3 }}
                activeDot={{ r: 5, stroke: '#ff9800', strokeWidth: 2 }}
                name="Unrealized P&L"
              />
            </ComposedChart>
          </ResponsiveContainer>
        </Box>
        
        <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box sx={{ width: 12, height: 12, backgroundColor: '#2196f3', borderRadius: '50%' }} />
            <Typography variant="caption" color="text.secondary">
              Daily P&L
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box sx={{ width: 12, height: 12, backgroundColor: '#4caf50', borderRadius: '50%' }} />
            <Typography variant="caption" color="text.secondary">
              Cumulative P&L
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box sx={{ width: 12, height: 12, backgroundColor: '#ff9800', borderRadius: '50%' }} />
            <Typography variant="caption" color="text.secondary">
              Unrealized P&L
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Box sx={{ width: 12, height: 12, backgroundColor: '#424242', borderRadius: '2px' }} />
            <Typography variant="caption" color="text.secondary">
              Trading Volume
            </Typography>
          </Box>
        </Box>
      </CardContent>
    </Card>
  )
}