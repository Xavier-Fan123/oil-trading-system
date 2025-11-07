import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardActions,
  TextField,
  Button,
  Grid,
  Typography,
  Switch,
  FormControlLabel,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Save as SaveIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { SettlementTemplate } from '@/services/templateApi';
import { SettlementTemplateConfig, DefaultChargeItem, defaultTemplateConfig } from '@/types/templates';
import { ChargeTypeLabels, ChargeType } from '@/types/settlement';

interface TemplateFormProps {
  template?: SettlementTemplate | null;
  onSave: (
    name: string,
    description: string,
    config: SettlementTemplateConfig,
    isPublic: boolean
  ) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
}

interface ChargeItemFormData {
  chargeType: ChargeType;
  chargeTypeLabel: string;
  description: string;
  amount: number;
  currency: string;
  isFixed: boolean;
  includeByDefault: boolean;
}

export const TemplateForm: React.FC<TemplateFormProps> = ({
  template,
  onSave,
  onCancel,
  isLoading = false,
}) => {
  const [name, setName] = useState(template?.name || '');
  const [description, setDescription] = useState(template?.description || '');
  const [isPublic, setIsPublic] = useState(template?.isPublic || false);
  const [config, setConfig] = useState<SettlementTemplateConfig>(
    template && template.templateConfiguration
      ? JSON.parse(template.templateConfiguration)
      : defaultTemplateConfig
  );
  const [saveLoading, setSaveLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [addChargeOpen, setAddChargeOpen] = useState(false);
  const [editChargeIndex, setEditChargeIndex] = useState<number | null>(null);
  const [chargeForm, setChargeForm] = useState<ChargeItemFormData>({
    chargeType: ChargeType.Demurrage,
    chargeTypeLabel: ChargeTypeLabels[ChargeType.Demurrage],
    description: '',
    amount: 0,
    currency: 'USD',
    isFixed: true,
    includeByDefault: true,
  });

  const handleSave = async () => {
    // Validation
    if (!name.trim()) {
      setError('Template name is required');
      return;
    }

    if (!description.trim()) {
      setError('Template description is required');
      return;
    }

    setSaveLoading(true);
    setError(null);

    try {
      await onSave(name, description, config, isPublic);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save template');
    } finally {
      setSaveLoading(false);
    }
  };

  const handleAddCharge = () => {
    setEditChargeIndex(null);
    setChargeForm({
      chargeType: ChargeType.Demurrage,
      chargeTypeLabel: ChargeTypeLabels[ChargeType.Demurrage],
      description: '',
      amount: 0,
      currency: 'USD',
      isFixed: true,
      includeByDefault: true,
    });
    setAddChargeOpen(true);
  };

  const handleEditCharge = (index: number) => {
    const charge = config.defaultCharges[index];
    setChargeForm({
      chargeType: charge.chargeType,
      chargeTypeLabel: charge.chargeTypeLabel,
      description: charge.description,
      amount: charge.amount,
      currency: charge.currency,
      isFixed: charge.isFixed,
      includeByDefault: charge.includeByDefault,
    });
    setEditChargeIndex(index);
    setAddChargeOpen(true);
  };

  const handleSaveCharge = () => {
    if (!chargeForm.description.trim()) {
      setError('Charge description is required');
      return;
    }

    if (chargeForm.amount <= 0) {
      setError('Charge amount must be greater than 0');
      return;
    }

    const newCharges = [...config.defaultCharges];

    if (editChargeIndex !== null) {
      newCharges[editChargeIndex] = chargeForm;
    } else {
      newCharges.push(chargeForm);
    }

    setConfig({ ...config, defaultCharges: newCharges });
    setAddChargeOpen(false);
    setError(null);
  };

  const handleDeleteCharge = (index: number) => {
    const newCharges = config.defaultCharges.filter((_, i) => i !== index);
    setConfig({ ...config, defaultCharges: newCharges });
  };

  const handleChargeTypeChange = (chargeType: ChargeType) => {
    setChargeForm({
      ...chargeForm,
      chargeType,
      chargeTypeLabel: ChargeTypeLabels[chargeType],
    });
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          {template ? 'Edit Settlement Template' : 'Create New Settlement Template'}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Grid container spacing={2}>
          {/* Basic Information */}
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Template Name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., Standard Oil Settlement"
              disabled={saveLoading}
            />
          </Grid>

          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe what this template is used for"
              multiline
              rows={3}
              disabled={saveLoading}
            />
          </Grid>

          {/* Visibility */}
          <Grid item xs={12}>
            <FormControlLabel
              control={
                <Switch
                  checked={isPublic}
                  onChange={(e) => setIsPublic(e.target.checked)}
                  disabled={saveLoading}
                />
              }
              label="Make this template public (share with other users)"
            />
          </Grid>

          {/* Configuration Settings */}
          <Grid item xs={12}>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle1" gutterBottom>
              Template Configuration
            </Typography>
          </Grid>

          <Grid item xs={6}>
            <TextField
              fullWidth
              label="Default Currency"
              value={config.defaultCurrency}
              onChange={(e) =>
                setConfig({ ...config, defaultCurrency: e.target.value })
              }
              placeholder="USD"
              disabled={saveLoading}
            />
          </Grid>

          <Grid item xs={6}>
            <FormControlLabel
              control={
                <Switch
                  checked={config.autoCalculatePrices}
                  onChange={(e) =>
                    setConfig({ ...config, autoCalculatePrices: e.target.checked })
                  }
                  disabled={saveLoading}
                />
              }
              label="Auto-calculate prices"
            />
          </Grid>

          {/* Default Charges */}
          <Grid item xs={12}>
            <Divider sx={{ my: 2 }} />
            <Box
              sx={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                mb: 2,
              }}
            >
              <Typography variant="subtitle1">
                Default Charges ({config.defaultCharges.length})
              </Typography>
              <Button
                startIcon={<AddIcon />}
                onClick={handleAddCharge}
                disabled={saveLoading}
                size="small"
              >
                Add Charge
              </Button>
            </Box>

            {config.defaultCharges.length > 0 && (
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableBody>
                    {config.defaultCharges.map((charge, idx) => (
                      <React.Fragment key={idx}>
                        <TableCell sx={{ fontWeight: 600, width: '25%' }}>
                          {charge.chargeTypeLabel}
                        </TableCell>
                        <TableCell sx={{ width: '20%' }}>
                          {charge.amount} {config.defaultCurrency}
                          {charge.isFixed && ' (Fixed)'}
                        </TableCell>
                        <TableCell sx={{ width: '40%' }}>
                          {charge.description}
                          {charge.includeByDefault && (
                            <span style={{ marginLeft: 8 }}>
                              [Default]
                            </span>
                          )}
                        </TableCell>
                        <TableCell align="right" sx={{ width: '15%' }}>
                          <IconButton
                            size="small"
                            onClick={() => handleEditCharge(idx)}
                            disabled={saveLoading}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => handleDeleteCharge(idx)}
                            disabled={saveLoading}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      </React.Fragment>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}

            {config.defaultCharges.length === 0 && (
              <Typography variant="caption" color="textSecondary">
                No default charges configured. Click "Add Charge" to add charges that
                will automatically be included in settlements using this template.
              </Typography>
            )}
          </Grid>

          {/* Notes */}
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Additional Notes"
              value={config.notes || ''}
              onChange={(e) =>
                setConfig({ ...config, notes: e.target.value })
              }
              placeholder="Any additional notes or instructions for users of this template"
              multiline
              rows={2}
              disabled={saveLoading}
            />
          </Grid>
        </Grid>
      </CardContent>

      <CardActions sx={{ justifyContent: 'space-between' }}>
        <Button onClick={onCancel} disabled={saveLoading}>
          Cancel
        </Button>
        <Button
          variant="contained"
          startIcon={<SaveIcon />}
          onClick={handleSave}
          disabled={saveLoading}
        >
          {saveLoading ? <CircularProgress size={24} /> : 'Save Template'}
        </Button>
      </CardActions>

      {/* Charge Form Dialog */}
      <Dialog
        open={addChargeOpen}
        onClose={() => setAddChargeOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          {editChargeIndex !== null ? 'Edit Charge' : 'Add New Charge'}
        </DialogTitle>
        <DialogContent dividers>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Charge Type</InputLabel>
              <Select
                value={chargeForm.chargeType}
                onChange={(e) => handleChargeTypeChange(e.target.value as ChargeType)}
                label="Charge Type"
              >
                {Object.entries(ChargeTypeLabels).map(([key, label]) => (
                  <MenuItem key={key} value={parseInt(key)}>
                    {label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <TextField
              fullWidth
              label="Description"
              value={chargeForm.description}
              onChange={(e) =>
                setChargeForm({ ...chargeForm, description: e.target.value })
              }
              placeholder="e.g., Port demurrage charges"
            />

            <TextField
              fullWidth
              label="Amount"
              type="number"
              value={chargeForm.amount}
              onChange={(e) =>
                setChargeForm({ ...chargeForm, amount: parseFloat(e.target.value) })
              }
              inputProps={{ step: '0.01', min: '0' }}
            />

            <TextField
              fullWidth
              label="Currency"
              value={chargeForm.currency}
              onChange={(e) =>
                setChargeForm({ ...chargeForm, currency: e.target.value })
              }
              placeholder="USD"
            />

            <FormControlLabel
              control={
                <Switch
                  checked={chargeForm.isFixed}
                  onChange={(e) =>
                    setChargeForm({ ...chargeForm, isFixed: e.target.checked })
                  }
                />
              }
              label="Fixed amount (unchecked = percentage-based)"
            />

            <FormControlLabel
              control={
                <Switch
                  checked={chargeForm.includeByDefault}
                  onChange={(e) =>
                    setChargeForm({ ...chargeForm, includeByDefault: e.target.checked })
                  }
                />
              }
              label="Include by default in settlements"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddChargeOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleSaveCharge}>
            {editChargeIndex !== null ? 'Update' : 'Add'} Charge
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};

export default TemplateForm;
