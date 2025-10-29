import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Box,
  Alert,
  CircularProgress,
  Autocomplete
} from '@mui/material';
import { 
  useCreateShippingOperation, 
  useUpdateShippingOperation, 
  useShippingOperation 
} from '@/hooks/useShipping';
import { 
  CreateShippingOperationDto, 
  UpdateShippingOperationDto, 
  COMMON_PORTS, 
  QUANTITY_UNITS 
} from '@/types/shipping';

interface ShippingOperationFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data?: any) => void;
  initialData?: { id: string } | undefined;
}

interface FormData {
  vesselName: string;
  imoNumber: string;
  contractId: string;
  plannedQuantity: string;
  quantityUnit: string;
  loadPortETA: string;
  dischargePortETA: string;
  loadPort: string;
  dischargePort: string;
  charterParty: string;
  notes: string;
}

const initialFormState: FormData = {
  vesselName: '',
  imoNumber: '',
  contractId: '',
  plannedQuantity: '',
  quantityUnit: 'MT',
  loadPortETA: '',
  dischargePortETA: '',
  loadPort: '',
  dischargePort: '',
  charterParty: '',
  notes: ''
};

export const ShippingOperationForm: React.FC<ShippingOperationFormProps> = ({
  open,
  onClose,
  onSubmit,
  initialData
}) => {
  const [formData, setFormData] = useState<FormData>(initialFormState);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  
  const isEditing = Boolean(initialData?.id);
  
  // Hooks for API operations
  const createMutation = useCreateShippingOperation();
  const updateMutation = useUpdateShippingOperation();
  
  // Load existing data when editing
  const { data: existingOperation, isLoading: loadingOperation } = useShippingOperation(
    initialData?.id || '',
    isEditing && open
  );

  // Initialize form data when editing
  useEffect(() => {
    if (isEditing && existingOperation) {
      setFormData({
        vesselName: existingOperation.vesselName || '',
        imoNumber: existingOperation.imoNumber || '',
        contractId: existingOperation.contractId || '',
        plannedQuantity: existingOperation.plannedQuantity?.value?.toString() || '',
        quantityUnit: existingOperation.plannedQuantity?.unit || 'MT',
        loadPortETA: existingOperation.loadPortATA ? 
          new Date(existingOperation.loadPortATA).toISOString().slice(0, 16) : '',
        dischargePortETA: existingOperation.dischargePortATA ? 
          new Date(existingOperation.dischargePortATA).toISOString().slice(0, 16) : '',
        loadPort: existingOperation.loadPort || '',
        dischargePort: existingOperation.dischargePort || '',
        charterParty: existingOperation.charterParty || '',
        notes: existingOperation.notes || ''
      });
    } else if (!isEditing) {
      setFormData(initialFormState);
    }
  }, [existingOperation, isEditing, open]);

  // Reset form when dialog opens
  useEffect(() => {
    if (open && !isEditing) {
      setFormData(initialFormState);
      setValidationErrors({});
    }
  }, [open, isEditing]);

  const handleInputChange = (field: keyof FormData, value: string) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
    
    // Clear validation error when user starts typing
    if (validationErrors[field]) {
      setValidationErrors(prev => ({
        ...prev,
        [field]: ''
      }));
    }
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.vesselName.trim()) {
      errors.vesselName = 'Vessel name is required';
    }

    if (!formData.contractId.trim()) {
      errors.contractId = 'Contract ID is required';
    }

    if (!formData.plannedQuantity.trim()) {
      errors.plannedQuantity = 'Planned quantity is required';
    } else if (isNaN(Number(formData.plannedQuantity)) || Number(formData.plannedQuantity) <= 0) {
      errors.plannedQuantity = 'Planned quantity must be a positive number';
    }

    if (!formData.loadPortETA.trim()) {
      errors.loadPortETA = 'Load Port ETA is required';
    }

    if (!formData.dischargePortETA.trim()) {
      errors.dischargePortETA = 'Discharge Port ETA is required';
    } else if (formData.loadPortETA && formData.dischargePortETA) {
      // Validate that discharge ETA is after load ETA
      const loadDate = new Date(formData.loadPortETA);
      const dischargeDate = new Date(formData.dischargePortETA);
      if (dischargeDate <= loadDate) {
        errors.dischargePortETA = 'Discharge Port ETA must be after Load Port ETA';
      }
    }

    // Validate that ETAs are in the future
    if (formData.loadPortETA) {
      const loadDate = new Date(formData.loadPortETA);
      if (loadDate <= new Date()) {
        errors.loadPortETA = 'Load Port ETA must be in the future';
      }
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    try {
      if (isEditing && initialData?.id) {
        const updateData: UpdateShippingOperationDto = {
          vesselName: formData.vesselName || undefined,
          imoNumber: formData.imoNumber || undefined,
          plannedQuantity: Number(formData.plannedQuantity) || undefined,
          plannedQuantityUnit: formData.quantityUnit || undefined,
          laycanStart: formData.loadPortETA ? new Date(formData.loadPortETA).toISOString() : undefined,
          laycanEnd: formData.dischargePortETA ? new Date(formData.dischargePortETA).toISOString() : undefined,
          notes: formData.notes || undefined,
        };

        await updateMutation.mutateAsync({
          id: initialData.id,
          operation: updateData
        });
      } else {
        // Create request - must include loadPortETA and dischargePortETA as required fields
        const loadPortETA = formData.loadPortETA ? new Date(formData.loadPortETA).toISOString() : '';
        const dischargePortETA = formData.dischargePortETA ? new Date(formData.dischargePortETA).toISOString() : '';

        const createData: CreateShippingOperationDto = {
          contractId: formData.contractId,
          vesselName: formData.vesselName,
          imoNumber: formData.imoNumber || undefined,
          plannedQuantity: Number(formData.plannedQuantity),
          plannedQuantityUnit: formData.quantityUnit,
          loadPortETA: loadPortETA,
          dischargePortETA: dischargePortETA,
          loadPort: formData.loadPort || undefined,
          dischargePort: formData.dischargePort || undefined,
          notes: formData.notes || undefined,
        };

        await createMutation.mutateAsync(createData);
      }

      onSubmit();
      onClose();
    } catch (error) {
      // Error handling is done by the mutations
      console.error('Form submission error:', error);
    }
  };

  const handleCancel = () => {
    setFormData(initialFormState);
    setValidationErrors({});
    onClose();
  };

  const isSubmitting = createMutation.isPending || updateMutation.isPending;
  const submitError = createMutation.error || updateMutation.error;

  // Show loading state when fetching existing operation
  if (isEditing && loadingOperation) {
    return (
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>Loading Operation...</DialogTitle>
        <DialogContent>
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
        </DialogActions>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onClose={handleCancel} maxWidth="md" fullWidth>
      <DialogTitle>
        {isEditing ? 'Edit Shipping Operation' : 'Create New Shipping Operation'}
      </DialogTitle>
      
      <DialogContent>
        {submitError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {(submitError as any)?.message || 'An error occurred while saving the operation'}
          </Alert>
        )}

        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Vessel Name *"
              value={formData.vesselName}
              onChange={(e) => handleInputChange('vesselName', e.target.value)}
              error={!!validationErrors.vesselName}
              helperText={validationErrors.vesselName}
              disabled={isSubmitting}
            />
          </Grid>
          
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="IMO Number"
              value={formData.imoNumber}
              onChange={(e) => handleInputChange('imoNumber', e.target.value)}
              disabled={isSubmitting}
              placeholder="e.g. IMO1234567"
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Contract ID *"
              value={formData.contractId}
              onChange={(e) => handleInputChange('contractId', e.target.value)}
              error={!!validationErrors.contractId}
              helperText={validationErrors.contractId}
              disabled={isSubmitting || isEditing} // Disable editing contract ID
            />
          </Grid>

          <Grid item xs={12} sm={3}>
            <TextField
              fullWidth
              label="Planned Quantity *"
              type="number"
              value={formData.plannedQuantity}
              onChange={(e) => handleInputChange('plannedQuantity', e.target.value)}
              error={!!validationErrors.plannedQuantity}
              helperText={validationErrors.plannedQuantity}
              disabled={isSubmitting}
              inputProps={{ min: 0, step: 0.01 }}
            />
          </Grid>

          <Grid item xs={12} sm={3}>
            <FormControl fullWidth error={!!validationErrors.quantityUnit}>
              <InputLabel>Unit *</InputLabel>
              <Select
                value={formData.quantityUnit}
                label="Unit *"
                onChange={(e) => handleInputChange('quantityUnit', e.target.value)}
                disabled={isSubmitting}
              >
                {QUANTITY_UNITS.map((unit) => (
                  <MenuItem key={unit.value} value={unit.value}>
                    {unit.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          <Grid item xs={12} sm={6}>
            <Autocomplete
              freeSolo
              options={COMMON_PORTS}
              value={formData.loadPort}
              onChange={(_, value) => handleInputChange('loadPort', value || '')}
              onInputChange={(_, value) => handleInputChange('loadPort', value)}
              renderInput={(params) => (
                <TextField
                  {...params}
                  fullWidth
                  label="Load Port"
                  helperText="Optional - Port information"
                  disabled={isSubmitting}
                />
              )}
              disabled={isSubmitting}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <Autocomplete
              freeSolo
              options={COMMON_PORTS}
              value={formData.dischargePort}
              onChange={(_, value) => handleInputChange('dischargePort', value || '')}
              onInputChange={(_, value) => handleInputChange('dischargePort', value)}
              renderInput={(params) => (
                <TextField
                  {...params}
                  fullWidth
                  label="Discharge Port"
                  helperText="Optional - Port information"
                  disabled={isSubmitting}
                />
              )}
              disabled={isSubmitting}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Load Port ETA *"
              type="datetime-local"
              value={formData.loadPortETA}
              onChange={(e) => handleInputChange('loadPortETA', e.target.value)}
              error={!!validationErrors.loadPortETA}
              helperText={validationErrors.loadPortETA}
              InputLabelProps={{ shrink: true }}
              disabled={isSubmitting}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Discharge Port ETA *"
              type="datetime-local"
              value={formData.dischargePortETA}
              onChange={(e) => handleInputChange('dischargePortETA', e.target.value)}
              error={!!validationErrors.dischargePortETA}
              helperText={validationErrors.dischargePortETA}
              InputLabelProps={{ shrink: true }}
              disabled={isSubmitting}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Charter Party"
              value={formData.charterParty}
              onChange={(e) => handleInputChange('charterParty', e.target.value)}
              disabled={isSubmitting}
              placeholder="Charter party reference"
            />
          </Grid>

          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Notes"
              multiline
              rows={3}
              value={formData.notes}
              onChange={(e) => handleInputChange('notes', e.target.value)}
              disabled={isSubmitting}
              placeholder="Additional notes about this shipping operation..."
            />
          </Grid>
        </Grid>
      </DialogContent>

      <DialogActions>
        <Button onClick={handleCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button 
          onClick={handleSubmit} 
          variant="contained"
          disabled={isSubmitting}
          startIcon={isSubmitting ? <CircularProgress size={16} /> : null}
        >
          {isEditing ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};