import React from 'react'
import {
  Card,
  CardContent,
  Typography,
  Grid,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Box,
  LinearProgress,
  Alert,
} from '@mui/material'
import { TrendingUp, TrendingDown } from '@mui/icons-material'
import { useMarketInsights } from '@/hooks/useDashboard'

export const MarketInsights: React.FC = () => {
  const { data: _data, isLoading, error } = useMarketInsights()

  if (error) {
    return (
      <Card>
        <CardContent>
          <Typography color="error">Failed to load market insights</Typography>
        </CardContent>
      </Card>
    )
  }

  const formatPrice = (price: number) => {
    return `$${price.toFixed(2)}`
  }

  const getVolatilityColor = (vol: number) => {
    if (vol > 30) return 'error'
    if (vol > 20) return 'warning'
    return 'success'
  }

  const getCorrelationColor = (corr: number) => {
    if (Math.abs(corr) > 0.7) return 'error'
    if (Math.abs(corr) > 0.4) return 'warning'
    return 'success'
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Market Insights
        </Typography>
        
        {isLoading && <LinearProgress sx={{ mb: 2 }} />}
        
        <Grid container spacing={3}>
          <Grid item xs={12} lg={6}>
            <Typography variant="subtitle1" gutterBottom>
              Benchmark Prices
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Benchmark</TableCell>
                    <TableCell align="right">Price</TableCell>
                    <TableCell align="right">24h Change</TableCell>
                    <TableCell align="right">% Change</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {[]?.map((price: any, index: number) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography fontWeight="medium">
                          {price.benchmark}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        {formatPrice(price.currentPrice)}
                      </TableCell>
                      <TableCell align="right">
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end' }}>
                          {price.change24h >= 0 ? (
                            <TrendingUp sx={{ color: 'success.main', mr: 0.5, fontSize: 16 }} />
                          ) : (
                            <TrendingDown sx={{ color: 'error.main', mr: 0.5, fontSize: 16 }} />
                          )}
                          <Typography
                            color={price.change24h >= 0 ? 'success.main' : 'error.main'}
                          >
                            {formatPrice(price.change24h)}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell align="right">
                        <Typography
                          color={price.changePercent24h >= 0 ? 'success.main' : 'error.main'}
                        >
                          {price.changePercent24h >= 0 ? '+' : ''}{price.changePercent24h.toFixed(2)}%
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )) || []}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
          
          <Grid item xs={12} lg={6}>
            <Typography variant="subtitle1" gutterBottom>
              Volatility Analysis
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell align="right">Implied Vol</TableCell>
                    <TableCell align="right">Historical Vol</TableCell>
                    <TableCell align="center">Trend</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {[]?.map((vol: any, index: number) => (
                    <TableRow key={index}>
                      <TableCell>{vol.product}</TableCell>
                      <TableCell align="right">
                        <Chip
                          label={`${vol.impliedVolatility.toFixed(1)}%`}
                          size="small"
                          color={getVolatilityColor(vol.impliedVolatility)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Chip
                          label={`${vol.historicalVolatility.toFixed(1)}%`}
                          size="small"
                          color={getVolatilityColor(vol.historicalVolatility)}
                        />
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={vol.volatilityTrend}
                          size="small"
                          variant="outlined"
                          color={vol.volatilityTrend === 'Rising' ? 'error' : 'success'}
                        />
                      </TableCell>
                    </TableRow>
                  )) || []}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
          
          <Grid item xs={12} lg={6}>
            <Typography variant="subtitle1" gutterBottom>
              Correlation Matrix
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product Pair</TableCell>
                    <TableCell align="right">Correlation</TableCell>
                    <TableCell align="center">Trend</TableCell>
                    <TableCell align="center">Risk Level</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {[]?.map((corr: any, index: number) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography variant="body2">
                          {corr.product1} - {corr.product2}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        {corr.correlation.toFixed(3)}
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={corr.trend}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={
                            Math.abs(corr.correlation) > 0.7 ? 'High' :
                            Math.abs(corr.correlation) > 0.4 ? 'Medium' : 'Low'
                          }
                          size="small"
                          color={getCorrelationColor(corr.correlation)}
                        />
                      </TableCell>
                    </TableRow>
                  )) || []}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
          
          <Grid item xs={12} lg={6}>
            <Typography variant="subtitle1" gutterBottom>
              Market Sentiment & Risk Factors
            </Typography>
            
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Current Sentiment
              </Typography>
              <Chip
                label={'Neutral'}
                size="medium"
                color={
                  'info'
                }
              />
            </Box>
            
            <Typography variant="body2" color="text.secondary" gutterBottom>
              Key Risk Factors
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {[]?.map((factor: any, index: number) => (
                <Alert key={index} severity="warning" variant="outlined">
                  {factor}
                </Alert>
              )) || []}
            </Box>
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