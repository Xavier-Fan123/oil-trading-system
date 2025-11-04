import React from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Alert,
  Grid,
  Paper,
  LinearProgress,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { settlementPaymentApi, PaymentTrackingDto, PaymentDto } from '@/services/settlementApi';
import { getPaymentStatusColor } from '@/types/settlement';

interface PaymentTrackingTabProps {
  settlementId: string;
}

/**
 * Payment Tracking Tab Component
 * Displays payment status, tracking progress, and payment history
 */
export const PaymentTrackingTab: React.FC<PaymentTrackingTabProps> = ({ settlementId }) => {
  const {
    data: tracking,
    isLoading: trackingLoading,
    error: trackingError,
  } = useQuery({
    queryKey: ['paymentTracking', settlementId],
    queryFn: () => settlementPaymentApi.getPaymentTracking(settlementId),
    enabled: !!settlementId,
  });

  const {
    data: payments,
    isLoading: paymentsLoading,
    error: paymentsError,
  } = useQuery({
    queryKey: ['payments', settlementId],
    queryFn: () => settlementPaymentApi.getPayments(settlementId),
    enabled: !!settlementId,
  });

  const isLoading = trackingLoading || paymentsLoading;
  const error = trackingError || paymentsError;

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load payment information: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {/* Payment Summary Cards */}
      {tracking && (
        <Grid container spacing={2}>
          <Grid item xs={12} sm={6} md={3}>
            <Paper elevation={2} sx={{ p: 2 }}>
              <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                Total Amount
              </Typography>
              <Typography variant="h5" sx={{ fontWeight: 600, mb: 1 }}>
                {tracking.currency} {tracking.totalAmount.toLocaleString()}
              </Typography>
              <LinearProgress variant="determinate" value={100} />
            </Paper>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <Paper elevation={2} sx={{ p: 2, backgroundColor: '#e8f5e9' }}>
              <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                Paid Amount
              </Typography>
              <Typography variant="h5" sx={{ fontWeight: 600, color: '#4caf50', mb: 1 }}>
                {tracking.currency} {tracking.amountPaid.toLocaleString()}
              </Typography>
              <LinearProgress
                variant="determinate"
                value={tracking.completionPercentage}
                sx={{ backgroundColor: '#e0e0e0' }}
              />
              <Typography variant="caption" color="textSecondary">
                {tracking.completionPercentage.toFixed(1)}% complete
              </Typography>
            </Paper>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <Paper elevation={2} sx={{ p: 2, backgroundColor: '#fff3e0' }}>
              <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                Amount Due
              </Typography>
              <Typography variant="h5" sx={{ fontWeight: 600, color: '#ff9800', mb: 1 }}>
                {tracking.currency} {tracking.amountDue.toLocaleString()}
              </Typography>
              <LinearProgress
                variant="determinate"
                value={100 - tracking.completionPercentage}
                sx={{ backgroundColor: '#e0e0e0' }}
              />
            </Paper>
          </Grid>

          <Grid item xs={12} sm={6} md={3}>
            <Paper elevation={2} sx={{ p: 2, backgroundColor: '#ffebee' }}>
              <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                Amount Overdue
              </Typography>
              <Typography variant="h5" sx={{ fontWeight: 600, color: '#f44336', mb: 1 }}>
                {tracking.currency} {tracking.amountOverdue.toLocaleString()}
              </Typography>
              <Chip
                label={`Status: ${typeof tracking.paymentStatus === 'string' ? tracking.paymentStatus : 'Unknown'}`}
                size="small"
                color={getPaymentStatusColor(tracking.paymentStatus)}
                variant="outlined"
                sx={{ mt: 1 }}
              />
            </Paper>
          </Grid>
        </Grid>
      )}

      {/* Payment Terms Information */}
      {tracking && (
        <Card>
          <CardHeader title="Payment Terms & Dates" />
          <CardContent>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Payment Terms
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {typeof tracking.paymentTerms === 'string' ? tracking.paymentTerms : 'Not specified'}
                  </Typography>
                </Box>
              </Grid>

              <Grid item xs={12} sm={6}>
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Payment Method
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {typeof tracking.paymentMethod === 'string' ? tracking.paymentMethod : 'Not specified'}
                  </Typography>
                </Box>
              </Grid>

              <Grid item xs={12} sm={6}>
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Next Due Date
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {tracking.nextDueDate ? format(new Date(tracking.nextDueDate), 'MMM dd, yyyy') : 'No due date'}
                  </Typography>
                </Box>
              </Grid>

              <Grid item xs={12} sm={6}>
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Last Payment Date
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {tracking.lastPaymentDate ? format(new Date(tracking.lastPaymentDate), 'MMM dd, yyyy') : 'No payments yet'}
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Payment Records Table */}
      {payments && payments.length > 0 && (
        <Card>
          <CardHeader
            title="Payment Records"
            subheader={`${payments.length} payment${payments.length !== 1 ? 's' : ''}`}
          />
          <CardContent>
            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                    <TableCell>Payment Reference</TableCell>
                    <TableCell align="right">Amount</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Method</TableCell>
                    <TableCell>Payment Date</TableCell>
                    <TableCell>Received Date</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {payments.map((payment) => (
                    <TableRow key={payment.id} sx={{ '&:hover': { backgroundColor: '#fafafa' } }}>
                      <TableCell>
                        <Typography variant="body2">{payment.paymentReference || '-'}</Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                          {payment.currency} {payment.amount.toLocaleString()}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={typeof payment.paymentStatus === 'string' ? payment.paymentStatus : 'Unknown'}
                          size="small"
                          color={getPaymentStatusColor(payment.paymentStatus)}
                          variant="filled"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {typeof payment.paymentMethod === 'string' ? payment.paymentMethod : 'Unknown'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        {payment.paymentDate ? (
                          format(new Date(payment.paymentDate), 'MMM dd, yyyy')
                        ) : (
                          <Typography variant="body2" color="textSecondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                      <TableCell>
                        {payment.receivedDate ? (
                          format(new Date(payment.receivedDate), 'MMM dd, yyyy')
                        ) : (
                          <Typography variant="body2" color="textSecondary">
                            -
                          </Typography>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {!payments || payments.length === 0 && (
        <Alert severity="info">
          No payments recorded yet. Payment records will appear here when payments are made.
        </Alert>
      )}
    </Box>
  );
};

export default PaymentTrackingTab;
