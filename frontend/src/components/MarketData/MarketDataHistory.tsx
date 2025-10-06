import React, { useState } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Button,
  Tab,
  Tabs,
  Alert,
  LinearProgress,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  ViewList as ViewListIcon,
  History as HistoryIcon,
  DateRange as DateRangeIcon,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { usePriceHistory, useLatestPrices } from '@/hooks/useMarketData';

interface MarketDataHistoryProps {
  onTabChange: (tab: 'upload' | 'latest' | 'history') => void;
}

export const MarketDataHistory: React.FC<MarketDataHistoryProps> = ({ onTabChange }) => {
  const [selectedProduct, setSelectedProduct] = useState<string>('');
  const [startDate, setStartDate] = useState<Date | null>(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) // 30 days ago
  );
  const [endDate, setEndDate] = useState<Date | null>(new Date());

  // Get available products
  const { data: latestPrices } = useLatestPrices();
  const products = latestPrices?.spotPrices || [];
  const uniqueProducts = Array.from(
    new Map(products.map(p => [p.productCode, p])).values()
  );

  // Get historical data
  const { data: historyData, isLoading, error, refetch } = usePriceHistory(
    selectedProduct,
    startDate?.toISOString(),
    endDate?.toISOString(),
    !!selectedProduct
  );

  const handleProductChange = (productCode: string) => {
    setSelectedProduct(productCode);
  };

  const handleDateRangeChange = () => {
    if (selectedProduct) {
      refetch();
    }
  };

  // Prepare chart data
  const chartData = historyData?.map(price => ({
    date: new Date(price.priceDate).toLocaleDateString(),
    price: price.price,
    high: price.high,
    low: price.low,
    volume: price.volume,
    change: price.change,
  })) || [];

  const formatTooltipValue = (value: number, name: string) => {
    if (name === 'volume') {
      return [value?.toLocaleString() || 'N/A', 'Volume'];
    }
    return [`$${value?.toFixed(2) || 'N/A'}`, name];
  };

  const selectedProductInfo = products.find(p => p.productCode === selectedProduct);

  return (
    <Box>
      {/* Header with Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value="history" onChange={(_, value) => onTabChange(value)}>
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

      <Typography variant="h5" gutterBottom>
        Price History Analysis
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        View historical price trends and analyze market movements for specific products.
      </Typography>

      {/* Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Grid container spacing={3} alignItems="center">
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Select Product</InputLabel>
                <Select
                  value={selectedProduct}
                  label="Select Product"
                  onChange={(e) => handleProductChange(e.target.value)}
                >
                  <MenuItem value="">
                    <em>Choose a product...</em>
                  </MenuItem>
                  {uniqueProducts.map((product) => (
                    <MenuItem key={product.productCode} value={product.productCode}>
                      {product.productName} ({product.productCode})
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} md={3}>
              <LocalizationProvider dateAdapter={AdapterDateFns}>
                <DatePicker
                  label="Start Date"
                  value={startDate}
                  onChange={setStartDate}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </LocalizationProvider>
            </Grid>
            <Grid item xs={12} md={3}>
              <LocalizationProvider dateAdapter={AdapterDateFns}>
                <DatePicker
                  label="End Date"
                  value={endDate}
                  onChange={setEndDate}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </LocalizationProvider>
            </Grid>
            <Grid item xs={12} md={2}>
              <Button
                fullWidth
                variant="contained"
                onClick={handleDateRangeChange}
                disabled={!selectedProduct || isLoading}
                startIcon={<DateRangeIcon />}
              >
                Update
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Product Information */}
      {selectedProductInfo && (
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Current Price
                </Typography>
                <Typography variant="h5">
                  ${selectedProductInfo.price.toFixed(2)}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  per MT
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  24h Change
                </Typography>
                <Typography 
                  variant="h5"
                  color={selectedProductInfo.change && selectedProductInfo.change >= 0 ? 'success.main' : 'error.main'}
                >
                  {selectedProductInfo.change !== null 
                    ? `${selectedProductInfo.change >= 0 ? '+' : ''}${selectedProductInfo.change.toFixed(2)}`
                    : 'N/A'
                  }
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {selectedProductInfo.changePercent !== null 
                    ? `${selectedProductInfo.changePercent >= 0 ? '+' : ''}${selectedProductInfo.changePercent.toFixed(2)}%`
                    : 'N/A'
                  }
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Exchange
                </Typography>
                <Typography variant="h6">
                  ICE
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Spot
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Data Points
                </Typography>
                <Typography variant="h5">
                  {historyData?.length || 0}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  records found
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Loading indicator */}
      {isLoading && <LinearProgress sx={{ mb: 2 }} />}

      {/* Error handling */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          Failed to load price history: {error.message}
        </Alert>
      )}

      {/* No product selected */}
      {!selectedProduct && (
        <Card sx={{ textAlign: 'center', py: 8 }}>
          <CardContent>
            <HistoryIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" gutterBottom>
              Select a Product
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Choose a product from the dropdown above to view its price history and trends.
            </Typography>
          </CardContent>
        </Card>
      )}

      {/* No data available */}
      {selectedProduct && historyData && historyData.length === 0 && !isLoading && (
        <Card sx={{ textAlign: 'center', py: 8 }}>
          <CardContent>
            <HistoryIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" gutterBottom>
              No Data Available
            </Typography>
            <Typography variant="body2" color="text.secondary">
              No historical price data found for the selected product and date range.
            </Typography>
          </CardContent>
        </Card>
      )}

      {/* Price Chart */}
      {selectedProduct && historyData && historyData.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Price History - {selectedProductInfo?.productName}
            </Typography>
            <Box sx={{ height: 400 }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis 
                    dataKey="date" 
                    tick={{ fontSize: 12 }}
                    angle={-45}
                    textAnchor="end"
                    height={80}
                  />
                  <YAxis 
                    tick={{ fontSize: 12 }}
                    tickFormatter={(value) => `$${value.toFixed(2)}`}
                  />
                  <Tooltip
                    formatter={formatTooltipValue}
                    labelFormatter={(label) => `Date: ${label}`}
                    contentStyle={{
                      backgroundColor: '#1a1d29',
                      border: '1px solid #2a2d3a',
                      borderRadius: '8px',
                      color: '#ffffff',
                    }}
                  />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="price"
                    stroke="#2196f3"
                    strokeWidth={2}
                    dot={{ fill: '#2196f3', strokeWidth: 2, r: 3 }}
                    name="Price"
                  />
                  {chartData.some(d => d.high !== null) && (
                    <Line
                      type="monotone"
                      dataKey="high"
                      stroke="#4caf50"
                      strokeWidth={1}
                      strokeDasharray="5 5"
                      dot={false}
                      name="High"
                    />
                  )}
                  {chartData.some(d => d.low !== null) && (
                    <Line
                      type="monotone"
                      dataKey="low"
                      stroke="#f44336"
                      strokeWidth={1}
                      strokeDasharray="5 5"
                      dot={false}
                      name="Low"
                    />
                  )}
                </LineChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};