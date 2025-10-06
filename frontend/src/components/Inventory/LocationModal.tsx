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
  FormControlLabel,
  Checkbox,
  Typography,
  Box,
  Chip,
  Autocomplete
} from '@mui/material';
import { inventoryApi } from '@/services/inventoryApi';
import { InventoryLocation, CreateInventoryLocationRequest, UpdateInventoryLocationRequest } from '@/types/inventory';

interface LocationModalProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  location?: InventoryLocation | null;
}

const LocationModal: React.FC<LocationModalProps> = ({
  open,
  onClose,
  onSuccess,
  location
}) => {
  const [formData, setFormData] = useState({
    locationCode: '',
    locationName: '',
    locationType: 'Terminal',
    country: '',
    region: '',
    address: '',
    coordinates: '',
    operatorName: '',
    contactInfo: '',
    totalCapacity: 0,
    capacityUnit: 'MT',
    supportedProducts: [] as string[],
    handlingServices: [] as string[],
    hasRailAccess: false,
    hasRoadAccess: false,
    hasSeaAccess: false,
    hasPipelineAccess: false,
    isActive: true
  });
  
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const locationTypes = [
    'Terminal',
    'Tank', 
    'Refinery',
    'Port',
    'Pipeline',
    'Storage',
    'Floating'
  ];

  const capacityUnits = ['MT', 'BBL', 'GALLON', 'LITER'];

  const productOptions = [
    'Brent Crude',
    'WTI Crude',
    'Diesel',
    'Gasoline',
    'Jet Fuel',
    'Fuel Oil',
    'Marine Gas Oil',
    'Heating Oil'
  ];

  const serviceOptions = [
    'Storage',
    'Blending',
    'Heating',
    'Quality Testing',
    'Cargo Inspection',
    'Bunker Services',
    'Transfer Services',
    'Tank Cleaning'
  ];

  useEffect(() => {
    if (location) {
      // Edit mode - populate form with existing data
      setFormData({
        locationCode: location.locationCode,
        locationName: location.locationName,
        locationType: location.locationType,
        country: location.country,
        region: location.region,
        address: location.address || '',
        coordinates: location.coordinates || '',
        operatorName: location.operatorName || '',
        contactInfo: location.contactInfo || '',
        totalCapacity: location.totalCapacity,
        capacityUnit: location.capacityUnit,
        supportedProducts: location.supportedProducts || [],
        handlingServices: location.handlingServices || [],
        hasRailAccess: location.hasRailAccess,
        hasRoadAccess: location.hasRoadAccess,
        hasSeaAccess: location.hasSeaAccess,
        hasPipelineAccess: location.hasPipelineAccess,
        isActive: location.isActive
      });
    } else {
      // Add mode - reset form
      setFormData({
        locationCode: '',
        locationName: '',
        locationType: 'Terminal',
        country: '',
        region: '',
        address: '',
        coordinates: '',
        operatorName: '',
        contactInfo: '',
        totalCapacity: 0,
        capacityUnit: 'MT',
        supportedProducts: [],
        handlingServices: [],
        hasRailAccess: false,
        hasRoadAccess: false,
        hasSeaAccess: false,
        hasPipelineAccess: false,
        isActive: true
      });
    }
    setErrors({});
  }, [location, open]);

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

    if (!formData.locationCode.trim()) {
      newErrors.locationCode = 'Location code is required';
    }
    if (!formData.locationName.trim()) {
      newErrors.locationName = 'Location name is required';
    }
    if (!formData.country.trim()) {
      newErrors.country = 'Country is required';
    }
    if (!formData.region.trim()) {
      newErrors.region = 'Region is required';
    }
    if (formData.totalCapacity <= 0) {
      newErrors.totalCapacity = 'Total capacity must be greater than 0';
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
      if (location) {
        // Edit mode
        const updateRequest: UpdateInventoryLocationRequest = {
          ...formData,
          id: location.id,
          locationType: formData.locationType as any,
          capacityUnit: formData.capacityUnit as any
        };
        await inventoryApi.locations.update(location.id, updateRequest);
      } else {
        // Add mode
        const createRequest: CreateInventoryLocationRequest = {
          ...formData,
          locationType: formData.locationType as any,
          capacityUnit: formData.capacityUnit as any
        };
        await inventoryApi.locations.create(createRequest);
      }
      
      onSuccess();
      onClose();
    } catch (error: any) {
      console.error('Error saving location:', error);
      alert(error.response?.data || 'Failed to save location');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {location ? 'Edit Location' : 'Add New Location'}
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
            <TextField
              fullWidth
              label="Location Code"
              value={formData.locationCode}
              onChange={(e) => handleChange('locationCode', e.target.value)}
              error={!!errors.locationCode}
              helperText={errors.locationCode}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Location Name"
              value={formData.locationName}
              onChange={(e) => handleChange('locationName', e.target.value)}
              error={!!errors.locationName}
              helperText={errors.locationName}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Location Type</InputLabel>
              <Select
                value={formData.locationType}
                onChange={(e) => handleChange('locationType', e.target.value)}
                label="Location Type"
              >
                {locationTypes.map(type => (
                  <MenuItem key={type} value={type}>{type}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={formData.isActive}
                  onChange={(e) => handleChange('isActive', e.target.checked)}
                />
              }
              label="Active"
            />
          </Grid>

          {/* Location Details */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Location Details
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Country"
              value={formData.country}
              onChange={(e) => handleChange('country', e.target.value)}
              error={!!errors.country}
              helperText={errors.country}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Region"
              value={formData.region}
              onChange={(e) => handleChange('region', e.target.value)}
              error={!!errors.region}
              helperText={errors.region}
              required
            />
          </Grid>
          
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Address"
              value={formData.address}
              onChange={(e) => handleChange('address', e.target.value)}
              multiline
              rows={2}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="GPS Coordinates"
              value={formData.coordinates}
              onChange={(e) => handleChange('coordinates', e.target.value)}
              placeholder="e.g., 40.7128, -74.0060"
            />
          </Grid>

          {/* Capacity Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Capacity Information
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Total Capacity"
              type="number"
              value={formData.totalCapacity}
              onChange={(e) => handleChange('totalCapacity', parseFloat(e.target.value) || 0)}
              error={!!errors.totalCapacity}
              helperText={errors.totalCapacity}
              required
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <FormControl fullWidth>
              <InputLabel>Capacity Unit</InputLabel>
              <Select
                value={formData.capacityUnit}
                onChange={(e) => handleChange('capacityUnit', e.target.value)}
                label="Capacity Unit"
              >
                {capacityUnits.map(unit => (
                  <MenuItem key={unit} value={unit}>{unit}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          {/* Operational Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Operational Information
            </Typography>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Operator Name"
              value={formData.operatorName}
              onChange={(e) => handleChange('operatorName', e.target.value)}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <TextField
              fullWidth
              label="Contact Information"
              value={formData.contactInfo}
              onChange={(e) => handleChange('contactInfo', e.target.value)}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Autocomplete
              multiple
              options={productOptions}
              value={formData.supportedProducts}
              onChange={(_, newValue) => handleChange('supportedProducts', newValue)}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip variant="outlined" label={option} {...getTagProps({ index })} />
                ))
              }
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Supported Products"
                  placeholder="Select products"
                />
              )}
            />
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Autocomplete
              multiple
              options={serviceOptions}
              value={formData.handlingServices}
              onChange={(_, newValue) => handleChange('handlingServices', newValue)}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => (
                  <Chip variant="outlined" label={option} {...getTagProps({ index })} />
                ))
              }
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Handling Services"
                  placeholder="Select services"
                />
              )}
            />
          </Grid>

          {/* Access Options */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Access Options
            </Typography>
            <Box display="flex" flexWrap="wrap" gap={2}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formData.hasRailAccess}
                    onChange={(e) => handleChange('hasRailAccess', e.target.checked)}
                  />
                }
                label="Rail Access"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formData.hasRoadAccess}
                    onChange={(e) => handleChange('hasRoadAccess', e.target.checked)}
                  />
                }
                label="Road Access"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formData.hasSeaAccess}
                    onChange={(e) => handleChange('hasSeaAccess', e.target.checked)}
                  />
                }
                label="Sea Access"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formData.hasPipelineAccess}
                    onChange={(e) => handleChange('hasPipelineAccess', e.target.checked)}
                  />
                }
                label="Pipeline Access"
              />
            </Box>
          </Grid>
        </Grid>
      </DialogContent>
      
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading}>
          {loading ? 'Saving...' : (location ? 'Update' : 'Create')}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default LocationModal;