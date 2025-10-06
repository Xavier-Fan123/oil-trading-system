import React from 'react'
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
} from 'recharts'
import { Card, CardContent, Typography, Box, Grid, Chip } from '@mui/material'

interface PositionData {
  productType: string
  exposure: number
  percentage: number
  pnlContribution: number
  contracts: number
  avgPrice: number
}

interface PositionChartProps {
  data: PositionData[]
  totalExposure: number
  isLoading?: boolean
  height?: number
}

const COLORS = [
  '#2196f3', '#4caf50', '#ff9800', '#f44336', '#9c27b0',
  '#00bcd4', '#8bc34a', '#ffc107', '#e91e63', '#3f51b5'
]

export const PositionChart: React.FC<PositionChartProps> = ({ 
  data, 
  totalExposure,
  isLoading = false, 
  height = 350 
}) => {
  const formatTooltipValue = (value: number, name: string, props: any) => {
    const { payload } = props
    if (name === 'exposure') {
      return [
        `$${value.toLocaleString()}M (${payload.percentage.toFixed(1)}%)`,
        'Exposure'
      ]
    }
    if (name === 'pnlContribution') {
      return [`$${value.toLocaleString()}K`, 'P&L Contribution']
    }
    if (name === 'contracts') {
      return [`${value} contracts`, 'Active Contracts']
    }
    return [value, name]
  }

  const renderCustomLabel = ({ cx, cy, midAngle, innerRadius, outerRadius, percentage }: any) => {
    if (percentage < 5) return null // Don't show labels for small slices
    
    const RADIAN = Math.PI / 180
    const radius = innerRadius + (outerRadius - innerRadius) * 0.5
    const x = cx + radius * Math.cos(-midAngle * RADIAN)
    const y = cy + radius * Math.sin(-midAngle * RADIAN)

    return (
      <text 
        x={x} 
        y={y} 
        fill="white" 
        textAnchor={x > cx ? 'start' : 'end'} 
        dominantBaseline="central"
        fontSize={12}
        fontWeight="bold"
      >
        {`${percentage.toFixed(1)}%`}
      </text>
    )
  }

  if (isLoading) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Position Distribution
          </Typography>
          <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Typography color="text.secondary">Loading position data...</Typography>
          </Box>
        </CardContent>
      </Card>
    )
  }

  const sortedData = [...data].sort((a, b) => b.exposure - a.exposure)
  const topPositions = sortedData.slice(0, 5)
  const otherPositions = sortedData.slice(5)
  const otherTotal = otherPositions.reduce((sum, pos) => sum + pos.exposure, 0)
  
  const pieData = [...topPositions]
  if (otherTotal > 0) {
    pieData.push({
      productType: 'Others',
      exposure: otherTotal,
      percentage: (otherTotal / totalExposure) * 100,
      pnlContribution: otherPositions.reduce((sum, pos) => sum + pos.pnlContribution, 0),
      contracts: otherPositions.reduce((sum, pos) => sum + pos.contracts, 0),
      avgPrice: 0
    })
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Position Distribution
        </Typography>
        
        <Box sx={{ mb: 2, display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <Chip
            label={`Total Exposure: $${totalExposure.toLocaleString()}M`}
            color="primary"
            variant="outlined"
          />
          <Chip
            label={`Active Positions: ${data.length}`}
            color="info"
            variant="outlined"
          />
          <Chip
            label={`Largest Position: ${Math.max(...data.map(d => d.percentage)).toFixed(1)}%`}
            color={Math.max(...data.map(d => d.percentage)) > 25 ? 'warning' : 'success'}
            variant="outlined"
          />
        </Box>
        
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle2" gutterBottom>
              Exposure by Product
            </Typography>
            <Box sx={{ height: height * 0.8 }}>
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={pieData}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={renderCustomLabel}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="exposure"
                  >
                    {pieData.map((_entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1a1d29',
                      border: '1px solid #2a2d3a',
                      borderRadius: '8px',
                      color: '#ffffff',
                    }}
                    formatter={formatTooltipValue}
                  />
                </PieChart>
              </ResponsiveContainer>
            </Box>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle2" gutterBottom>
              P&L Contribution
            </Typography>
            <Box sx={{ height: height * 0.8 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={sortedData.slice(0, 6)} layout="horizontal">
                  <CartesianGrid strokeDasharray="3 3" stroke="#2a2d3a" />
                  <XAxis 
                    type="number"
                    stroke="#b0b0b0"
                    tick={{ fill: '#b0b0b0', fontSize: 10 }}
                    tickFormatter={(value) => `$${value}K`}
                  />
                  <YAxis 
                    type="category"
                    dataKey="productType"
                    stroke="#b0b0b0"
                    tick={{ fill: '#b0b0b0', fontSize: 10 }}
                    width={60}
                  />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#1a1d29',
                      border: '1px solid #2a2d3a',
                      borderRadius: '8px',
                      color: '#ffffff',
                    }}
                    formatter={(value: number) => [`$${value.toLocaleString()}K`, 'P&L Contribution']}
                  />
                  <Bar 
                    dataKey="pnlContribution" 
                    radius={[0, 4, 4, 0]}
                  >
                    {sortedData.slice(0, 6).map((entry, index) => (
                      <Cell 
                        key={`pnl-${index}`} 
                        fill={entry.pnlContribution >= 0 ? '#4caf50' : '#f44336'} 
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </Box>
          </Grid>
        </Grid>
        
        <Box sx={{ mt: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            Position Details
          </Typography>
          <Grid container spacing={1}>
            {pieData.map((position, index) => (
              <Grid item xs={12} sm={6} md={4} key={position.productType}>
                <Box 
                  sx={{ 
                    display: 'flex', 
                    alignItems: 'center', 
                    gap: 1,
                    p: 1,
                    backgroundColor: 'background.paper',
                    borderRadius: 1,
                    border: 1,
                    borderColor: 'divider'
                  }}
                >
                  <Box 
                    sx={{ 
                      width: 12, 
                      height: 12, 
                      backgroundColor: COLORS[index % COLORS.length], 
                      borderRadius: '50%',
                      flexShrink: 0
                    }} 
                  />
                  <Box sx={{ minWidth: 0, flex: 1 }}>
                    <Typography variant="caption" fontWeight="medium" noWrap>
                      {position.productType}
                    </Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                      <Typography variant="caption" color="text.secondary">
                        ${position.exposure.toFixed(1)}M
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {position.percentage.toFixed(1)}%
                      </Typography>
                    </Box>
                  </Box>
                </Box>
              </Grid>
            ))}
          </Grid>
        </Box>
      </CardContent>
    </Card>
  )
}