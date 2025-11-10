import React, { useEffect, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  CircularProgress,
  Alert,
  Grid,
  Paper,
  Typography,
} from '@mui/material';
import paymentRiskAlertApi, { AlertSeverity, AlertType, PaymentRiskAlertDto } from '@/services/paymentRiskAlertApi';
import { formatDate, formatCurrency } from '@/utils/formatting';

export const PaymentRiskAlerts: React.FC = () => {
  const [alerts, setAlerts] = useState<PaymentRiskAlertDto[]>([]);
  const [summary, setSummary] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedAlert, setSelectedAlert] = useState<PaymentRiskAlertDto | null>(null);
  const [openDialog, setOpenDialog] = useState(false);

  useEffect(() => {
    loadAlerts();
    loadSummary();
  }, []);

  const loadAlerts = async () => {
    try {
      setLoading(true);
      const result = await paymentRiskAlertApi.getAlerts(undefined, undefined, undefined, true, 1, 100);
      setAlerts(result.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load alerts');
    } finally {
      setLoading(false);
    }
  };

  const loadSummary = async () => {
    try {
      const result = await paymentRiskAlertApi.getAlertSummary();
      setSummary(result);
    } catch (err) {
      console.error('Failed to load summary:', err);
    }
  };

  const handleResolveAlert = async (alertId: string) => {
    try {
      await paymentRiskAlertApi.resolveAlert(alertId);
      loadAlerts();
      loadSummary();
      setOpenDialog(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to resolve alert');
    }
  };

  const getSeverityColor = (severity: AlertSeverity): 'error' | 'warning' | 'info' => {
    switch (severity) {
      case AlertSeverity.Critical:
        return 'error';
      case AlertSeverity.Warning:
        return 'warning';
      default:
        return 'info';
    }
  };

  const getAlertTypeLabel = (type: AlertType): string => {
    const labels: Record<AlertType, string> = {
      [AlertType.OverduePayment]: 'Overdue Payment',
      [AlertType.UpcomingDueDate]: 'Upcoming Due Date',
      [AlertType.CreditLimitApproaching]: 'Credit Limit Approaching',
      [AlertType.CreditLimitExceeded]: 'Credit Limit Exceeded',
      [AlertType.CreditExpired]: 'Credit Expired',
      [AlertType.LargeOutstandingAmount]: 'Large Outstanding Amount',
      [AlertType.FrequentLatePayment]: 'Frequent Late Payment',
    };
    return labels[type] || 'Unknown';
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ p: 2 }}>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Summary Cards */}
      {summary && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Paper sx={{ p: 2, bgcolor: 'error.light' }}>
              <Typography variant="body2" color="textSecondary">
                Critical Alerts
              </Typography>
              <Typography variant="h5" color="error">
                {summary.criticalAlerts}
              </Typography>
            </Paper>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Paper sx={{ p: 2, bgcolor: 'warning.light' }}>
              <Typography variant="body2" color="textSecondary">
                Warning Alerts
              </Typography>
              <Typography variant="h5" color="warning.main">
                {summary.warningAlerts}
              </Typography>
            </Paper>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Paper sx={{ p: 2, bgcolor: 'info.light' }}>
              <Typography variant="body2" color="textSecondary">
                Unresolved
              </Typography>
              <Typography variant="h5" color="primary">
                {summary.unresolvedAlerts}
              </Typography>
            </Paper>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Paper sx={{ p: 2 }}>
              <Typography variant="body2" color="textSecondary">
                Amount at Risk
              </Typography>
              <Typography variant="h5">
                {formatCurrency(summary.totalAmountAtRisk)}
              </Typography>
            </Paper>
          </Grid>
        </Grid>
      )}

      {/* Alerts Table */}
      <Card>
        <CardHeader
          title="Payment Risk Alerts"
          action={
            <Button
              variant="contained"
              color="primary"
              onClick={async () => {
                try {
                  await paymentRiskAlertApi.generateAutomaticAlerts();
                  loadAlerts();
                  loadSummary();
                } catch (err) {
                  setError(err instanceof Error ? err.message : 'Failed to generate alerts');
                }
              }}
            >
              Generate Automatic Alerts
            </Button>
          }
        />
        <CardContent>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow sx={{ bgcolor: 'grey.100' }}>
                  <TableCell>Trading Partner</TableCell>
                  <TableCell>Alert Type</TableCell>
                  <TableCell>Severity</TableCell>
                  <TableCell>Amount</TableCell>
                  <TableCell>Created Date</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {alerts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center" sx={{ py: 3 }}>
                      <Typography color="textSecondary">No alerts found</Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  alerts.map((alert) => (
                    <TableRow key={alert.alertId} hover>
                      <TableCell>{alert.companyName}</TableCell>
                      <TableCell>{getAlertTypeLabel(alert.alertType)}</TableCell>
                      <TableCell>
                        <Chip
                          label={AlertSeverity[alert.severity]}
                          color={getSeverityColor(alert.severity)}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>{formatCurrency(alert.amount)}</TableCell>
                      <TableCell>{formatDate(alert.createdDate)}</TableCell>
                      <TableCell>
                        <Chip
                          label={alert.isResolved ? 'Resolved' : 'Unresolved'}
                          color={alert.isResolved ? 'default' : 'warning'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Button
                          size="small"
                          onClick={() => {
                            setSelectedAlert(alert);
                            setOpenDialog(true);
                          }}
                        >
                          View Details
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      {/* Details Dialog */}
      {selectedAlert && (
        <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
          <DialogTitle>{selectedAlert.title}</DialogTitle>
          <DialogContent>
            <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Trading Partner
                </Typography>
                <Typography variant="body1">{selectedAlert.companyName}</Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Description
                </Typography>
                <Typography variant="body2">{selectedAlert.description}</Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Amount
                </Typography>
                <Typography variant="body1">
                  {formatCurrency(selectedAlert.amount)} {selectedAlert.currency}
                </Typography>
              </Box>
              {selectedAlert.dueDate && (
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Due Date
                  </Typography>
                  <Typography variant="body1">{formatDate(selectedAlert.dueDate)}</Typography>
                </Box>
              )}
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Alert Type
                </Typography>
                <Typography variant="body1">{getAlertTypeLabel(selectedAlert.alertType)}</Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Severity
                </Typography>
                <Chip
                  label={AlertSeverity[selectedAlert.severity]}
                  color={getSeverityColor(selectedAlert.severity)}
                  size="small"
                  sx={{ mt: 0.5 }}
                />
              </Box>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Created
                </Typography>
                <Typography variant="body2">{formatDate(selectedAlert.createdDate)}</Typography>
              </Box>
              {selectedAlert.isResolved && selectedAlert.resolvedDate && (
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Resolved
                  </Typography>
                  <Typography variant="body2">{formatDate(selectedAlert.resolvedDate)}</Typography>
                </Box>
              )}
            </Box>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpenDialog(false)}>Close</Button>
            {!selectedAlert.isResolved && (
              <Button
                variant="contained"
                color="primary"
                onClick={() => handleResolveAlert(selectedAlert.alertId)}
              >
                Resolve Alert
              </Button>
            )}
          </DialogActions>
        </Dialog>
      )}
    </Box>
  );
};

export default PaymentRiskAlerts;
