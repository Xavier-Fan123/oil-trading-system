import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  Chip,
  CircularProgress,
  Grid,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Alert,
  Button,
} from '@mui/material';
import { format } from 'date-fns';
import { ContractExecutionReportDto } from '@/types/reports';
import { contractExecutionReportApi } from '@/services/contractExecutionReportApi';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';

interface ContractExecutionReportDetailsProps {
  contractId: string;
  isPurchaseContract?: boolean;
  onBack?: () => void;
}

export const ContractExecutionReportDetails: React.FC<
  ContractExecutionReportDetailsProps
> = ({ contractId, isPurchaseContract = true, onBack }) => {
  const [report, setReport] = useState<ContractExecutionReportDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadReport();
  }, [contractId]);

  const loadReport = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await contractExecutionReportApi.getContractReport(
        contractId,
        isPurchaseContract
      );
      setReport(result);
    } catch (err: any) {
      setError(err.message || 'Failed to load report');
      console.error('Error loading report:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  if (!report) {
    return <Alert severity="warning">Report not found</Alert>;
  }

  const getStatusColor = (status: string): 'success' | 'info' | 'warning' | 'error' => {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'OnTrack':
        return 'info';
      case 'Delayed':
        return 'warning';
      case 'Cancelled':
        return 'error';
      default:
        return 'info';
    }
  };

  const getPaymentStatusColor = (status: string): 'success' | 'warning' | 'error' | 'default' => {
    switch (status) {
      case 'Paid':
        return 'success';
      case 'PartiallyPaid':
        return 'warning';
      case 'NotPaid':
      case 'NotDue':
        return 'default';
      default:
        return 'default';
    }
  };

  return (
    <Box>
      {onBack && (
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={onBack}
          sx={{ mb: 2 }}
        >
          Back to Reports
        </Button>
      )}

      {/* Header Card */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          title={`Contract #${report.contractNumber}`}
          subheader={`Generated: ${format(
            new Date(report.reportGeneratedDate),
            'MMM dd, yyyy HH:mm'
          )}`}
        />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Contract Type
              </Typography>
              <Chip
                label={report.contractType}
                variant="outlined"
                color={report.contractType === 'Purchase' ? 'primary' : 'secondary'}
                size="small"
                sx={{ mt: 1 }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Execution Status
              </Typography>
              <Chip
                label={report.executionStatus}
                color={getStatusColor(report.executionStatus)}
                size="small"
                sx={{ mt: 1 }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Payment Status
              </Typography>
              <Chip
                label={report.paymentStatus}
                color={getPaymentStatusColor(report.paymentStatus)}
                size="small"
                sx={{ mt: 1 }}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Execution %
              </Typography>
              <Typography variant="h6" sx={{ mt: 1, fontWeight: 600 }}>
                {report.executionPercentage.toFixed(1)}%
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Contract Details */}
      <Card sx={{ mb: 3 }}>
        <CardHeader title="Contract Details" />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Trading Partner
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.tradingPartnerName}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Product
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.productName}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Quantity
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.quantity.toLocaleString()} {report.quantityUnit}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Executed Quantity
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {(report.executedQuantity ?? 0).toLocaleString()} {report.quantityUnit}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Contract Value
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.currency} {report.contractValue?.toLocaleString() ?? 'N/A'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Delivery Terms
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.deliveryTerms ?? 'N/A'}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Contract Status
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.contractStatus}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Created Date
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                {report.createdDate ? format(new Date(report.createdDate), 'MMM dd, yyyy') : 'N/A'}
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Execution Metrics */}
      <Card sx={{ mb: 3 }}>
        <CardHeader title="Execution Metrics" />
        <CardContent>
          <TableContainer>
            <Table size="small">
              <TableHead sx={{ backgroundColor: '#f5f5f5' }}>
                <TableRow>
                  <TableCell>Metric</TableCell>
                  <TableCell align="right">Value</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                <TableRow>
                  <TableCell>Execution Percentage</TableCell>
                  <TableCell align="right">{report.executionPercentage.toFixed(2)}%</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Executed Quantity</TableCell>
                  <TableCell align="right">
                    {(report.executedQuantity ?? 0).toLocaleString()} {report.quantityUnit}
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Shipping Operations</TableCell>
                  <TableCell align="right">{report.shippingOperationCount}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Days to Activation</TableCell>
                  <TableCell align="right">{report.daysToActivation || 'N/A'}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Days to Completion</TableCell>
                  <TableCell align="right">{report.daysToCompletion || 'N/A'}</TableCell>
                </TableRow>
                <TableRow>
                  <TableCell>Is On Schedule</TableCell>
                  <TableCell align="right">
                    <Chip
                      label={report.isOnSchedule ? 'Yes' : 'No'}
                      color={report.isOnSchedule ? 'success' : 'error'}
                      size="small"
                    />
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      {/* Settlement Information */}
      <Card sx={{ mb: 3 }}>
        <CardHeader title="Settlement Information" />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Settlement Count
              </Typography>
              <Typography variant="h6" sx={{ mt: 0.5, fontWeight: 600 }}>
                {report.settlementCount}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Total Settled Amount
              </Typography>
              <Typography variant="h6" sx={{ mt: 0.5, fontWeight: 600 }}>
                {report.currency} {report.totalSettledAmount.toLocaleString()}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Paid Amount
              </Typography>
              <Typography variant="h6" sx={{ mt: 0.5, fontWeight: 600 }}>
                {report.currency} {report.paidSettledAmount.toLocaleString()}
              </Typography>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="caption" color="textSecondary">
                Unpaid Amount
              </Typography>
              <Typography variant="h6" sx={{ mt: 0.5, fontWeight: 600 }}>
                {report.currency} {report.unpaidSettledAmount.toLocaleString()}
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Pricing Information */}
      {(report.benchmarkPrice || report.adjustmentPrice || report.finalPrice) && (
        <Card sx={{ mb: 3 }}>
          <CardHeader title="Pricing Information" />
          <CardContent>
            <Grid container spacing={2}>
              {report.benchmarkPrice && (
                <Grid item xs={12} sm={6} md={3}>
                  <Typography variant="caption" color="textSecondary">
                    Benchmark Price
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                    {report.benchmarkPrice.toFixed(2)}
                  </Typography>
                </Grid>
              )}
              {report.adjustmentPrice && (
                <Grid item xs={12} sm={6} md={3}>
                  <Typography variant="caption" color="textSecondary">
                    Adjustment Price
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                    {report.adjustmentPrice.toFixed(2)}
                  </Typography>
                </Grid>
              )}
              {report.finalPrice && (
                <Grid item xs={12} sm={6} md={3}>
                  <Typography variant="caption" color="textSecondary">
                    Final Price
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                    {report.finalPrice.toFixed(2)}
                  </Typography>
                </Grid>
              )}
              <Grid item xs={12} sm={6} md={3}>
                <Typography variant="caption" color="textSecondary">
                  Price Finalized
                </Typography>
                <Chip
                  label={report.isPriceFinalized ? 'Yes' : 'No'}
                  color={report.isPriceFinalized ? 'success' : 'warning'}
                  size="small"
                  sx={{ mt: 1 }}
                />
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Delivery Dates */}
      <Card sx={{ mb: 3 }}>
        <CardHeader title="Delivery & Dates" />
        <CardContent>
          <Grid container spacing={2}>
            {report.laycanStart !== undefined && (
              <Grid item xs={12} sm={6} md={3}>
                <Typography variant="caption" color="textSecondary">
                  Laycan Start
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                  {format(new Date(report.laycanStart as string), 'MMM dd, yyyy')}
                </Typography>
              </Grid>
            )}
            {report.laycanEnd !== undefined && (
              <Grid item xs={12} sm={6} md={3}>
                <Typography variant="caption" color="textSecondary">
                  Laycan End
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                  {format(new Date(report.laycanEnd as string), 'MMM dd, yyyy')}
                </Typography>
              </Grid>
            )}
            {report.estimatedDeliveryDate !== undefined && (
              <Grid item xs={12} sm={6} md={3}>
                <Typography variant="caption" color="textSecondary">
                  Estimated Delivery
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                  {format(new Date(report.estimatedDeliveryDate as string), 'MMM dd, yyyy')}
                </Typography>
              </Grid>
            )}
            {report.actualDeliveryDate !== undefined && (
              <Grid item xs={12} sm={6} md={3}>
                <Typography variant="caption" color="textSecondary">
                  Actual Delivery
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.5, fontWeight: 500 }}>
                  {format(new Date(report.actualDeliveryDate as string), 'MMM dd, yyyy')}
                </Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {/* Notes */}
      {report.notes && (
        <Card>
          <CardHeader title="Notes" />
          <CardContent>
            <Typography variant="body2">{report.notes}</Typography>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};
