import React, { useState } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Typography,
  Card,
  CardContent,
  Chip,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  IconButton,
  Tooltip,
  Tab,
  Tabs,
  Alert,
  LinearProgress,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Refresh as RefreshIcon,
  GetApp as ExportIcon,
  CloudUpload as UploadIcon,
  ViewList as ViewListIcon,
  History as HistoryIcon,
} from '@mui/icons-material';
import { useLatestPrices } from '@/hooks/useMarketData';

interface MarketDataTableProps {
  onTabChange: (tab: 'upload' | 'latest' | 'history') => void;
}

export const MarketDataTable: React.FC<MarketDataTableProps> = ({ onTabChange }) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [searchTerm, setSearchTerm] = useState('');
  const [priceTypeFilter, setPriceTypeFilter] = useState<string>('');
  const [exchangeFilter, setExchangeFilter] = useState<string>('');

  // Fetch data
  const { data: pricesData, isLoading, error, refetch } = useLatestPrices();

  // Combine spot and futures prices for display
  const spotPrices = pricesData?.spotPrices || [];
  const futuresPrices = pricesData?.futuresPrices || [];
  
  // Convert futures prices to match the display format
  const convertedFuturesPrices = futuresPrices.map(fp => ({
    productCode: `${fp.productCode}_${fp.contractMonth}`,
    productName: `${fp.productName} ${fp.contractMonth}`,
    price: fp.price,
    previousPrice: fp.previousSettlement,
    change: fp.change,
    changePercent: fp.change && fp.previousSettlement ? (fp.change / fp.previousSettlement) * 100 : null,
    priceDate: fp.priceDate
  }));
  
  // Combine all prices
  const allPrices = [...spotPrices, ...convertedFuturesPrices];

  // Filter data
  const filteredPrices = allPrices.filter(price => {
    const matchesSearch = price.productName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         price.productCode.toLowerCase().includes(searchTerm.toLowerCase());
    return matchesSearch;
  });

  // Pagination
  const paginatedPrices = filteredPrices.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage
  );

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const formatPrice = (price: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(price);
  };

  const formatChange = (change: number | null | undefined) => {
    if (change === null || change === undefined || typeof change !== 'number') return '-';
    const sign = change >= 0 ? '+' : '';
    return `${sign}${change.toFixed(2)}`;
  };

  const formatChangePercent = (changePercent: number | null | undefined) => {
    if (changePercent === null || changePercent === undefined || typeof changePercent !== 'number') return '-';
    const sign = changePercent >= 0 ? '+' : '';
    return `${sign}${changePercent.toFixed(2)}%`;
  };

  const getChangeColor = (change: number | null | undefined) => {
    if (change === null || change === undefined || typeof change !== 'number') return 'text.secondary';
    return change >= 0 ? 'success.main' : 'error.main';
  };

  const getTrendIcon = (change: number | null | undefined) => {
    if (change === null || change === undefined || typeof change !== 'number') return null;
    return change >= 0 ? 
      <TrendingUp sx={{ fontSize: 16, color: 'success.main' }} /> : 
      <TrendingDown sx={{ fontSize: 16, color: 'error.main' }} />;
  };


  // Get unique values for filters - ProductPriceDto doesn't have these fields
  const uniquePriceTypes: string[] = []; // Not available in ProductPriceDto
  const uniqueExchanges: string[] = []; // Not available in ProductPriceDto

  if (error) {
    return (
      <Alert severity="error" action={
        <IconButton color="inherit" size="small" onClick={() => refetch()} aria-label="Retry loading data">
          <RefreshIcon />
        </IconButton>
      }>
        Failed to load market data: {error.message}
      </Alert>
    );
  }

  return (
    <Box>
      {/* Header with Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value="latest" onChange={(_, value) => onTabChange(value)}>
          <Tab 
            label="Upload Data" 
            value="upload" 
            icon={<UploadIcon />}
            iconPosition="start"
          />
          <Tab 
            label="Latest Prices" 
            value="latest" 
            icon={<ViewListIcon />}
            iconPosition="start"
          />
          <Tab 
            label="Price History" 
            value="history" 
            icon={<HistoryIcon />}
            iconPosition="start"
          />
        </Tabs>
      </Box>

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5">
          Latest Market Prices
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Refresh data">
            <span>
              <IconButton onClick={() => refetch()} disabled={isLoading}>
                <RefreshIcon />
              </IconButton>
            </span>
          </Tooltip>
          <Tooltip title="Export data">
            <IconButton aria-label="Export data">
              <ExportIcon />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Summary Cards */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Total Products
              </Typography>
              <Typography variant="h4">
                {allPrices.length}
              </Typography>
              <Typography variant="caption" color="textSecondary">
                {spotPrices.length} Spot + {futuresPrices.length} Futures
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Last Updated
              </Typography>
              <Typography variant="h6">
                {pricesData?.lastUpdateDate 
                  ? new Date(pricesData.lastUpdateDate).toLocaleString()
                  : 'N/A'
                }
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Data Source
              </Typography>
              <Typography variant="h6">
                CSV Import
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Product Types
              </Typography>
              <Typography variant="h4">
                {new Set(allPrices.map(p => p.productCode.split('_')[0])).size}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} sm={6} md={3}>
              <TextField
                fullWidth
                size="small"
                label="Search products"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Product name or code..."
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <FormControl fullWidth size="small">
                <InputLabel>Price Type</InputLabel>
                <Select
                  value={priceTypeFilter}
                  label="Price Type"
                  onChange={(e) => setPriceTypeFilter(e.target.value)}
                >
                  <MenuItem value="">All Types</MenuItem>
                  {uniquePriceTypes.map(type => (
                    <MenuItem key={type} value={type}>{type}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <FormControl fullWidth size="small">
                <InputLabel>Exchange</InputLabel>
                <Select
                  value={exchangeFilter}
                  label="Exchange"
                  onChange={(e) => setExchangeFilter(e.target.value)}
                >
                  <MenuItem value="">All Exchanges</MenuItem>
                  {uniqueExchanges.map(exchange => (
                    <MenuItem key={exchange} value={exchange}>{exchange}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <Typography variant="body2" color="text.secondary">
                Showing {filteredPrices.length} of {allPrices.length} products
              </Typography>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Loading indicator */}
      {isLoading && <LinearProgress sx={{ mb: 2 }} />}

      {/* Data Table */}
      <TableContainer component={Paper}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Product</TableCell>
              <TableCell>Code</TableCell>
              <TableCell align="right">Current Price</TableCell>
              <TableCell align="right">Previous Price</TableCell>
              <TableCell align="right">Change</TableCell>
              <TableCell align="right">Change %</TableCell>
              <TableCell>Date</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {paginatedPrices.map((price, index) => (
              <TableRow key={`${price.productCode}-${index}`} hover>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium">
                    {price.productName}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip 
                    label={price.productCode} 
                    size="small" 
                    variant="outlined"
                  />
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" fontWeight="medium">
                    {formatPrice(price.price, 'USD')}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2">
                    {price.previousPrice ? formatPrice(price.previousPrice, 'USD') : '-'}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end' }}>
                    {getTrendIcon(price.change)}
                    <Typography 
                      variant="body2" 
                      color={getChangeColor(price.change)}
                      sx={{ ml: 0.5 }}
                    >
                      {formatChange(price.change)}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <Typography 
                    variant="body2" 
                    color={getChangeColor(price.changePercent)}
                  >
                    {formatChangePercent(price.changePercent)}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {new Date(price.priceDate).toLocaleDateString()}
                  </Typography>
                </TableCell>
              </TableRow>
            ))}
            {paginatedPrices.length === 0 && !isLoading && (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  <Typography variant="body2" color="text.secondary" sx={{ py: 4 }}>
                    No market data found
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <TablePagination
        rowsPerPageOptions={[10, 25, 50, 100]}
        component="div"
        count={filteredPrices.length}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />
    </Box>
  );
};