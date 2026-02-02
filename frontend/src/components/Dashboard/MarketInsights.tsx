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
} from '@mui/material'
import { TrendingUp, TrendingDown } from '@mui/icons-material'
import { useMarketInsights } from '@/hooks/useDashboard'

export const MarketInsights: React.FC = () => {
  const { data, isLoading, error } = useMarketInsights()

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

  const getVolatilityColor = (vol: number): 'error' | 'warning' | 'success' => {
    if (vol > 30) return 'error'
    if (vol > 20) return 'warning'
    return 'success'
  }

  const getCorrelationColor = (corr: number): 'error' | 'warning' | 'success' => {
    if (Math.abs(corr) > 0.7) return 'error'
    if (Math.abs(corr) > 0.4) return 'warning'
    return 'success'
  }

  // Transform key prices from API
  const keyPrices = data?.keyPrices || []

  // Transform volatility indicators (Record<string, number>) to array
  const volatilityEntries = data?.volatilityIndicators
    ? Object.entries(data.volatilityIndicators).map(([product, value]) => ({
        product,
        volatility: value,
      }))
    : []

  // Transform correlation matrix to unique pairs (upper triangle only)
  const correlationPairs: { product1: string; product2: string; correlation: number }[] = []
  if (data?.correlationMatrix) {
    const products = Object.keys(data.correlationMatrix)
    for (let i = 0; i < products.length; i++) {
      for (let j = i + 1; j < products.length; j++) {
        const corr = data.correlationMatrix[products[i]]?.[products[j]]
        if (corr !== undefined) {
          correlationPairs.push({
            product1: products[i],
            product2: products[j],
            correlation: corr,
          })
        }
      }
    }
  }

  // Sentiment analysis from API
  const sentimentIndicators = data?.sentimentIndicators || {}
  const overallSentiment = sentimentIndicators['overallSentiment'] || 0
  const sentimentLabel = overallSentiment > 0.6 ? 'Bullish' : overallSentiment < 0.4 ? 'Bearish' : 'Neutral'
  const sentimentColor: 'success' | 'error' | 'info' =
    overallSentiment > 0.6 ? 'success' : overallSentiment < 0.4 ? 'error' : 'info'

  // Market trends from API
  const marketTrends = data?.marketTrends || []

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
                    <TableCell align="right">Change</TableCell>
                    <TableCell align="right">% Change</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {keyPrices.map((price, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography fontWeight="medium">
                          {price.product}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        {formatPrice(price.price)}
                      </TableCell>
                      <TableCell align="right">
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end' }}>
                          {price.change >= 0 ? (
                            <TrendingUp sx={{ color: 'success.main', mr: 0.5, fontSize: 16 }} />
                          ) : (
                            <TrendingDown sx={{ color: 'error.main', mr: 0.5, fontSize: 16 }} />
                          )}
                          <Typography
                            color={price.change >= 0 ? 'success.main' : 'error.main'}
                          >
                            {price.change >= 0 ? '+' : ''}{formatPrice(Math.abs(price.change))}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell align="right">
                        <Typography
                          color={price.changePercent >= 0 ? 'success.main' : 'error.main'}
                        >
                          {price.changePercent >= 0 ? '+' : ''}{price.changePercent.toFixed(2)}%
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ))}
                  {keyPrices.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4} align="center">
                        <Typography variant="body2" color="text.secondary">No price data available</Typography>
                      </TableCell>
                    </TableRow>
                  )}
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
                    <TableCell align="right">Annualized Vol</TableCell>
                    <TableCell align="center">Level</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {volatilityEntries.map((entry, index) => (
                    <TableRow key={index}>
                      <TableCell>{entry.product}</TableCell>
                      <TableCell align="right">
                        <Chip
                          label={`${entry.volatility.toFixed(1)}%`}
                          size="small"
                          color={getVolatilityColor(entry.volatility)}
                        />
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={entry.volatility > 30 ? 'High' : entry.volatility > 20 ? 'Medium' : 'Low'}
                          size="small"
                          variant="outlined"
                          color={getVolatilityColor(entry.volatility)}
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                  {volatilityEntries.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={3} align="center">
                        <Typography variant="body2" color="text.secondary">No volatility data</Typography>
                      </TableCell>
                    </TableRow>
                  )}
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
                    <TableCell align="center">Risk Level</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {correlationPairs.map((corr, index) => (
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
                          label={
                            Math.abs(corr.correlation) > 0.7 ? 'High' :
                            Math.abs(corr.correlation) > 0.4 ? 'Medium' : 'Low'
                          }
                          size="small"
                          color={getCorrelationColor(corr.correlation)}
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                  {correlationPairs.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={3} align="center">
                        <Typography variant="body2" color="text.secondary">No correlation data</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>

          <Grid item xs={12} lg={6}>
            <Typography variant="subtitle1" gutterBottom>
              Market Sentiment & Trends
            </Typography>

            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Overall Sentiment
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Chip
                  label={sentimentLabel}
                  size="medium"
                  color={sentimentColor}
                />
                {sentimentIndicators['bullishRatio'] !== undefined && (
                  <Typography variant="caption" color="text.secondary">
                    Bull: {((sentimentIndicators['bullishRatio'] || 0) * 100).toFixed(0)}% /
                    Bear: {((sentimentIndicators['bearishRatio'] || 0) * 100).toFixed(0)}%
                  </Typography>
                )}
              </Box>
            </Box>

            <Typography variant="body2" color="text.secondary" gutterBottom>
              Market Trends
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell align="center">Trend</TableCell>
                    <TableCell align="right">Strength</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {marketTrends.map((trend, index) => (
                    <TableRow key={index}>
                      <TableCell>{trend.product}</TableCell>
                      <TableCell align="center">
                        <Chip
                          label={trend.trend}
                          size="small"
                          color={trend.trend === 'Bullish' ? 'success' : trend.trend === 'Bearish' ? 'error' : 'info'}
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell align="right">
                        {(trend.strength * 100).toFixed(0)}%
                      </TableCell>
                    </TableRow>
                  ))}
                  {marketTrends.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={3} align="center">
                        <Typography variant="body2" color="text.secondary">No trend data</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
        </Grid>

        <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">
            Market Data Points: {data?.marketDataCount || 0} | Last Updated: {data?.calculatedAt ? new Date(data.calculatedAt).toLocaleString() : 'N/A'}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}
