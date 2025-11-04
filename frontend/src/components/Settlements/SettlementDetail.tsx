import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Grid,
  Divider,
  Chip,
  Alert,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Tabs,
  Tab
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Edit as EditIcon,
  Calculate as CalculateIcon,
  Lock as LockIcon,
  Add as AddIcon,
  Refresh as RefreshIcon
} from '@mui/icons-material';
import { format } from 'date-fns';
import {
  ContractSettlementDto,
  ContractSettlementStatus,
  getSettlementStatusColor
} from '@/types/settlement';
import { getSettlementWithFallback, settlementApi } from '@/services/settlementApi';
import { ChargeManager } from './ChargeManager';
import { SettlementTab } from './SettlementTab';
import { PaymentTab } from './PaymentTab';
import { ExecutionTab } from './ExecutionTab';
import { SettlementHistoryTab } from './SettlementHistoryTab';
import { PaymentTrackingTab } from './PaymentTrackingTab';
import { ExecutionStatusTab } from './ExecutionStatusTab';

interface SettlementDetailProps {
  settlementId: string;
  onEdit: () => void;
  onBack: () => void;
}

export const SettlementDetail: React.FC<SettlementDetailProps> = ({
  settlementId,
  onEdit,
  onBack
}) => {
  const [settlement, setSettlement] = useState<ContractSettlementDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showChargeManager, setShowChargeManager] = useState(false);
  const [recalculating, setRecalculating] = useState(false);
  const [finalizing, setFinalizing] = useState(false);
  const [confirmFinalize, setConfirmFinalize] = useState(false);
  const [activeTab, setActiveTab] = useState(0);

  const loadSettlement = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getSettlementWithFallback(settlementId);
      setSettlement(data);
    } catch (err) {
      console.error('Error loading settlement:', err);
      setError('Failed to load settlement details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSettlement();
  }, [settlementId]);

  const handleRecalculate = async () => {
    if (!settlement) return;

    setRecalculating(true);
    try {
      const updatedSettlement = await settlementApi.recalculateSettlement(settlementId);
      setSettlement(updatedSettlement);
    } catch (err) {
      console.error('Error recalculating settlement:', err);
      setError('Failed to recalculate settlement');
    } finally {
      setRecalculating(false);
    }
  };

  const handleFinalize = async () => {
    if (!settlement) return;

    setFinalizing(true);
    try {
      const finalizedSettlement = await settlementApi.finalizeSettlement(settlementId);
      setSettlement(finalizedSettlement);
      setConfirmFinalize(false);
    } catch (err) {
      console.error('Error finalizing settlement:', err);
      setError('Failed to finalize settlement');
    } finally {
      setFinalizing(false);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const formatQuantity = (quantity: number, unit: string = '') => {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(quantity) + (unit ? ` ${unit}` : '');
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !settlement) {
    return (
      <Box>
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={onBack}
          sx={{ mb: 2 }}
        >
          Back
        </Button>
        <Alert severity="error">
          {error || 'Settlement not found'}
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <Button
            startIcon={<ArrowBackIcon />}
            onClick={onBack}
            sx={{ mr: 2 }}
          >
            Back
          </Button>
          <Box>
            <Typography variant="h4" component="h1">
              Settlement Details
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {settlement.externalContractNumber} • {settlement.contractNumber}
            </Typography>
          </Box>
        </Box>

        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Refresh">
            <IconButton onClick={loadSettlement}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
          {settlement.canBeModified && (
            <>
              <Button
                variant="outlined"
                startIcon={<CalculateIcon />}
                onClick={handleRecalculate}
                disabled={recalculating}
              >
                {recalculating ? 'Calculating...' : 'Recalculate'}
              </Button>
              <Button
                variant="outlined"
                startIcon={<EditIcon />}
                onClick={onEdit}
              >
                Edit
              </Button>
            </>
          )}
          {settlement.canBeModified && settlement.status === 'Reviewed' && (
            <Button
              variant="contained"
              startIcon={<LockIcon />}
              onClick={() => setConfirmFinalize(true)}
              disabled={finalizing}
            >
              {finalizing ? 'Finalizing...' : 'Finalize'}
            </Button>
          )}
        </Box>
      </Box>

      {/* Tabbed Content Section */}
      <Card sx={{ mb: 3 }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider', overflowX: 'auto' }}>
          <Tabs value={activeTab} onChange={(_, newValue) => setActiveTab(newValue)} variant="scrollable" scrollButtons="auto">
            <Tab label="Settlement Details" />
            <Tab label="Payment Tracking" />
            <Tab label="Settlement History" />
            <Tab label="Execution Status" />
            <Tab label="Payment Information" />
            <Tab label="Charges & Fees" />
          </Tabs>
        </Box>
        <CardContent sx={{ pt: 3 }}>
          {activeTab === 0 && <SettlementTab settlement={settlement} />}
          {activeTab === 1 && <PaymentTrackingTab settlementId={settlementId} />}
          {activeTab === 2 && <SettlementHistoryTab settlementId={settlementId} />}
          {activeTab === 3 && <ExecutionStatusTab settlementId={settlementId} />}
          {activeTab === 4 && <PaymentTab settlement={settlement} />}
          {activeTab === 5 && <ChargeManager settlementId={settlementId} canEdit={settlement.canBeModified} />}
        </CardContent>
      </Card>

      {/* Status and Basic Info - Legacy Content (Optional) */}
      <Grid container spacing={3}>
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                <Typography variant="h6">Settlement Status</Typography>
                <Chip
                  label={settlement.displayStatus}
                  color={getSettlementStatusColor(settlement.status as unknown as ContractSettlementStatus)}
                  size="medium"
                  variant={settlement.isFinalized ? 'filled' : 'outlined'}
                  icon={settlement.isFinalized ? <LockIcon /> : undefined}
                />
              </Box>
              
              <Grid container spacing={3}>
                <Grid item xs={12} md={4}>
                  <Typography variant="body2" color="text.secondary">External Contract</Typography>
                  <Typography variant="h6">{settlement.externalContractNumber}</Typography>
                </Grid>
                <Grid item xs={12} md={4}>
                  <Typography variant="body2" color="text.secondary">Contract Number</Typography>
                  <Typography variant="h6">{settlement.contractNumber}</Typography>
                </Grid>
                <Grid item xs={12} md={4}>
                  <Typography variant="body2" color="text.secondary">Document</Typography>
                  <Typography variant="h6">
                    {settlement.documentNumber || 'N/A'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {settlement.documentType} • {format(new Date(settlement.documentDate), 'MMM dd, yyyy')}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Contract Information */}
        {settlement.purchaseContract && (
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Purchase Contract</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Supplier</Typography>
                    <Typography variant="body1">{settlement.purchaseContract.supplierName}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Product</Typography>
                    <Typography variant="body1">{settlement.purchaseContract.productName}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Contract Quantity</Typography>
                    <Typography variant="body1">
                      {formatQuantity(settlement.purchaseContract.quantity, settlement.purchaseContract.quantityUnit === 1 ? 'MT' : 'BBL')}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Laycan</Typography>
                    <Typography variant="body1">
                      {format(new Date(settlement.purchaseContract.laycanStart), 'MMM dd')} - {format(new Date(settlement.purchaseContract.laycanEnd), 'MMM dd, yyyy')}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        {settlement.salesContract && (
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Sales Contract</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Customer</Typography>
                    <Typography variant="body1">{settlement.salesContract.customerName}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Product</Typography>
                    <Typography variant="body1">{settlement.salesContract.productName}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Contract Quantity</Typography>
                    <Typography variant="body1">
                      {formatQuantity(settlement.salesContract.quantity, settlement.salesContract.quantityUnit === 1 ? 'MT' : 'BBL')}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Laycan</Typography>
                    <Typography variant="body1">
                      {format(new Date(settlement.salesContract.laycanStart), 'MMM dd')} - {format(new Date(settlement.salesContract.laycanEnd), 'MMM dd, yyyy')}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        {/* Quantities */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Quantities & Calculations</Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">Actual Quantity (B/L)</Typography>
                  <Typography variant="h6">{formatQuantity(settlement.actualQuantityMT, 'MT')}</Typography>
                  <Typography variant="body2" color="text.secondary">{formatQuantity(settlement.actualQuantityBBL, 'BBL')}</Typography>
                </Grid>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">Calculation Quantity</Typography>
                  <Typography variant="h6">{formatQuantity(settlement.calculationQuantityMT, 'MT')}</Typography>
                  <Typography variant="body2" color="text.secondary">{formatQuantity(settlement.calculationQuantityBBL, 'BBL')}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  {settlement.quantityCalculationNote && (
                    <Box>
                      <Typography variant="body2" color="text.secondary">Calculation Note</Typography>
                      <Typography variant="body2">{settlement.quantityCalculationNote}</Typography>
                    </Box>
                  )}
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Pricing Information */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Pricing & Settlement</Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">Benchmark Price</Typography>
                  <Typography variant="h6">{formatCurrency(settlement.benchmarkPrice, settlement.benchmarkPriceCurrency)}</Typography>
                  {settlement.benchmarkPriceFormula && (
                    <Typography variant="caption" color="text.secondary">
                      {settlement.benchmarkPriceFormula}
                    </Typography>
                  )}
                </Grid>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">Pricing Period</Typography>
                  <Typography variant="body2">
                    {settlement.pricingStartDate && settlement.pricingEndDate ? (
                      <>
                        {format(new Date(settlement.pricingStartDate), 'MMM dd')} - {format(new Date(settlement.pricingEndDate), 'MMM dd, yyyy')}
                      </>
                    ) : 'Not specified'}
                  </Typography>
                </Grid>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">Benchmark Amount</Typography>
                  <Typography variant="h6">{formatCurrency(settlement.benchmarkAmount, settlement.settlementCurrency)}</Typography>
                </Grid>
                <Grid item xs={12} md={3}>
                  <Typography variant="body2" color="text.secondary">Adjustment Amount</Typography>
                  <Typography variant="h6">{formatCurrency(settlement.adjustmentAmount, settlement.settlementCurrency)}</Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Settlement Calculation */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Settlement Calculation</Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="body1">Cargo Value (Benchmark + Adjustment)</Typography>
                  <Typography variant="h6">{settlement.formattedCargoValue}</Typography>
                </Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="body1">Total Charges ({settlement.charges.length} items)</Typography>
                  <Typography variant="h6">{settlement.formattedTotalCharges}</Typography>
                </Box>
                <Divider />
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="h6">Total Settlement Amount</Typography>
                  <Typography variant="h5" color="primary">{settlement.formattedTotalAmount}</Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Charges */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">Settlement Charges</Typography>
                {settlement.canBeModified && (
                  <Button
                    variant="outlined"
                    startIcon={<AddIcon />}
                    size="small"
                    onClick={() => setShowChargeManager(true)}
                  >
                    Manage Charges
                  </Button>
                )}
              </Box>

              {settlement.charges.length === 0 ? (
                <Alert severity="info">
                  No charges have been added to this settlement.
                  {settlement.canBeModified && (
                    <Button
                      variant="outlined"
                      size="small"
                      onClick={() => setShowChargeManager(true)}
                      sx={{ ml: 2 }}
                    >
                      Add Charges
                    </Button>
                  )}
                </Alert>
              ) : (
                <TableContainer component={Paper} variant="outlined">
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Charge Type</TableCell>
                        <TableCell>Description</TableCell>
                        <TableCell align="right">Amount</TableCell>
                        <TableCell>Incurred Date</TableCell>
                        <TableCell>Reference</TableCell>
                        <TableCell>Created By</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {settlement.charges.map((charge) => (
                        <TableRow key={charge.id}>
                          <TableCell>
                            <Chip
                              label={charge.chargeTypeDisplayName}
                              size="small"
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell>{charge.description}</TableCell>
                          <TableCell align="right">
                            <Typography
                              variant="body2"
                              color={charge.amount >= 0 ? 'text.primary' : 'error'}
                              fontWeight="medium"
                            >
                              {charge.formattedAmount}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            {charge.incurredDate ? format(new Date(charge.incurredDate), 'MMM dd, yyyy') : 'N/A'}
                          </TableCell>
                          <TableCell>{charge.referenceDocument || 'N/A'}</TableCell>
                          <TableCell>
                            <Typography variant="body2">{charge.createdBy}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {format(new Date(charge.createdDate), 'MMM dd, yyyy')}
                            </Typography>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Audit Trail */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Audit Trail</Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Created</Typography>
                    <Typography variant="body1">
                      {format(new Date(settlement.createdDate), 'MMM dd, yyyy HH:mm')} by {settlement.createdBy}
                    </Typography>
                  </Box>
                </Grid>
                {settlement.lastModifiedDate && (
                  <Grid item xs={12} md={6}>
                    <Box>
                      <Typography variant="body2" color="text.secondary">Last Modified</Typography>
                      <Typography variant="body1">
                        {format(new Date(settlement.lastModifiedDate), 'MMM dd, yyyy HH:mm')} by {settlement.lastModifiedBy}
                      </Typography>
                    </Box>
                  </Grid>
                )}
                {settlement.finalizedDate && (
                  <Grid item xs={12} md={6}>
                    <Box>
                      <Typography variant="body2" color="text.secondary">Finalized</Typography>
                      <Typography variant="body1">
                        {format(new Date(settlement.finalizedDate), 'MMM dd, yyyy HH:mm')} by {settlement.finalizedBy}
                      </Typography>
                    </Box>
                  </Grid>
                )}
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Charge Manager Dialog */}
      {showChargeManager && (
        <ChargeManager
          settlementId={settlementId}
          charges={settlement.charges}
          canEdit={settlement.canBeModified}
          onClose={() => setShowChargeManager(false)}
          onChargesUpdated={loadSettlement}
        />
      )}

      {/* Finalize Confirmation Dialog */}
      <Dialog
        open={confirmFinalize}
        onClose={() => setConfirmFinalize(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Confirm Settlement Finalization</DialogTitle>
        <DialogContent>
          <Typography paragraph>
            Are you sure you want to finalize this settlement? Once finalized, the settlement cannot be modified.
          </Typography>
          <Alert severity="warning" sx={{ mt: 2 }}>
            This action cannot be undone. Please review all charges and calculations before finalizing.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmFinalize(false)}>Cancel</Button>
          <Button
            onClick={handleFinalize}
            variant="contained"
            disabled={finalizing}
            startIcon={finalizing ? <CircularProgress size={20} /> : <LockIcon />}
          >
            {finalizing ? 'Finalizing...' : 'Finalize Settlement'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};