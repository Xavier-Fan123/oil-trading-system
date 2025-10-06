import React from 'react';
import {
  Card,
  CardContent,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  FormControlLabel,
  Switch,
  Box,
  Typography,
  Button,
} from '@mui/material';
import {
  Clear as ClearIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import { PositionFilters, ProductType, PositionType } from '@/types/positions';

interface PositionFiltersProps {
  filters: PositionFilters;
  onFiltersChange: (filters: PositionFilters) => void;
  onClearFilters: () => void;
}

export const PositionFiltersComponent: React.FC<PositionFiltersProps> = ({
  filters,
  onFiltersChange,
  onClearFilters,
}) => {
  const handleFilterChange = (key: keyof PositionFilters, value: any) => {
    onFiltersChange({
      ...filters,
      [key]: value === '' ? undefined : value,
    });
  };

  const hasActiveFilters = Object.values(filters).some(value => 
    value !== undefined && value !== '' && value !== false
  );

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
          <Box display="flex" alignItems="center">
            <FilterIcon sx={{ mr: 1 }} />
            <Typography variant="h6">Filters</Typography>
          </Box>
          {hasActiveFilters && (
            <Button
              startIcon={<ClearIcon />}
              onClick={onClearFilters}
              variant="outlined"
              size="small"
            >
              Clear All
            </Button>
          )}
        </Box>

        <Grid container spacing={3}>
          {/* Product Type */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Product Type</InputLabel>
              <Select
                value={filters.productType ?? ''}
                label="Product Type"
                onChange={(e) => handleFilterChange('productType', e.target.value)}
              >
                <MenuItem value="">All Products</MenuItem>
                {Object.entries(ProductType)
                  .filter(([, value]) => typeof value === 'number')
                  .map(([key, value]) => (
                    <MenuItem key={key} value={value}>
                      {key}
                    </MenuItem>
                  ))
                }
              </Select>
            </FormControl>
          </Grid>

          {/* Position Type */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Position Type</InputLabel>
              <Select
                value={filters.positionType ?? ''}
                label="Position Type"
                onChange={(e) => handleFilterChange('positionType', e.target.value)}
              >
                <MenuItem value="">All Positions</MenuItem>
                {Object.entries(PositionType)
                  .filter(([, value]) => typeof value === 'number')
                  .map(([key, value]) => (
                    <MenuItem key={key} value={value}>
                      {key}
                    </MenuItem>
                  ))
                }
              </Select>
            </FormControl>
          </Grid>

          {/* Delivery Month */}
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              size="small"
              label="Delivery Month"
              placeholder="e.g., 2025-03"
              value={filters.deliveryMonth || ''}
              onChange={(e) => handleFilterChange('deliveryMonth', e.target.value)}
            />
          </Grid>

          {/* Min Quantity */}
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              size="small"
              type="number"
              label="Min Quantity"
              value={filters.minQuantity || ''}
              onChange={(e) => handleFilterChange('minQuantity', parseFloat(e.target.value) || undefined)}
            />
          </Grid>

          {/* Max Quantity */}
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              size="small"
              type="number"
              label="Max Quantity"
              value={filters.maxQuantity || ''}
              onChange={(e) => handleFilterChange('maxQuantity', parseFloat(e.target.value) || undefined)}
            />
          </Grid>

          {/* Show Flat Positions */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControlLabel
              control={
                <Switch
                  checked={filters.showFlatPositions ?? true}
                  onChange={(e) => handleFilterChange('showFlatPositions', e.target.checked)}
                />
              }
              label="Show Flat Positions"
            />
          </Grid>
        </Grid>

        {hasActiveFilters && (
          <Box mt={2} p={2} bgcolor="primary.50" borderRadius={1}>
            <Typography variant="body2" color="primary.main">
              Active filters applied. {Object.values(filters).filter(v => v !== undefined && v !== '' && v !== false).length} filter(s) active.
            </Typography>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};