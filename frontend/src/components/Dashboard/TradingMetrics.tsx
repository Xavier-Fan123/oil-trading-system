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
  const { data: _data, isLoading, error } = useTradingMetrics()

  if (error) {
    return (
      <Card>
        <CardContent>
          <Typography color="error">Failed to load trading metrics</Typography>
        </CardContent>
      </Card>
    )
  }

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
                0 MT
              </Typography>
            </Box>
            
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Trading Frequency
              </Typography>
              <Typography variant="h4">
                0 deals/month
              </Typography>
            </Box>
            
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2" color="text.secondary">
                Average Deal Size
              </Typography>
              <Typography variant="h4">
                $0K
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
                    <TableCell align="right">Volume %</TableCell>
                    <TableCell align="right">P&L Contrib.</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {[]?.map((product: any, index: number) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Chip 
                          label={product.productType} 
                          size="small" 
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell align="right">
                        {product.volumePercentage.toFixed(1)}%
                      </TableCell>
                      <TableCell align="right">
                        <Typography
                          color={product.pnlContribution >= 0 ? 'success.main' : 'error.main'}
                        >
                          ${product.pnlContribution.toLocaleString()}K
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )) || []}
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
                    <TableCell align="right">Exposure %</TableCell>
                    <TableCell align="center">Credit Rating</TableCell>
                    <TableCell align="right">Risk Level</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {[]?.map((cp: any, index: number) => {
                    const getRiskColor = (exposure: number, rating: string) => {
                      if (exposure > 20 || rating.includes('C')) return 'error'
                      if (exposure > 10 || rating.includes('B')) return 'warning'
                      return 'success'
                    }
                    
                    return (
                      <TableRow key={index}>
                        <TableCell>{cp.counterpartyName}</TableCell>
                        <TableCell align="right">
                          {cp.exposurePercentage.toFixed(1)}%
                        </TableCell>
                        <TableCell align="center">
                          <Chip 
                            label={cp.creditRating} 
                            size="small"
                            color={cp.creditRating.includes('A') ? 'success' : 'warning'}
                          />
                        </TableCell>
                        <TableCell align="right">
                          <Chip
                            label={
                              getRiskColor(cp.exposurePercentage, cp.creditRating) === 'error' 
                                ? 'High' 
                                : getRiskColor(cp.exposurePercentage, cp.creditRating) === 'warning'
                                ? 'Medium'
                                : 'Low'
                            }
                            size="small"
                            color={getRiskColor(cp.exposurePercentage, cp.creditRating)}
                          />
                        </TableCell>
                      </TableRow>
                    )
                  }) || []}
                </TableBody>
              </Table>
            </TableContainer>
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