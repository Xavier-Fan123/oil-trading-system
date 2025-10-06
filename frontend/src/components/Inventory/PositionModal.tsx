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
  Typography,
} from '@mui/material';
import { inventoryApi } from '@/services/inventoryApi';
import { InventoryPosition, CreateInventoryPositionRequest, UpdateInventoryPositionRequest, InventoryLocation, QuantityUnit, InventoryStatus } from '@/types/inventory';

interface PositionModalProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  position?: InventoryPosition | null;
  locations: InventoryLocation[];
}

const PositionModal: React.FC<PositionModalProps> = ({
  open,
  onClose,
  onSuccess,
  position,
  locations
}) => {
  const [formData, setFormData] = useState({
    locationId: '',
    productId: '',
    quantity: 0,
    quantityUnit: 'MT',
    averageCost: 0,
    currency: 'USD',
    grade: '',
    batchReference: '',
    sulfur: 0,
    api: 0,
    viscosity: 0,
    qualityNotes: '',
    receivedDate: new Date().toISOString().split('T')[0],
    status: 'Available',
    statusNotes: ''
  });
  
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [products, setProducts] = useState<any[]>([]);

  const quantityUnits = ['MT', 'BBL', 'GALLON', 'LITER'];
  const currencies = ['USD', 'EUR', 'GBP', 'JPY', 'CNY'];
  const statuses = ['Available', 'Reserved', 'InTransit', 'Quality', 'Blocked', 'Contaminated', 'Aged'];

  // Load products when component mounts
  useEffect(() => {
    const loadProducts = async () => {
      try {
        // You'll need to implement this API call
        // const productsData = await inventoryApi.products.getAll();
        // setProducts(productsData);
        
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
    if (position) {
      // Edit mode - populate form with existing data
      setFormData({
        locationId: position.locationId,
        productId: position.productId,
        quantity: position.quantity,
        quantityUnit: position.quantityUnit,
        averageCost: position.averageCost,
        currency: position.currency,
        grade: position.grade || '',
        batchReference: position.batchReference || '',
        sulfur: position.sulfur || 0,
        api: position.api || 0,
        viscosity: position.viscosity || 0,
        qualityNotes: position.qualityNotes || '',
        receivedDate: position.receivedDate ? new Date(position.receivedDate).toISOString().split('T')[0] : new Date().toISOString().split('T')[0],
        status: position.status,
        statusNotes: position.statusNotes || ''
      });
    } else {
      // Add mode - reset form
      setFormData({
        locationId: '',
        productId: '',
        quantity: 0,
        quantityUnit: 'MT',
        averageCost: 0,
        currency: 'USD',
        grade: '',
        batchReference: '',
        sulfur: 0,
        api: 0,
        viscosity: 0,
        qualityNotes: '',
        receivedDate: new Date().toISOString().split('T')[0],
        status: 'Available',
        statusNotes: ''
      });
    }
    setErrors({});
  }, [position, open]);

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

    if (!formData.locationId) {
      newErrors.locationId = 'Location is required';
    }
    if (!formData.productId) {
      newErrors.productId = 'Product is required';
    }
    if (formData.quantity <= 0) {
      newErrors.quantity = 'Quantity must be greater than 0';
    }
    if (formData.averageCost <= 0) {
      newErrors.averageCost = 'Average cost must be greater than 0';
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
      if (position) {
        // Edit mode
        const updateRequest: UpdateInventoryPositionRequest = {
          ...formData,
          id: position.id,
          receivedDate: formData.receivedDate,
          quantityUnit: formData.quantityUnit as QuantityUnit,
          status: formData.status as InventoryStatus
        };
        await inventoryApi.positions.update(position.id, updateRequest);
      } else {
        // Add mode
        const createRequest: CreateInventoryPositionRequest = {
          ...formData,
          receivedDate: formData.receivedDate,
          quantityUnit: formData.quantityUnit as QuantityUnit,
          status: formData.status as InventoryStatus
        };
        await inventoryApi.positions.create(createRequest);
      }
      
      onSuccess();
      onClose();
    } catch (error: any) {
      console.error('Error saving position:', error);
      alert(error.response?.data || 'Failed to save position');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {position ? 'Edit Inventory Position' : 'Add New Inventory Position'}
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
            <FormControl fullWidth required error={!!errors.locationId}>
              <InputLabel>Location</InputLabel>
              <Select
                value={formData.locationId}
                onChange={(e) => handleChange('locationId', e.target.value)}
                label="Location"
              >
                {locations.map(location => (
                  <MenuItem key={location.id} value={location.id}>
                    {location.locationCode} - {location.locationName}
                  </MenuItem>
                ))}
              </Select>
              {errors.locationId && (
                <Typography variant="caption" color="error">
                  {errors.locationId}
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

          {/* Quantity & Pricing */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Quantity & Pricing
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
              label="Average Cost"
              type="number"
              value={formData.averageCost}
              onChange={(e) => handleChange('averageCost', parseFloat(e.target.value) || 0)}
              error={!!errors.averageCost}
              helperText={errors.averageCost}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Currency</InputLabel>
              <Select
                value={formData.currency}
                onChange={(e) => handleChange('currency', e.target.value)}
                label="Currency"
              >
                {currencies.map(currency => (
                  <MenuItem key={currency} value={currency}>{currency}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          {/* Quality Specifications */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Quality Specifications
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Grade"
              value={formData.grade}
              onChange={(e) => handleChange('grade', e.target.value)}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Batch Reference"
              value={formData.batchReference}
              onChange={(e) => handleChange('batchReference', e.target.value)}
            />
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Sulfur Content (%)"
              type="number"
              value={formData.sulfur}
              onChange={(e) => handleChange('sulfur', parseFloat(e.target.value) || 0)}
            />
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="API Gravity"
              type="number"
              value={formData.api}
              onChange={(e) => handleChange('api', parseFloat(e.target.value) || 0)}
            />
          </Grid>
          
          <Grid item xs={12} md={4}>
            <TextField
              fullWidth
              label="Viscosity"
              type="number"
              value={formData.viscosity}
              onChange={(e) => handleChange('viscosity', parseFloat(e.target.value) || 0)}
            />
          </Grid>
          
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Quality Notes"
              value={formData.qualityNotes}
              onChange={(e) => handleChange('qualityNotes', e.target.value)}
              multiline
              rows={2}
            />
          </Grid>

          {/* Status Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Status Information
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Received Date"
              type="date"
              value={formData.receivedDate}
              onChange={(e) => handleChange('receivedDate', e.target.value)}
              InputLabelProps={{ shrink: true }}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Status</InputLabel>
              <Select
                value={formData.status}
                onChange={(e) => handleChange('status', e.target.value)}
                label="Status"
              >
                {statuses.map(status => (
                  <MenuItem key={status} value={status}>{status}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Status Notes"
              value={formData.statusNotes}
              onChange={(e) => handleChange('statusNotes', e.target.value)}
              multiline
              rows={2}
            />
          </Grid>
        </Grid>
      </DialogContent>
      
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading}>
          {loading ? 'Saving...' : (position ? 'Update' : 'Create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default PositionModal;