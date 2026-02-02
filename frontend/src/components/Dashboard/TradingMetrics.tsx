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
import { useTradingMetrics } from '@/hooks/useDashboard'

export const TradingMetrics: React.FC = () => {
  const { data, isLoading, error } = useTradingMetrics()

  if (error) {
    return (
      <Card>
        <CardContent>
          <Typography color="error">Failed to load trading metrics</Typography>
        </CardContent>
      </Card>
    )
  }

  const totalVolume = data?.totalVolume || 0
  const tradeFrequency = data?.tradeFrequency || 0
  const avgTradeSize = data?.averageTradeSize || 0

  const productEntries = data?.productBreakdown
    ? Object.entries(data.productBreakdown).map(([product, volume]) => ({
        product,
        volume,
        percentage: totalVolume > 0 ? (volume / totalVolume) * 100 : 0,
      })).sort((a, b) => b.volume - a.volume)
    : []

  const counterpartyEntries = data?.counterpartyBreakdown
    ? Object.entries(data.counterpartyBreakdown).map(([name, volume]) => ({
        name,
        volume,
        percentage: totalVolume > 0 ? (volume / totalVolume) * 100 : 0,
      })).sort((a, b) => b.volume - a.volume)
    : []

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Trading Metrics
        </Typography>

        {isLoading && <LinearProgress sx={{ mb: 2 }} />}

        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle1" gutterBottom>
              Volume & Frequency
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Total Volume
              </Typography>
              <Typography variant="h4">
                {totalVolume.toLocaleString()} MT
              </Typography>
            </Box>

            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Trading Frequency
              </Typography>
              <Typography variant="h4">
                {tradeFrequency.toFixed(1)} deals/month
              </Typography>
            </Box>

            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Average Deal Size
              </Typography>
              <Typography variant="h4">
                {avgTradeSize.toLocaleString()} MT
              </Typography>
            </Box>

            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Total Trades
              </Typography>
              <Typography variant="h4">
                {data?.totalTrades || 0}
              </Typography>
            </Box>
          </Grid>

          <Grid item xs={12} md={6}>
            <Typography variant="subtitle1" gutterBottom>
              Product Distribution
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell align="right">Volume</TableCell>
                    <TableCell align="right">Share %</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {productEntries.map((entry, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Chip label={entry.product} size="small" variant="outlined" />
                      </TableCell>
                      <TableCell align="right">
                        {entry.volume.toLocaleString()}
                      </TableCell>
                      <TableCell align="right">
                        {entry.percentage.toFixed(1)}%
                      </TableCell>
                    </TableRow>
                  ))}
                  {productEntries.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={3} align="center">
                        <Typography variant="body2" color="text.secondary">No product data</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>

          <Grid item xs={12}>
            <Typography variant="subtitle1" gutterBottom>
              Counterparty Concentration
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Counterparty</TableCell>
                    <TableCell align="right">Volume</TableCell>
                    <TableCell align="right">Exposure %</TableCell>
                    <TableCell align="right">Risk Level</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {counterpartyEntries.map((cp, index) => {
                    const riskColor = cp.percentage > 20 ? 'error' : cp.percentage > 10 ? 'warning' : 'success'
                    return (
                      <TableRow key={index}>
                        <TableCell>{cp.name}</TableCell>
                        <TableCell align="right">{cp.volume.toLocaleString()}</TableCell>
                        <TableCell align="right">{cp.percentage.toFixed(1)}%</TableCell>
                        <TableCell align="right">
                          <Chip
                            label={riskColor === 'error' ? 'High' : riskColor === 'warning' ? 'Medium' : 'Low'}
                            size="small"
                            color={riskColor}
                          />
                        </TableCell>
                      </TableRow>
                    )
                  })}
                  {counterpartyEntries.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={4} align="center">
                        <Typography variant="body2" color="text.secondary">No counterparty data</Typography>
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
            Period: {data?.period || 'N/A'} | Last Updated: {data?.calculatedAt ? new Date(data.calculatedAt).toLocaleString() : 'N/A'}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}
