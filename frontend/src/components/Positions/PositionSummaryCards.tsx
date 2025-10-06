import React from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  LinearProgress,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  AccountBalance as BalanceIcon,
  ShowChart as ChartIcon,
} from '@mui/icons-material';
import { PositionSummary } from '@/types/positions';

interface PositionSummaryCardsProps {
  summary: PositionSummary;
  isLoading?: boolean;
}

const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
};

const formatPercentage = (value: number): string => {
  return `${(value * 100).toFixed(2)}%`;
};

export const PositionSummaryCards: React.FC<PositionSummaryCardsProps> = ({ 
  summary, 
  isLoading = false 
}) => {
  if (isLoading) {
    return (
      <Grid container spacing={3}>
        {Array.from({ length: 4 }).map((_, index) => (
          <Grid item xs={12} sm={6} md={3} key={index}>
            <Card>
              <CardContent>
                <LinearProgress />
                <Box mt={2}>
                  <Typography variant="body2">Loading...</Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    );
  }

  const pnlColor = summary.totalPnL >= 0 ? 'success.main' : 'error.main';
  const pnlIcon = summary.totalPnL >= 0 ? <TrendingUpIcon /> : <TrendingDownIcon />;

  return (
    <Grid container spacing={3}>
      {/* Total Positions */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent>
            <Box display="flex" alignItems="center" mb={1}>
              <BalanceIcon color="primary" sx={{ mr: 1 }} />
              <Typography variant="subtitle2" color="textSecondary">
                Total Positions
              </Typography>
            </Box>
            <Typography variant="h4" component="div" gutterBottom>
              {summary.totalPositions}
            </Typography>
            <Typography variant="body2" color="textSecondary">
              Active positions
            </Typography>
          </CardContent>
        </Card>
      </Grid>

      {/* Net Exposure */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent>
            <Box display="flex" alignItems="center" mb={1}>
              <ChartIcon color="info" sx={{ mr: 1 }} />
              <Typography variant="subtitle2" color="textSecondary">
                Net Exposure
              </Typography>
            </Box>
            <Typography variant="h5" component="div" gutterBottom>
              {formatCurrency(summary.netExposure)}
            </Typography>
            <Box display="flex" gap={1} mt={1}>
              <Chip 
                label={`Long: ${formatCurrency(summary.longExposure)}`}
                size="small"
                color="success"
                variant="outlined"
              />
              <Chip 
                label={`Short: ${formatCurrency(Math.abs(summary.shortExposure))}`}
                size="small"
                color="error"
                variant="outlined"
              />
            </Box>
          </CardContent>
        </Card>
      </Grid>

      {/* Total P&L */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent>
            <Box display="flex" alignItems="center" mb={1}>
              {pnlIcon}
              <Typography variant="subtitle2" color="textSecondary" sx={{ ml: 1 }}>
                Total P&L
              </Typography>
            </Box>
            <Typography 
              variant="h5" 
              component="div" 
              sx={{ color: pnlColor }}
              gutterBottom
            >
              {formatCurrency(summary.totalPnL)}
            </Typography>
            <Box display="flex" gap={1} mt={1}>
              <Typography variant="caption" color="textSecondary">
                Unrealized: {formatCurrency(summary.unrealizedPnL)}
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      {/* Portfolio Risk */}
      <Grid item xs={12} sm={6} md={3}>
        <Card>
          <CardContent>
            <Box display="flex" alignItems="center" mb={1}>
              <ChartIcon color="warning" sx={{ mr: 1 }} />
              <Typography variant="subtitle2" color="textSecondary">
                Portfolio VaR
              </Typography>
            </Box>
            <Typography variant="h6" component="div" gutterBottom>
              {formatCurrency(summary.riskMetrics.portfolioVaR95)}
            </Typography>
            <Typography variant="body2" color="textSecondary">
              95% confidence (1-day)
            </Typography>
            <Box mt={1}>
              <Typography variant="caption" color="textSecondary">
                Volatility: {formatPercentage(summary.riskMetrics.portfolioVolatility)}
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );
};