import React from 'react';
import {
  Card,
  CardContent,
  CardHeader,
  Box,
  Grid,
  Typography,
  LinearProgress,
  Chip,
  Alert,
  Button,
  Stack,
} from '@mui/material';
import { TradingPartnerExposureDto } from '@/services/tradingPartnerExposureApi';
import RiskLevelBadge from './RiskLevelBadge';
import { formatCurrency, formatPercentage } from '@/utils/formatting';

interface ExposureCardProps {
  exposure: TradingPartnerExposureDto;
  onViewDetails?: (partnerId: string) => void;
}

export const ExposureCard: React.FC<ExposureCardProps> = ({
  exposure,
  onViewDetails,
}) => {
  const creditUsageColor =
    exposure.creditUtilizationPercentage > 100
      ? '#F44336'
      : exposure.creditUtilizationPercentage > 85
      ? '#FF9800'
      : exposure.creditUtilizationPercentage > 60
      ? '#FFC107'
      : '#4CAF50';

  const isBlocked = exposure.isBlocked || exposure.isCreditExpired || exposure.isOverLimit;

  return (
    <Card sx={{ height: '100%' }}>
      <CardHeader
        title={
          <Box display="flex" alignItems="center" gap={1}>
            <Typography variant="subtitle1" fontWeight="bold">
              {exposure.companyName}
            </Typography>
            <RiskLevelBadge
              riskLevel={exposure.riskLevel}
              riskLevelDescription={exposure.riskLevelDescription}
            />
          </Box>
        }
        subheader={`Code: ${exposure.companyCode}`}
        sx={{ pb: 1 }}
      />

      <CardContent>
        {/* Alerts */}
        {isBlocked && (
          <Stack spacing={1} sx={{ mb: 2 }}>
            {exposure.isBlocked && (
              <Alert severity="error">
                Partner is blocked. Reason: {exposure.blockReason || 'Not specified'}
              </Alert>
            )}
            {exposure.isCreditExpired && (
              <Alert severity="warning">Credit limit expired</Alert>
            )}
            {exposure.isOverLimit && (
              <Alert severity="warning">Credit limit exceeded</Alert>
            )}
          </Stack>
        )}

        {/* Credit Utilization */}
        <Box sx={{ mb: 2 }}>
          <Box display="flex" justifyContent="space-between" sx={{ mb: 1 }}>
            <Typography variant="body2" fontWeight="bold">
              Credit Utilization
            </Typography>
            <Typography variant="body2" fontWeight="bold" sx={{ color: creditUsageColor }}>
              {formatPercentage(exposure.creditUtilizationPercentage)}
            </Typography>
          </Box>
          <LinearProgress
            variant="determinate"
            value={Math.min(exposure.creditUtilizationPercentage, 100)}
            sx={{
              height: 8,
              backgroundColor: '#E0E0E0',
              '& .MuiLinearProgress-bar': {
                backgroundColor: creditUsageColor,
              },
            }}
          />
        </Box>

        {/* Credit Summary Grid */}
        <Grid container spacing={1} sx={{ mb: 2 }}>
          <Grid item xs={6}>
            <Box
              sx={{
                p: 1,
                backgroundColor: '#F5F5F5',
                borderRadius: 1,
                textAlign: 'center',
              }}
            >
              <Typography variant="caption" color="textSecondary">
                Credit Limit
              </Typography>
              <Typography variant="body2" fontWeight="bold">
                {formatCurrency(exposure.creditLimit)}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={6}>
            <Box
              sx={{
                p: 1,
                backgroundColor: '#F5F5F5',
                borderRadius: 1,
                textAlign: 'center',
              }}
            >
              <Typography variant="caption" color="textSecondary">
                Current Exposure
              </Typography>
              <Typography variant="body2" fontWeight="bold" sx={{ color: creditUsageColor }}>
                {formatCurrency(exposure.currentExposure)}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={6}>
            <Box
              sx={{
                p: 1,
                backgroundColor: '#F5F5F5',
                borderRadius: 1,
                textAlign: 'center',
              }}
            >
              <Typography variant="caption" color="textSecondary">
                Available Credit
              </Typography>
              <Typography variant="body2" fontWeight="bold">
                {formatCurrency(exposure.availableCredit)}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={6}>
            <Box
              sx={{
                p: 1,
                backgroundColor: '#F5F5F5',
                borderRadius: 1,
                textAlign: 'center',
              }}
            >
              <Typography variant="caption" color="textSecondary">
                Net Exposure
              </Typography>
              <Typography variant="body2" fontWeight="bold">
                {formatCurrency(exposure.netExposure)}
              </Typography>
            </Box>
          </Grid>
        </Grid>

        {/* Outstanding & Overdue */}
        <Grid container spacing={1} sx={{ mb: 2 }}>
          <Grid item xs={12}>
            <Typography variant="caption" fontWeight="bold" color="textSecondary">
              Outstanding Amounts
            </Typography>
          </Grid>
          <Grid item xs={6}>
            <Box sx={{ p: 1, backgroundColor: '#E3F2FD', borderRadius: 1 }}>
              <Typography variant="caption" color="textSecondary">
                We Owe (AP)
              </Typography>
              <Typography variant="body2" fontWeight="bold">
                {formatCurrency(exposure.outstandingApAmount)}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={6}>
            <Box sx={{ p: 1, backgroundColor: '#F3E5F5', borderRadius: 1 }}>
              <Typography variant="caption" color="textSecondary">
                They Owe (AR)
              </Typography>
              <Typography variant="body2" fontWeight="bold">
                {formatCurrency(exposure.outstandingArAmount)}
              </Typography>
            </Box>
          </Grid>
        </Grid>

        {/* Overdue Information */}
        {exposure.overdueApAmount > 0 && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            {exposure.overdueSettlementCount} overdue payment(s) totaling{' '}
            {formatCurrency(exposure.overdueApAmount)}
          </Alert>
        )}

        {/* Settlement Statistics */}
        <Box
          sx={{
            p: 1,
            backgroundColor: '#F5F5F5',
            borderRadius: 1,
            mb: 2,
          }}
        >
          <Typography variant="caption" fontWeight="bold" color="textSecondary">
            Settlement Statistics
          </Typography>
          <Box display="flex" justifyContent="space-between" sx={{ mt: 0.5 }}>
            <Typography variant="body2">
              Total Unpaid: {exposure.totalUnpaidSettlements}
            </Typography>
            <Typography variant="body2">
              Due in 30 days: {exposure.settlementsDueIn30Days}
            </Typography>
          </Box>
        </Box>

        {/* Action Button */}
        {onViewDetails && (
          <Button
            variant="outlined"
            size="small"
            fullWidth
            onClick={() => onViewDetails(exposure.tradingPartnerId)}
          >
            View Details
          </Button>
        )}
      </CardContent>
    </Card>
  );
};

export default ExposureCard;
