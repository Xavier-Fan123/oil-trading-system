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
  Typography
} from '@mui/material';
import { inventoryApi } from '@/services/inventoryApi';
import { InventoryMovement, CreateInventoryMovementRequest, UpdateInventoryMovementRequest, InventoryLocation, QuantityUnit, InventoryMovementType, InventoryMovementStatus } from '@/types/inventory';

interface MovementModalProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  movement?: InventoryMovement | null;
  locations: InventoryLocation[];
}

const MovementModal: React.FC<MovementModalProps> = ({
  open,
  onClose,
  onSuccess,
  movement,
  locations
}) => {
  const [formData, setFormData] = useState({
    fromLocationId: '',
    toLocationId: '',
    productId: '',
    quantity: 0,
    quantityUnit: 'MT',
    movementType: 'Transfer',
    movementDate: new Date().toISOString().split('T')[0],
    plannedDate: new Date().toISOString().split('T')[0],
    transportMode: '',
    vesselName: '',
    transportReference: '',
    transportCost: 0,
    handlingCost: 0,
    costCurrency: 'USD',
    notes: '',
    purchaseContractId: '',
    salesContractId: '',
    shippingOperationId: ''
  });
  
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [products, setProducts] = useState<any[]>([]);

  const quantityUnits = ['MT', 'BBL', 'GALLON', 'LITER'];
  const movementTypes = ['Receipt', 'Shipment', 'Transfer', 'Blending', 'Loss', 'Adjustment'];
  const transportModes = ['Vessel', 'Truck', 'Rail', 'Pipeline', 'Barge'];
  const currencies = ['USD', 'EUR', 'GBP', 'JPY', 'CNY'];

  // Load products when component mounts
  useEffect(() => {
    const loadProducts = async () => {
      try {
        // Mock products for now
        const mockProducts = [
          { id: '1', name: 'Brent Crude', code: 'BRENT' },
          { id: '2', name: 'WTI Crude', code: 'WTI' },
          { id: '3', name: 'Diesel', code: 'DIESEL' },
          { id: '4', name: 'Gasoline', code: 'GASOLINE' },
          { id: '5', name: 'Jet Fuel', code: 'JET' },
          { id: '6', name: 'Fuel Oil', code: 'FO' }
        ];
        setProducts(mockProducts);
      } catch (error) {
        console.error('Error loading products:', error);
      }
    };

    if (open) {
      loadProducts();
    }
  }, [open]);

  useEffect(() => {
    if (movement) {
      // Edit mode - populate form with existing data
      setFormData({
        fromLocationId: movement.fromLocationId,
        toLocationId: movement.toLocationId,
        productId: movement.productId,
        quantity: movement.quantity,
        quantityUnit: movement.quantityUnit,
        movementType: movement.movementType.toString(),
        movementDate: new Date(movement.movementDate).toISOString().split('T')[0],
        plannedDate: movement.plannedDate ? new Date(movement.plannedDate).toISOString().split('T')[0] : '',
        transportMode: movement.transportMode || '',
        vesselName: movement.vesselName || '',
        transportReference: movement.transportReference || '',
        transportCost: movement.transportCost || 0,
        handlingCost: movement.handlingCost || 0,
        costCurrency: movement.costCurrency || 'USD',
        notes: movement.notes || '',
        purchaseContractId: movement.purchaseContractId || '',
        salesContractId: movement.salesContractId || '',
        shippingOperationId: movement.shippingOperationId || ''
      });
    } else {
      // Add mode - reset form
      setFormData({
        fromLocationId: '',
        toLocationId: '',
        productId: '',
        quantity: 0,
        quantityUnit: 'MT',
        movementType: 'Transfer',
        movementDate: new Date().toISOString().split('T')[0],
        plannedDate: new Date().toISOString().split('T')[0],
        transportMode: '',
        vesselName: '',
        transportReference: '',
        transportCost: 0,
        handlingCost: 0,
        costCurrency: 'USD',
        notes: '',
        purchaseContractId: '',
        salesContractId: '',
        shippingOperationId: ''
      });
    }
    setErrors({});
  }, [movement, open]);

  const handleChange = (field: string, value: any) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: ''
      }));
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.fromLocationId) {
      newErrors.fromLocationId = 'From location is required';
    }
    if (!formData.toLocationId) {
      newErrors.toLocationId = 'To location is required';
    }
    if (formData.fromLocationId === formData.toLocationId) {
      newErrors.toLocationId = 'From and To locations cannot be the same';
    }
    if (!formData.productId) {
      newErrors.productId = 'Product is required';
    }
    if (formData.quantity <= 0) {
      newErrors.quantity = 'Quantity must be greater than 0';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    setLoading(true);
    try {
      if (movement) {
        // Edit mode
        const updateRequest: UpdateInventoryMovementRequest = {
          ...formData,
          id: movement.id,
          movementDate: formData.movementDate,
          plannedDate: formData.plannedDate,
          quantityUnit: formData.quantityUnit as QuantityUnit,
          movementType: formData.movementType as InventoryMovementType,
          status: movement.status as InventoryMovementStatus
        };
        await inventoryApi.movements.update(movement.id, updateRequest);
      } else {
        // Add mode
        const createRequest: CreateInventoryMovementRequest = {
          ...formData,
          movementDate: formData.movementDate,
          plannedDate: formData.plannedDate,
          quantityUnit: formData.quantityUnit as QuantityUnit,
          movementType: formData.movementType as InventoryMovementType
        };
        await inventoryApi.movements.create(createRequest);
      }
      
      onSuccess();
      onClose();
    } catch (error: any) {
      console.error('Error saving movement:', error);
      alert(error.response?.data || 'Failed to save movement');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {movement ? 'Edit Inventory Movement' : 'Create New Inventory Movement'}
      </DialogTitle>
      
      <DialogContent>
        <Grid container spacing={3} sx={{ mt: 1 }}>
          {/* Basic Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom>
              Basic Information
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth required error={!!errors.fromLocationId}>
              <InputLabel>From Location</InputLabel>
              <Select
                value={formData.fromLocationId}
                onChange={(e) => handleChange('fromLocationId', e.target.value)}
                label="From Location"
              >
                {locations.map(location => (
                  <MenuItem key={location.id} value={location.id}>
                    {location.locationCode} - {location.locationName}
                  </MenuItem>
                ))}
              </Select>
              {errors.fromLocationId && (
                <Typography variant="caption" color="error">
                  {errors.fromLocationId}
                </Typography>
              )}
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth required error={!!errors.toLocationId}>
              <InputLabel>To Location</InputLabel>
              <Select
                value={formData.toLocationId}
                onChange={(e) => handleChange('toLocationId', e.target.value)}
                label="To Location"
              >
                {locations.map(location => (
                  <MenuItem key={location.id} value={location.id}>
                    {location.locationCode} - {location.locationName}
                  </MenuItem>
                ))}
              </Select>
              {errors.toLocationId && (
                <Typography variant="caption" color="error">
                  {errors.toLocationId}
                </Typography>
              )}
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth required error={!!errors.productId}>
              <InputLabel>Product</InputLabel>
              <Select
                value={formData.productId}
                onChange={(e) => handleChange('productId', e.target.value)}
                label="Product"
              >
                {products.map(product => (
                  <MenuItem key={product.id} value={product.id}>
                    {product.code} - {product.name}
                  </MenuItem>
                ))}
              </Select>
              {errors.productId && (
                <Typography variant="caption" color="error">
                  {errors.productId}
                </Typography>
              )}
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Movement Type</InputLabel>
              <Select
                value={formData.movementType}
                onChange={(e) => handleChange('movementType', e.target.value)}
                label="Movement Type"
              >
                {movementTypes.map(type => (
                  <MenuItem key={type} value={type}>{type}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          {/* Quantity Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Quantity & Timing
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Quantity"
              type="number"
              value={formData.quantity}
              onChange={(e) => handleChange('quantity', parseFloat(e.target.value) || 0)}
              error={!!errors.quantity}
              helperText={errors.quantity}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={4}>
            <FormControl fullWidth>
              <InputLabel>Quantity Unit</InputLabel>
              <Select
                value={formData.quantityUnit}
                onChange={(e) => handleChange('quantityUnit', e.target.value)}
                label="Quantity Unit"
              >
                {quantityUnits.map(unit => (
                  <MenuItem key={unit} value={unit}>{unit}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Movement Date"
              type="date"
              value={formData.movementDate}
              onChange={(e) => handleChange('movementDate', e.target.value)}
              InputLabelProps={{ shrink: true }}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Planned Date"
              type="date"
              value={formData.plannedDate}
              onChange={(e) => handleChange('plannedDate', e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
          </Grid>

          {/* Transport Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Transport Information
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Transport Mode</InputLabel>
              <Select
                value={formData.transportMode}
                onChange={(e) => handleChange('transportMode', e.target.value)}
                label="Transport Mode"
              >
                {transportModes.map(mode => (
                  <MenuItem key={mode} value={mode}>{mode}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Vessel Name"
              value={formData.vesselName}
              onChange={(e) => handleChange('vesselName', e.target.value)}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Transport Reference"
              value={formData.transportReference}
              onChange={(e) => handleChange('transportReference', e.target.value)}
            />
          </Grid>

          {/* Cost Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Cost Information
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Transport Cost"
              type="number"
              value={formData.transportCost}
              onChange={(e) => handleChange('transportCost', parseFloat(e.target.value) || 0)}
            />
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Handling Cost"
              type="number"
              value={formData.handlingCost}
              onChange={(e) => handleChange('handlingCost', parseFloat(e.target.value) || 0)}
            />
          </Grid>
          
          <Grid item xs={12} md={4}>
            <FormControl fullWidth>
              <InputLabel>Cost Currency</InputLabel>
              <Select
                value={formData.costCurrency}
                onChange={(e) => handleChange('costCurrency', e.target.value)}
                label="Cost Currency"
              >
                {currencies.map(currency => (
                  <MenuItem key={currency} value={currency}>{currency}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Notes"
              value={formData.notes}
              onChange={(e) => handleChange('notes', e.target.value)}
              multiline
              rows={3}
            />
          </Grid>
        </Grid>
      </DialogContent>
      
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading}>
          {loading ? 'Saving...' : (movement ? 'Update' : 'Create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default MovementModal;