import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Typography,
  Autocomplete,
  Stack,
} from '@mui/material';
import { format } from 'date-fns';
import type { ContractExecutionReportFilter as ContractExecutionReportFilterType } from '@/types/reports';
import { tradingPartnersApi, productsApi } from '@/services/contractsApi';
import { TradingPartner, Product } from '@/types/contracts';

interface ContractExecutionReportFilterProps {
  onFilterChange: (filters: ContractExecutionReportFilterType) => void;
  isLoading?: boolean;
}

export const ContractExecutionReportFilter: React.FC<ContractExecutionReportFilterProps> = ({
  onFilterChange,
  isLoading = false,
}) => {
  const [filters, setFilters] = useState<ContractExecutionReportFilterType>({
    pageNumber: 1,
    pageSize: 10,
    sortBy: 'ReportGeneratedDate',
    sortDescending: true,
  });

  const [tradingPartners, setTradingPartners] = useState<TradingPartner[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loadingData, setLoadingData] = useState(false);

  // Load trading partners and products on component mount
  useEffect(() => {
    loadDropdownData();
  }, []);

  const loadDropdownData = async () => {
    try {
      setLoadingData(true);
      const [partnersRes, productsRes] = await Promise.all([
        tradingPartnersApi.getAll(),
        productsApi.getAll(),
      ]);
      setTradingPartners(partnersRes || []);
      setProducts(productsRes || []);
    } catch (err) {
      console.error('Error loading dropdown data:', err);
    } finally {
      setLoadingData(false);
    }
  };

  const handleContractTypeChange = (event: any) => {
    const newFilters = {
      ...filters,
      contractType: event.target.value || undefined,
      pageNumber: 1,
    };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleExecutionStatusChange = (event: any) => {
    const newFilters = {
      ...filters,
      executionStatus: event.target.value || undefined,
      pageNumber: 1,
    };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleFromDateChange = (evt: React.ChangeEvent<HTMLInputElement>) => {
    const newFilters = {
      ...filters,
      fromDate: evt.target.value ? new Date(evt.target.value) : undefined,
      pageNumber: 1,
    };
    setFilters(newFilters);
  };

  const handleToDateChange = (evt: React.ChangeEvent<HTMLInputElement>) => {
    const newFilters = {
      ...filters,
      toDate: evt.target.value ? new Date(evt.target.value) : undefined,
      pageNumber: 1,
    };
    setFilters(newFilters);
  };

  const handleTradingPartnerChange = (value: TradingPartner | null) => {
    const newFilters = {
      ...filters,
      tradingPartnerId: value?.id,
      pageNumber: 1,
    };
    setFilters(newFilters);
  };

  const handleProductChange = (value: Product | null) => {
    const newFilters = {
      ...filters,
      productId: value?.id,
      pageNumber: 1,
    };
    setFilters(newFilters);
  };

  const handleSortByChange = (event: any) => {
    const newFilters = {
      ...filters,
      sortBy: event.target.value,
      pageNumber: 1,
    };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleSortDirectionChange = (event: any) => {
    const newFilters = {
      ...filters,
      sortDescending: event.target.value === 'desc',
      pageNumber: 1,
    };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleApplyFilters = () => {
    const newFilters = { ...filters, pageNumber: 1 };
    setFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleClearFilters = () => {
    const clearedFilters: ContractExecutionReportFilterType = {
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'ReportGeneratedDate',
      sortDescending: true,
    };
    setFilters(clearedFilters);
    onFilterChange(clearedFilters);
  };

  const hasActiveFilters = !!(
    filters.contractType ||
    filters.executionStatus ||
    filters.fromDate ||
    filters.toDate ||
    filters.tradingPartnerId ||
    filters.productId
  );

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent>
        <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
          Report Filters
        </Typography>

        <Grid container spacing={2}>
          {/* Contract Type */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Contract Type</InputLabel>
              <Select
                label="Contract Type"
                value={filters.contractType || ''}
                onChange={handleContractTypeChange}
              >
                <MenuItem value="">All Types</MenuItem>
                <MenuItem value="Purchase">Purchase</MenuItem>
                <MenuItem value="Sales">Sales</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Execution Status */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Execution Status</InputLabel>
              <Select
                label="Execution Status"
                value={filters.executionStatus || ''}
                onChange={handleExecutionStatusChange}
              >
                <MenuItem value="">All Status</MenuItem>
                <MenuItem value="OnTrack">On Track</MenuItem>
                <MenuItem value="Delayed">Delayed</MenuItem>
                <MenuItem value="Completed">Completed</MenuItem>
                <MenuItem value="Cancelled">Cancelled</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* From Date */}
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              size="small"
              label="From Date"
              type="date"
              InputLabelProps={{ shrink: true }}
              value={
                filters.fromDate
                  ? format(new Date(filters.fromDate), 'yyyy-MM-dd')
                  : ''
              }
              onChange={handleFromDateChange}
            />
          </Grid>

          {/* To Date */}
          <Grid item xs={12} sm={6} md={3}>
            <TextField
              fullWidth
              size="small"
              label="To Date"
              type="date"
              InputLabelProps={{ shrink: true }}
              value={
                filters.toDate
                  ? format(new Date(filters.toDate), 'yyyy-MM-dd')
                  : ''
              }
              onChange={handleToDateChange}
            />
          </Grid>

          {/* Trading Partner */}
          <Grid item xs={12} sm={6} md={3}>
            <Autocomplete
              size="small"
              options={tradingPartners}
              getOptionLabel={(option) => option.companyName}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                filters.tradingPartnerId
                  ? tradingPartners.find((p) => p.id === filters.tradingPartnerId) || null
                  : null
              }
              onChange={(_event, value) => handleTradingPartnerChange(value)}
              loading={loadingData}
              renderInput={(params) => (
                <TextField {...params} label="Trading Partner" />
              )}
            />
          </Grid>

          {/* Product */}
          <Grid item xs={12} sm={6} md={3}>
            <Autocomplete
              size="small"
              options={products}
              getOptionLabel={(option) => option.productName}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              value={
                filters.productId
                  ? products.find((p) => p.id === filters.productId) || null
                  : null
              }
              onChange={(_event, value) => handleProductChange(value)}
              loading={loadingData}
              renderInput={(params) => (
                <TextField {...params} label="Product" />
              )}
            />
          </Grid>

          {/* Sort By */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Sort By</InputLabel>
              <Select
                label="Sort By"
                value={filters.sortBy || 'ReportGeneratedDate'}
                onChange={handleSortByChange}
              >
                <MenuItem value="ReportGeneratedDate">Report Date</MenuItem>
                <MenuItem value="ContractNumber">Contract Number</MenuItem>
                <MenuItem value="ContractType">Contract Type</MenuItem>
                <MenuItem value="ExecutionStatus">Execution Status</MenuItem>
                <MenuItem value="ExecutionPercentage">Execution %</MenuItem>
                <MenuItem value="PaymentStatus">Payment Status</MenuItem>
                <MenuItem value="TradingPartnerName">Trading Partner</MenuItem>
                <MenuItem value="ProductName">Product Name</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Sort Direction */}
          <Grid item xs={12} sm={6} md={3}>
            <FormControl fullWidth size="small">
              <InputLabel>Sort Direction</InputLabel>
              <Select
                label="Sort Direction"
                value={filters.sortDescending ? 'desc' : 'asc'}
                onChange={handleSortDirectionChange}
              >
                <MenuItem value="desc">Descending</MenuItem>
                <MenuItem value="asc">Ascending</MenuItem>
              </Select>
            </FormControl>
          </Grid>
        </Grid>

        {/* Active Filters Display */}
        {hasActiveFilters && (
          <Box sx={{ mt: 2 }}>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
              {filters.contractType && (
                <Chip
                  label={`Type: ${filters.contractType}`}
                  onDelete={() =>
                    handleContractTypeChange({
                      target: { value: '' },
                    } as any)
                  }
                  size="small"
                  variant="outlined"
                />
              )}
              {filters.executionStatus && (
                <Chip
                  label={`Status: ${filters.executionStatus}`}
                  onDelete={() =>
                    handleExecutionStatusChange({
                      target: { value: '' },
                    } as any)
                  }
                  size="small"
                  variant="outlined"
                />
              )}
              {filters.fromDate && (
                <Chip
                  label={`From: ${format(new Date(filters.fromDate), 'MMM dd, yyyy')}`}
                  onDelete={() => handleFromDateChange({ target: { value: '' } } as any)}
                  size="small"
                  variant="outlined"
                />
              )}
              {filters.toDate && (
                <Chip
                  label={`To: ${format(new Date(filters.toDate), 'MMM dd, yyyy')}`}
                  onDelete={() => handleToDateChange({ target: { value: '' } } as any)}
                  size="small"
                  variant="outlined"
                />
              )}
              {filters.tradingPartnerId && (
                <Chip
                  label={`Partner: ${
                    tradingPartners.find((p) => p.id === filters.tradingPartnerId)
                      ?.companyName
                  }`}
                  onDelete={() => handleTradingPartnerChange(null)}
                  size="small"
                  variant="outlined"
                />
              )}
              {filters.productId && (
                <Chip
                  label={`Product: ${
                    products.find((p) => p.id === filters.productId)?.productName
                  }`}
                  onDelete={() => handleProductChange(null)}
                  size="small"
                  variant="outlined"
                />
              )}
            </Stack>
          </Box>
        )}

        <Divider sx={{ my: 2 }} />

        {/* Action Buttons */}
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          <Button
            variant="outlined"
            onClick={handleClearFilters}
            disabled={!hasActiveFilters}
          >
            Clear Filters
          </Button>
          <Button
            variant="contained"
            onClick={handleApplyFilters}
            disabled={isLoading}
          >
            Apply Filters
          </Button>
        </Stack>
      </CardContent>
    </Card>
  );
};
