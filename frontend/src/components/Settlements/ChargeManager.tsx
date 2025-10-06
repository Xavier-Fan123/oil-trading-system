import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Tooltip,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Alert,
  Chip,
  CircularProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Save as SaveIcon,
  Cancel as CancelIcon,
  Close as CloseIcon
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { format } from 'date-fns';
import {
  SettlementChargeDto,
  ChargeType,
  ChargeTypeLabels,
  getChargeTypeColor,
  ChargeFormData
} from '@/types/settlement';
import { settlementChargeApi } from '@/services/settlementApi';

interface ChargeManagerProps {
  settlementId: string;
  charges: SettlementChargeDto[];
  canEdit: boolean;
  onClose: () => void;
  onChargesUpdated: () => void;
}

type EditMode = 'none' | 'add' | 'edit';

export const ChargeManager: React.FC<ChargeManagerProps> = ({
  settlementId,
  charges,
  canEdit,
  onClose,
  onChargesUpdated
}) => {
  const [editMode, setEditMode] = useState<EditMode>('none');
  const [editingChargeId, setEditingChargeId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<ChargeFormData>({
    chargeType: ChargeType.Other,
    description: '',
    amount: 0,
    currency: 'USD',
    incurredDate: new Date(),
    referenceDocument: '',
    notes: ''
  });

  const resetForm = () => {
    setFormData({
      chargeType: ChargeType.Other,
      description: '',
      amount: 0,
      currency: 'USD',
      incurredDate: new Date(),
      referenceDocument: '',
      notes: ''
    });
    setEditMode('none');
    setEditingChargeId(null);
    setError(null);
  };

  const handleAdd = () => {
    resetForm();
    setEditMode('add');
  };

  const handleEdit = (charge: SettlementChargeDto) => {
    setFormData({
      id: charge.id,
      chargeType: Object.keys(ChargeTypeLabels).find(key => 
        ChargeTypeLabels[key as unknown as ChargeType] === charge.chargeTypeDisplayName
      ) as unknown as ChargeType || ChargeType.Other,
      description: charge.description,
      amount: charge.amount,
      currency: charge.currency,
      incurredDate: charge.incurredDate || new Date(),
      referenceDocument: charge.referenceDocument || '',
      notes: charge.notes || ''
    });
    setEditingChargeId(charge.id);
    setEditMode('edit');
  };

  const handleSave = async () => {
    if (!formData.description.trim()) {
      setError('Description is required');
      return;
    }

    if (formData.amount === 0) {
      setError('Amount cannot be zero');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      if (editMode === 'add') {
        await settlementChargeApi.addCharge(settlementId, {
          chargeType: formData.chargeType,
          description: formData.description.trim(),
          amount: formData.amount,
          referenceDocument: formData.referenceDocument?.trim() || undefined
        });
      } else if (editMode === 'edit' && editingChargeId) {
        await settlementChargeApi.updateCharge(settlementId, editingChargeId, {
          chargeType: formData.chargeType,
          description: formData.description.trim(),
          amount: formData.amount,
          referenceDocument: formData.referenceDocument?.trim() || undefined
        });
      }

      resetForm();
      onChargesUpdated();
    } catch (err) {
      console.error('Error saving charge:', err);
      setError('Failed to save charge. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (chargeId: string) => {
    if (!confirm('Are you sure you want to delete this charge?')) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await settlementChargeApi.removeCharge(settlementId, chargeId);
      onChargesUpdated();
    } catch (err) {
      console.error('Error deleting charge:', err);
      setError('Failed to delete charge. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const totalCharges = charges.reduce((sum, charge) => sum + charge.amount, 0);
  const positiveCharges = charges.filter(charge => charge.amount > 0).reduce((sum, charge) => sum + charge.amount, 0);
  const negativeCharges = charges.filter(charge => charge.amount < 0).reduce((sum, charge) => sum + charge.amount, 0);

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Dialog
        open
        onClose={onClose}
        maxWidth="lg"
        fullWidth
        PaperProps={{
          sx: { minHeight: '600px' }
        }}
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography variant="h6">Manage Settlement Charges</Typography>
            <IconButton onClick={onClose} size="small">
              <CloseIcon />
            </IconButton>
          </Box>
        </DialogTitle>

        <DialogContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          {/* Summary */}
          <Box sx={{ mb: 3, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
            <Typography variant="h6" gutterBottom>Charges Summary</Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={3}>
                <Typography variant="body2" color="text.secondary">Total Charges</Typography>
                <Typography variant="h6">{charges.length}</Typography>
              </Grid>
              <Grid item xs={12} md={3}>
                <Typography variant="body2" color="text.secondary">Positive Charges</Typography>
                <Typography variant="h6" color="success.main">
                  {formatCurrency(positiveCharges)}
                </Typography>
              </Grid>
              <Grid item xs={12} md={3}>
                <Typography variant="body2" color="text.secondary">Negative Charges</Typography>
                <Typography variant="h6" color="error.main">
                  {formatCurrency(negativeCharges)}
                </Typography>
              </Grid>
              <Grid item xs={12} md={3}>
                <Typography variant="body2" color="text.secondary">Net Total</Typography>
                <Typography variant="h6" color={totalCharges >= 0 ? 'success.main' : 'error.main'}>
                  {formatCurrency(totalCharges)}
                </Typography>
              </Grid>
            </Grid>
          </Box>

          {/* Add/Edit Form */}
          {editMode !== 'none' && canEdit && (
            <Box sx={{ mb: 3, p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
              <Typography variant="h6" gutterBottom>
                {editMode === 'add' ? 'Add New Charge' : 'Edit Charge'}
              </Typography>

              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <FormControl fullWidth required>
                    <InputLabel>Charge Type</InputLabel>
                    <Select
                      value={formData.chargeType}
                      label="Charge Type"
                      onChange={(e) => setFormData(prev => ({ ...prev, chargeType: e.target.value as ChargeType }))}
                      disabled={loading}
                    >
                      {Object.entries(ChargeTypeLabels).map(([value, label]) => (
                        <MenuItem key={value} value={value}>
                          {label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Grid>

                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    required
                    label="Amount"
                    type="number"
                    value={formData.amount}
                    onChange={(e) => setFormData(prev => ({ ...prev, amount: parseFloat(e.target.value) || 0 }))}
                    disabled={loading}
                    helperText="Use negative values for credits/discounts"
                  />
                </Grid>

                <Grid item xs={12} md={4}>
                  <TextField
                    fullWidth
                    label="Currency"
                    value={formData.currency}
                    onChange={(e) => setFormData(prev => ({ ...prev, currency: e.target.value }))}
                    disabled={loading}
                  />
                </Grid>

                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    required
                    label="Description"
                    value={formData.description}
                    onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                    disabled={loading}
                    placeholder="e.g., Vessel demurrage charges, Port handling fees"
                  />
                </Grid>

                <Grid item xs={12} md={6}>
                  <DatePicker
                    label="Incurred Date"
                    value={formData.incurredDate}
                    onChange={(date) => setFormData(prev => ({ ...prev, incurredDate: date || new Date() }))}
                    disabled={loading}
                    slotProps={{ textField: { fullWidth: true } }}
                  />
                </Grid>

                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="Reference Document"
                    value={formData.referenceDocument}
                    onChange={(e) => setFormData(prev => ({ ...prev, referenceDocument: e.target.value }))}
                    disabled={loading}
                    placeholder="e.g., DMG-001, INV-123"
                  />
                </Grid>

                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Notes"
                    multiline
                    rows={2}
                    value={formData.notes}
                    onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
                    disabled={loading}
                  />
                </Grid>

                <Grid item xs={12}>
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <Button
                      variant="contained"
                      startIcon={loading ? <CircularProgress size={20} /> : <SaveIcon />}
                      onClick={handleSave}
                      disabled={loading}
                    >
                      {loading ? 'Saving...' : 'Save Charge'}
                    </Button>
                    <Button
                      variant="outlined"
                      startIcon={<CancelIcon />}
                      onClick={resetForm}
                      disabled={loading}
                    >
                      Cancel
                    </Button>
                  </Box>
                </Grid>
              </Grid>
            </Box>
          )}

          {/* Charges List */}
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h6">Charges List</Typography>
              {canEdit && editMode === 'none' && (
                <Button
                  variant="contained"
                  startIcon={<AddIcon />}
                  onClick={handleAdd}
                  disabled={loading}
                >
                  Add Charge
                </Button>
              )}
            </Box>

            {charges.length === 0 ? (
              <Alert severity="info">
                No charges have been added to this settlement.
                {canEdit && (
                  <>
                    {' '}Click "Add Charge" to get started.
                  </>
                )}
              </Alert>
            ) : (
              <TableContainer component={Paper} variant="outlined">
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Charge Type</TableCell>
                      <TableCell>Description</TableCell>
                      <TableCell align="right">Amount</TableCell>
                      <TableCell>Incurred Date</TableCell>
                      <TableCell>Reference</TableCell>
                      <TableCell>Created By</TableCell>
                      {canEdit && <TableCell align="center">Actions</TableCell>}
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {charges.map((charge) => (
                      <TableRow key={charge.id}>
                        <TableCell>
                          <Chip
                            label={charge.chargeTypeDisplayName}
                            color={getChargeTypeColor(charge.chargeType as unknown as ChargeType)}
                            size="small"
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{charge.description}</Typography>
                          {charge.notes && (
                            <Typography variant="caption" color="text.secondary" display="block">
                              {charge.notes}
                            </Typography>
                          )}
                        </TableCell>
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
                        {canEdit && (
                          <TableCell align="center">
                            <Box sx={{ display: 'flex', gap: 0.5 }}>
                              <Tooltip title="Edit Charge">
                                <IconButton
                                  size="small"
                                  onClick={() => handleEdit(charge)}
                                  disabled={loading || editMode !== 'none'}
                                >
                                  <EditIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                              <Tooltip title="Delete Charge">
                                <IconButton
                                  size="small"
                                  onClick={() => handleDelete(charge.id)}
                                  disabled={loading || editMode !== 'none'}
                                  color="error"
                                >
                                  <DeleteIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            </Box>
                          </TableCell>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>
        </DialogContent>

        <DialogActions>
          <Button onClick={onClose} disabled={loading}>
            {editMode !== 'none' ? 'Close' : 'Done'}
          </Button>
        </DialogActions>
      </Dialog>
    </LocalizationProvider>
  );
};