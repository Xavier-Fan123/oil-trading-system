import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Grid,
  Paper,
  Typography,
  Box,
  CircularProgress,
  Divider,
  Stack,
} from '@mui/material';
import {
  TradingPartnerExposureDto,
  PartnerSettlementSummaryDto,
} from '@/services/tradingPartnerExposureApi';
import { formatCurrency, formatDate } from '@/utils/formatting';

interface SettlementDetailsDialogProps {
  open: boolean;
  partner: TradingPartnerExposureDto;
  settlementDetails?: PartnerSettlementSummaryDto;
  isLoading: boolean;
  onClose: () => void;
}

export const SettlementDetailsDialog: React.FC<SettlementDetailsDialogProps> = ({
  open,
  partner,
  settlementDetails,
  isLoading,
  onClose,
}) => {
  const getNetDirection = (direction: string) => {
    switch (direction) {
      case 'We Owe':
        return { color: '#FF9800', icon: '↓' };
      case 'They Owe Us':
        return { color: '#4CAF50', icon: '↑' };
      case 'Balanced':
        return { color: '#9E9E9E', icon: '=' };
      default:
        return { color: '#000', icon: '' };
    }
  };

  const netDirection = settlementDetails
    ? getNetDirection(settlementDetails.netDirection)
    : { color: '#000', icon: '' };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">{partner.companyName}</Typography>
          <Typography variant="caption" color="textSecondary">
            {partner.companyCode}
          </Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        {isLoading ? (
          <Box display="flex" justifyContent="center" sx={{ py: 4 }}>
            <CircularProgress />
          </Box>
        ) : settlementDetails ? (
          <Stack spacing={2}>
            {/* Partner Overview */}
            <Paper sx={{ p: 2, backgroundColor: '#F5F5F5' }}>
              <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 1 }}>
                Partner Information
              </Typography>
              <Grid container spacing={1}>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">
                    Credit Limit
                  </Typography>
                  <Typography variant="body2" fontWeight="bold">
                    {formatCurrency(partner.creditLimit)}
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">
                    Current Exposure
                  </Typography>
                  <Typography variant="body2" fontWeight="bold">
                    {formatCurrency(partner.currentExposure)}
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">
                    Credit Limit Valid Until
                  </Typography>
                  <Typography variant="body2" fontWeight="bold">
                    {formatDate(partner.creditLimitValidUntil)}
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="textSecondary">
                    Last Transaction
                  </Typography>
                  <Typography variant="body2" fontWeight="bold">
                    {partner.lastTransactionDate
                      ? formatDate(partner.lastTransactionDate)
                      : 'N/A'}
                  </Typography>
                </Grid>
              </Grid>
            </Paper>

            <Divider />

            {/* Accounts Payable (We Owe) */}
            <Paper sx={{ p: 2, backgroundColor: '#E3F2FD' }}>
              <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 1 }}>
                Accounts Payable (We Owe)
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Total Amount
                    </Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {formatCurrency(settlementDetails.totalApAmount)}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Paid Amount
                    </Typography>
                    <Typography variant="body2" fontWeight="bold" sx={{ color: '#4CAF50' }}>
                      {formatCurrency(settlementDetails.paidApAmount)}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Unpaid Amount
                    </Typography>
                    <Typography variant="body2" fontWeight="bold" sx={{ color: '#F44336' }}>
                      {formatCurrency(settlementDetails.unpaidApAmount)}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Settlement Count
                    </Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {settlementDetails.apSettlementCount}
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </Paper>

            <Divider />

            {/* Accounts Receivable (They Owe Us) */}
            <Paper sx={{ p: 2, backgroundColor: '#F3E5F5' }}>
              <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 1 }}>
                Accounts Receivable (They Owe Us)
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Total Amount
                    </Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {formatCurrency(settlementDetails.totalArAmount)}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Paid Amount
                    </Typography>
                    <Typography variant="body2" fontWeight="bold" sx={{ color: '#4CAF50' }}>
                      {formatCurrency(settlementDetails.paidArAmount)}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Unpaid Amount
                    </Typography>
                    <Typography variant="body2" fontWeight="bold" sx={{ color: '#F44336' }}>
                      {formatCurrency(settlementDetails.unpaidArAmount)}
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={6}>
                  <Box>
                    <Typography variant="caption" color="textSecondary">
                      Settlement Count
                    </Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {settlementDetails.arSettlementCount}
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </Paper>

            <Divider />

            {/* Net Position */}
            <Paper
              sx={{
                p: 2,
                backgroundColor:
                  netDirection.color === '#4CAF50'
                    ? '#E8F5E9'
                    : netDirection.color === '#FF9800'
                    ? '#FFF3E0'
                    : '#F5F5F5',
              }}
            >
              <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 1 }}>
                Net Position
              </Typography>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    {settlementDetails.netDirection}
                  </Typography>
                  <Typography variant="body2" fontWeight="bold" sx={{ color: netDirection.color }}>
                    {formatCurrency(Math.abs(settlementDetails.netAmount))}
                  </Typography>
                </Box>
                <Typography variant="h4" sx={{ color: netDirection.color }}>
                  {netDirection.icon}
                </Typography>
              </Box>
            </Paper>
          </Stack>
        ) : (
          <Typography color="error">Failed to load settlement details</Typography>
        )}
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} color="primary">
          Close
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default SettlementDetailsDialog;
