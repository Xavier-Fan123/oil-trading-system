import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Chip,
  LinearProgress,
  Alert
} from '@mui/material';
import {
  Timeline,
  TimelineItem,
  TimelineSeparator,
  TimelineConnector,
  TimelineContent,
  TimelineOppositeContent,
  TimelineDot,
  TimelineProps
} from '@mui/lab';
import { ContractSettlementDto } from '@/types/settlement';
import { format } from 'date-fns';

interface PaymentTabProps {
  settlement: ContractSettlementDto;
}

const PaymentStatusColors: Record<string, 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'> = {
  'Paid': 'success',
  'PartiallyPaid': 'warning',
  'Due': 'info',
  'Overdue': 'error',
  'NotDue': 'default'
};

const PaymentStatusDescriptions: Record<string, string> = {
  'Paid': 'Settlement has been fully paid',
  'PartiallyPaid': 'Settlement is partially paid with remaining balance',
  'Due': 'Settlement payment is due',
  'Overdue': 'Settlement payment is overdue - immediate action required',
  'NotDue': 'Settlement payment is not yet due'
};

export const PaymentTab: React.FC<PaymentTabProps> = ({ settlement }) => {
  // Calculate payment progress
  const totalSettledAmount = settlement.totalSettlementAmount;
  const paidAmount = 0; // TODO: Add paidAmount field to ContractSettlementDto
  const unpaidAmount = totalSettledAmount - paidAmount;
  const paymentPercentage = totalSettledAmount > 0 ? (paidAmount / totalSettledAmount) * 100 : 0;

  const getPaymentStatus = (): keyof typeof PaymentStatusColors => {
    // This would normally come from the contract's payment status
    if (paidAmount >= totalSettledAmount) {
      return 'Paid';
    } else if (paidAmount > 0) {
      return 'PartiallyPaid';
    } else {
      return 'NotDue';
    }
  };

  const paymentStatus = getPaymentStatus();

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {/* Payment Status Overview */}
      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
            <Typography variant="h6">Payment Status</Typography>
            <Chip
              label={paymentStatus}
              color={PaymentStatusColors[paymentStatus]}
              variant="filled"
              size="medium"
            />
          </Box>

          <Alert severity={paymentStatus === 'Paid' ? 'success' : paymentStatus === 'Overdue' ? 'error' : 'info'} sx={{ mb: 3 }}>
            {PaymentStatusDescriptions[paymentStatus]}
          </Alert>

          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
            Payment Progress
          </Typography>
          <LinearProgress
            variant="determinate"
            value={paymentPercentage}
            sx={{
              height: 8,
              borderRadius: 4,
              backgroundColor: '#e0e0e0',
              '& .MuiLinearProgress-bar': {
                borderRadius: 4,
                backgroundColor: paymentPercentage === 100 ? '#4caf50' : paymentPercentage > 0 ? '#ff9800' : '#2196f3'
              }
            }}
          />
          <Typography variant="body2" sx={{ mt: 1, textAlign: 'center', fontWeight: 'bold' }}>
            {paymentPercentage.toFixed(1)}%
          </Typography>
        </CardContent>
      </Card>

      {/* Payment Amounts Section */}
      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Total Settlement Amount
              </Typography>
              <Typography variant="h5" sx={{ color: 'primary.main' }}>
                ${totalSettledAmount.toFixed(2)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {settlement.settlementCurrency}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Amount Paid
              </Typography>
              <Typography variant="h5" sx={{ color: 'success.main' }}>
                ${paidAmount.toFixed(2)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {paymentPercentage.toFixed(1)}% paid
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                Outstanding Balance
              </Typography>
              <Typography variant="h5" sx={{ color: unpaidAmount > 0 ? 'error.main' : 'success.main' }}>
                ${unpaidAmount.toFixed(2)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {(100 - paymentPercentage).toFixed(1)}% remaining
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Payment Terms Section */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Payment Due Date
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="body2" color="text.secondary">
                Expected Payment Date
              </Typography>
              {settlement.actualPayableDueDate ? (
                <Typography variant="h6">
                  {format(new Date(settlement.actualPayableDueDate), 'PPP')}
                </Typography>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Not specified
                </Typography>
              )}
            </Grid>

            <Grid item xs={12} md={6}>
              <Typography variant="body2" color="text.secondary">
                Days Until Due
              </Typography>
              {settlement.actualPayableDueDate ? (
                <Typography variant="h6">
                  {Math.ceil((new Date(settlement.actualPayableDueDate).getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24))} days
                </Typography>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Not specified
                </Typography>
              )}
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Payment History Timeline */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Settlement Timeline
          </Typography>
          <Timeline
            position="alternate"
            sx={{
              [`& .MuiTimelineItem-root:before`]: {
                flex: 0,
                padding: 0,
              },
            }}
            slotProps={{}}
          >
            <TimelineItem>
              <TimelineOppositeContent color="textSecondary">
                {format(new Date(settlement.createdDate), 'MMM dd, yyyy')}
              </TimelineOppositeContent>
              <TimelineSeparator>
                <TimelineDot color="primary" />
                <TimelineConnector />
              </TimelineSeparator>
              <TimelineContent>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                  Settlement Created
                </Typography>
                <Typography variant="caption" color="textSecondary">
                  by {settlement.createdBy}
                </Typography>
              </TimelineContent>
            </TimelineItem>

            {settlement.lastModifiedDate && settlement.lastModifiedBy && (
              <TimelineItem>
                <TimelineOppositeContent color="textSecondary">
                  {format(new Date(settlement.lastModifiedDate), 'MMM dd, yyyy')}
                </TimelineOppositeContent>
                <TimelineSeparator>
                  <TimelineDot color="secondary" />
                  <TimelineConnector />
                </TimelineSeparator>
                <TimelineContent>
                  <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                    Settlement Modified
                  </Typography>
                  <Typography variant="caption" color="textSecondary">
                    by {settlement.lastModifiedBy}
                  </Typography>
                </TimelineContent>
              </TimelineItem>
            )}

            {settlement.finalizedDate && settlement.finalizedBy && (
              <TimelineItem>
                <TimelineOppositeContent color="textSecondary">
                  {format(new Date(settlement.finalizedDate), 'MMM dd, yyyy')}
                </TimelineOppositeContent>
                <TimelineSeparator>
                  <TimelineDot color="success" />
                </TimelineSeparator>
                <TimelineContent>
                  <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                    Settlement Finalized
                  </Typography>
                  <Typography variant="caption" color="textSecondary">
                    by {settlement.finalizedBy}
                  </Typography>
                </TimelineContent>
              </TimelineItem>
            )}
          </Timeline>
        </CardContent>
      </Card>

      {/* Payment Instructions */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Payment Instructions
          </Typography>
          <Alert severity="info">
            <Typography variant="body2">
              Payment should be made according to the settlement terms outlined in the contract.
              Please ensure all payments are processed through authorized banking channels.
              For any payment-related inquiries, please contact the finance department.
            </Typography>
          </Alert>
        </CardContent>
      </Card>
    </Box>
  );
};
