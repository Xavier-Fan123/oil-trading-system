import React from 'react'
import {
  ResponsiveContainer,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ComposedChart,
  Line,
} from 'recharts'
import { Card, CardContent, Typography, Box, Chip } from '@mui/material'

interface VaRDataPoint {
  period: string
  portfolioVar95: number
  portfolioVar99: number
  portfolioExpectedShortfall: number
  diversificationBenefit: number
  componentContribution: number
}

interface VaRChartProps {
  data: VaRDataPoint[]
  isLoading?: boolean
  height?: number
}

export const VaRChart: React.FC<VaRChartProps> = ({ 
  data, 
  isLoading = false, 
  height = 350 
}) => {
  const formatTooltipValue = (value: number, name: string) => {
    if (name.includes('Concentration')) {
      return [`${value.toFixed(1)}%`, name]
    }
    return [`$${value.toLocaleString()}K`, name]
  }

  // Risk color functions removed - not used in current implementation

  if (isLoading) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Value at Risk Analysis
          </Typography>
          <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Typography color="text.secondary">Loading VaR data...</Typography>
          </Box>
        </CardContent>
      </Card>
    )
  }

  const latestVar95 = data.length > 0 ? data[data.length - 1].portfolioVar95 : 0
  const latestVar99 = data.length > 0 ? data[data.length - 1].portfolioVar99 : 0
  const avgDiversificationBenefit = data.reduce((sum, item) => sum + item.diversificationBenefit, 0) / (data.length || 1)

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Value at Risk Analysis
        </Typography>
        
        <Box sx={{ display: 'flex', gap: 2, mb: 2, flexWrap: 'wrap' }}>
          <Chip
            label={`Portfolio VaR 95%: $${latestVar95.toLocaleString()}K`}
            color="warning"
            variant="outlined"
          />
          <Chip
            label={`Portfolio VaR 99%: $${latestVar99.toLocaleString()}K`}
            color="error"
            variant="outlined"
          />
          <Chip
            label={`Diversification Benefit: ${avgDiversificationBenefit.toFixed(1)}%`}
            color="success"
            variant="outlined"
          />
        </Box>
        
        <Box sx={{ height }}>
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 60 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2a2d3a" />
              <XAxis 
                dataKey="period" 
                stroke="#b0b0b0"
                tick={{ fill: '#b0b0b0', fontSize: 11 }}
                angle={-45}
                textAnchor="end"
                height={60}
              />
              <YAxis 
                yAxisId="var"
                stroke="#b0b0b0"
                tick={{ fill: '#b0b0b0', fontSize: 12 }}
                tickFormatter={(value) => `$${value}K`}
              />
              <YAxis 
                yAxisId="concentration"
                orientation="right"
                stroke="#b0b0b0"
                tick={{ fill: '#b0b0b0', fontSize: 12 }}
                tickFormatter={(value) => `${value}%`}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#1a1d29',
                  border: '1px solid #2a2d3a',
                  borderRadius: '8px',
                  color: '#ffffff',
                }}
                formatter={formatTooltipValue}
                labelFormatter={(label) => `Product: ${label}`}
              />
              <Legend />
              
              <Bar
                yAxisId="var"
                dataKey="portfolioVar95"
                name="Portfolio VaR 95%"
                radius={[4, 4, 0, 0]}
                fill="#ff9800"
              />
              
              <Bar
                yAxisId="var"
                dataKey="portfolioVar99"
                name="Portfolio VaR 99%"
                radius={[4, 4, 0, 0]}
                fill="#f44336"
                fillOpacity={0.8}
              />
              
              <Bar
                yAxisId="var"
                dataKey="portfolioExpectedShortfall"
                name="Expected Shortfall"
                radius={[4, 4, 0, 0]}
                fill="#9c27b0"
                fillOpacity={0.7}
              />
              
              <Line
                yAxisId="concentration"
                type="monotone"
                dataKey="diversificationBenefit"
                stroke="#4caf50"
                strokeWidth={3}
                dot={{ fill: '#4caf50', strokeWidth: 2, r: 4 }}
                activeDot={{ r: 6, stroke: '#4caf50', strokeWidth: 2 }}
                name="Diversification Benefit %"
              />
            </ComposedChart>
          </ResponsiveContainer>
        </Box>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            Portfolio VaR Analysis - With Correlation Effects
          </Typography>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Box sx={{ width: 12, height: 12, backgroundColor: '#ff9800', borderRadius: '2px' }} />
              <Typography variant="caption" color="text.secondary">
                Portfolio VaR 95%
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Box sx={{ width: 12, height: 12, backgroundColor: '#f44336', borderRadius: '2px' }} />
              <Typography variant="caption" color="text.secondary">
                Portfolio VaR 99%
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Box sx={{ width: 12, height: 12, backgroundColor: '#9c27b0', borderRadius: '2px' }} />
              <Typography variant="caption" color="text.secondary">
                Expected Shortfall
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Box sx={{ width: 12, height: 12, backgroundColor: '#4caf50', borderRadius: '50%' }} />
              <Typography variant="caption" color="text.secondary">
                Diversification Benefit
              </Typography>
            </Box>
          </Box>
          <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
            * Portfolio VaR considers correlations between oil products, providing accurate total risk exposure
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}