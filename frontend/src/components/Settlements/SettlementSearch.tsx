import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Grid,
  Alert,
  CircularProgress,
  Divider,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  IconButton,
  Tooltip
} from '@mui/material';
import {
  Search as SearchIcon,
  Add as AddIcon,
  Clear as ClearIcon,
  Help as HelpIcon
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { 
  ContractSettlementListDto, 
  ContractSettlementStatus,
  ContractSettlementStatusLabels,
  SettlementSearchFilters 
} from '@/types/settlement';
import { searchSettlementsWithFallback, settlementApi } from '@/services/settlementApi';

interface SettlementSearchProps {
  onSearch: (searchTerm: string, results: ContractSettlementListDto[]) => void;
  onCreateNew: () => void;
}

export const SettlementSearch: React.FC<SettlementSearchProps> = ({
  onSearch,
  onCreateNew
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [advancedFilters, setAdvancedFilters] = useState<Partial<SettlementSearchFilters>>({
    startDate: undefined,
    endDate: undefined,
    status: undefined,
    contractId: undefined,
    documentNumber: undefined
  });
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSimpleSearch = async () => {
    if (!searchTerm.trim()) {
      setError('Please enter an external contract number to search');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await searchSettlementsWithFallback(searchTerm.trim());
      
      if (result.data.length === 0) {
        setError(`No settlements found for external contract number: ${searchTerm}`);
      } else {
        onSearch(searchTerm, result.data);
      }
    } catch (err) {
      console.error('Search error:', err);
      setError('Failed to search settlements. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleAdvancedSearch = async () => {
    setLoading(true);
    setError(null);

    try {
      const filters: SettlementSearchFilters = {
        pageNumber: 1,
        pageSize: 50,
        ...advancedFilters
      };

      // Add simple search term to external contract number filter if provided
      if (searchTerm.trim()) {
        filters.externalContractNumber = searchTerm.trim();
      }

      const result = await settlementApi.getSettlements(filters);
      
      if (result.data.length === 0) {
        setError('No settlements found matching the specified criteria');
      } else {
        onSearch(searchTerm || 'Advanced Search', result.data);
      }
    } catch (err) {
      console.error('Advanced search error:', err);
      // Fallback to mock data for advanced search
      try {
        const mockResult = await searchSettlementsWithFallback(searchTerm || '');
        if (mockResult.data.length > 0) {
          onSearch(searchTerm || 'Advanced Search', mockResult.data);
        } else {
          setError('No settlements found matching the specified criteria');
        }
      } catch (mockErr) {
        setError('Failed to search settlements. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleClearSearch = () => {
    setSearchTerm('');
    setAdvancedFilters({
      startDate: undefined,
      endDate: undefined,
      status: undefined,
      contractId: undefined,
      documentNumber: undefined
    });
    setError(null);
    setShowAdvanced(false);
  };

  const handleKeyPress = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter') {
      handleSimpleSearch();
    }
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Box sx={{ maxWidth: 800, mx: 'auto' }}>
        {/* Title */}
        <Typography variant="h4" component="h1" gutterBottom>
          Contract Settlement Search
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Search for contract settlements by external contract number or use advanced filters
        </Typography>

        {/* Main Search Card */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            {/* Simple Search */}
            <Box sx={{ mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                Quick Search
                <Tooltip title="Enter the external contract number (e.g., EXT-001) for exact search">
                  <IconButton size="small" sx={{ ml: 1 }}>
                    <HelpIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Typography>
              
              <Grid container spacing={2} alignItems="center">
                <Grid item xs={12} md={6}>
                  <TextField
                    fullWidth
                    label="External Contract Number"
                    placeholder="e.g., EXT-001, EXT-002"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    onKeyPress={handleKeyPress}
                    disabled={loading}
                    InputProps={{
                      endAdornment: searchTerm && (
                        <IconButton
                          size="small"
                          onClick={() => setSearchTerm('')}
                          disabled={loading}
                        >
                          <ClearIcon />
                        </IconButton>
                      )
                    }}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <Button
                      variant="contained"
                      startIcon={loading ? <CircularProgress size={20} /> : <SearchIcon />}
                      onClick={handleSimpleSearch}
                      disabled={loading || !searchTerm.trim()}
                      sx={{ minWidth: 120 }}
                    >
                      {loading ? 'Searching...' : 'Search'}
                    </Button>
                    <Button
                      variant="outlined"
                      onClick={() => setShowAdvanced(!showAdvanced)}
                      disabled={loading}
                    >
                      {showAdvanced ? 'Hide' : 'Advanced'}
                    </Button>
                    <Button
                      variant="text"
                      startIcon={<ClearIcon />}
                      onClick={handleClearSearch}
                      disabled={loading}
                    >
                      Clear
                    </Button>
                  </Box>
                </Grid>
              </Grid>
            </Box>

            {/* Advanced Search */}
            {showAdvanced && (
              <>
                <Divider sx={{ my: 3 }} />
                <Box>
                  <Typography variant="h6" gutterBottom>
                    Advanced Search Filters
                  </Typography>
                  
                  <Grid container spacing={3}>
                    {/* Date Range */}
                    <Grid item xs={12} md={6}>
                      <DatePicker
                        label="Start Date"
                        value={advancedFilters.startDate || null}
                        onChange={(date) => setAdvancedFilters(prev => ({ ...prev, startDate: date || undefined }))}
                        disabled={loading}
                        slotProps={{ textField: { fullWidth: true } }}
                      />
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <DatePicker
                        label="End Date"
                        value={advancedFilters.endDate || null}
                        onChange={(date) => setAdvancedFilters(prev => ({ ...prev, endDate: date || undefined }))}
                        disabled={loading}
                        minDate={advancedFilters.startDate || undefined}
                        slotProps={{ textField: { fullWidth: true } }}
                      />
                    </Grid>

                    {/* Status Filter */}
                    <Grid item xs={12} md={6}>
                      <FormControl fullWidth>
                        <InputLabel>Settlement Status</InputLabel>
                        <Select
                          value={advancedFilters.status || ''}
                          label="Settlement Status"
                          onChange={(e) => setAdvancedFilters(prev => ({ 
                            ...prev, 
                            status: e.target.value ? e.target.value as ContractSettlementStatus : undefined 
                          }))}
                          disabled={loading}
                        >
                          <MenuItem value="">All Statuses</MenuItem>
                          {Object.entries(ContractSettlementStatusLabels).map(([value, label]) => (
                            <MenuItem key={value} value={value}>
                              {label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Grid>

                    {/* Contract ID */}
                    <Grid item xs={12} md={6}>
                      <TextField
                        fullWidth
                        label="Contract ID"
                        placeholder="Enter contract ID"
                        value={advancedFilters.contractId || ''}
                        onChange={(e) => setAdvancedFilters(prev => ({ ...prev, contractId: e.target.value || undefined }))}
                        disabled={loading}
                      />
                    </Grid>

                    {/* Document Number */}
                    <Grid item xs={12} md={6}>
                      <TextField
                        fullWidth
                        label="Document Number"
                        placeholder="e.g., BL-2024-001"
                        value={advancedFilters.documentNumber || ''}
                        onChange={(e) => setAdvancedFilters(prev => ({ ...prev, documentNumber: e.target.value || undefined }))}
                        disabled={loading}
                      />
                    </Grid>

                    {/* Search Button for Advanced */}
                    <Grid item xs={12}>
                      <Button
                        variant="contained"
                        startIcon={loading ? <CircularProgress size={20} /> : <SearchIcon />}
                        onClick={handleAdvancedSearch}
                        disabled={loading}
                        size="large"
                      >
                        {loading ? 'Searching...' : 'Search with Filters'}
                      </Button>
                    </Grid>
                  </Grid>
                </Box>
              </>
            )}
          </CardContent>
        </Card>

        {/* Create New Settlement Card */}
        <Card>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <Box>
                <Typography variant="h6" gutterBottom>
                  Create New Settlement
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Create a new contract settlement from Bill of Lading or Certificate of Quantity data
                </Typography>
              </Box>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={onCreateNew}
                disabled={loading}
                size="large"
              >
                Create Settlement
              </Button>
            </Box>
          </CardContent>
        </Card>

        {/* Error Display */}
        {error && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            {error}
          </Alert>
        )}

        {/* Help Information */}
        <Card sx={{ mt: 3, bgcolor: 'grey.50' }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Search Tips
            </Typography>
            <Typography variant="body2" component="div">
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                <li>Use the exact external contract number for fastest results (e.g., EXT-001)</li>
                <li>External contract numbers are case-insensitive</li>
                <li>Advanced search allows filtering by date range, status, and document numbers</li>
                <li>Leave fields empty in advanced search to include all values</li>
                <li>Date filters use the document date from settlements</li>
              </ul>
            </Typography>
          </CardContent>
        </Card>
      </Box>
    </LocalizationProvider>
  );
};