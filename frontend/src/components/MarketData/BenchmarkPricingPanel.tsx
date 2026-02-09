import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Card,
  CardContent,
  Grid,
  Tabs,
  Tab,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
  CircularProgress,
  Alert,
  Divider,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  Calculate as CalculateIcon,
} from '@mui/icons-material';
import { marketDataApi } from '@/services/marketDataApi';
import { XGROUP_PRODUCTS, type BenchmarkPriceResult } from '@/types/marketData';

interface BenchmarkPricingPanelProps {
  onPriceCalculated?: (price: number, method: string, details: any) => void;
}

export const BenchmarkPricingPanel: React.FC<BenchmarkPricingPanelProps> = ({
  onPriceCalculated,
}) => {
  const [pricingMethod, setPricingMethod] = useState<'dateRange' | 'contractMonth' | 'spotPremium'>('dateRange');
  const [selectedProduct, setSelectedProduct] = useState<string>(XGROUP_PRODUCTS[0].code);
  const [contractMonths, setContractMonths] = useState<string[]>([]);
  const [selectedContractMonth, setSelectedContractMonth] = useState<string>('');
  const [startDate, setStartDate] = useState<Date | null>(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  const [endDate, setEndDate] = useState<Date | null>(new Date());
  const [priceDate, setPriceDate] = useState<Date | null>(new Date());
  const [premium, setPremium] = useState<number>(0);
  const [isPercentage, setIsPercentage] = useState(false);
  const [priceType, setPriceType] = useState<'Spot' | 'Settlement'>('Spot');

  const [result, setResult] = useState<BenchmarkPriceResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load contract months when product changes
  useEffect(() => {
    const loadContractMonths = async () => {
      try {
        const months = await marketDataApi.getBenchmarkContractMonths(selectedProduct);
        setContractMonths(months);
        if (months.length > 0 && !selectedContractMonth) {
          setSelectedContractMonth(months[0]);
        }
      } catch (err) {
        setContractMonths([]);
      }
    };
    loadContractMonths();
  }, [selectedProduct]);

  const handleCalculate = async () => {
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      let response: BenchmarkPriceResult;

      switch (pricingMethod) {
        case 'dateRange':
          if (!startDate || !endDate) {
            throw new Error('Please select start and end dates');
          }
          response = await marketDataApi.getBenchmarkDateRangeAverage(
            selectedProduct,
            startDate.toISOString().split('T')[0],
            endDate.toISOString().split('T')[0],
            selectedContractMonth || undefined,
            priceType
          );
          break;

        case 'contractMonth':
          if (!selectedContractMonth) {
            throw new Error('Please select a contract month');
          }
          response = await marketDataApi.getBenchmarkContractMonthPrice(
            selectedProduct,
            selectedContractMonth,
            priceDate?.toISOString().split('T')[0]
          );
          break;

        case 'spotPremium':
          response = await marketDataApi.getBenchmarkSpotPlusPremium(
            selectedProduct,
            premium,
            isPercentage,
            priceDate?.toISOString().split('T')[0]
          );
          break;

        default:
          throw new Error('Invalid pricing method');
      }

      setResult(response);
      onPriceCalculated?.(response.calculatedPrice, response.method, response);
    } catch (err: any) {
      setError(err.response?.data?.error || err.message || 'Calculation failed');
    } finally {
      setLoading(false);
    }
  };

  const currentProduct = XGROUP_PRODUCTS.find(p => p.code === selectedProduct);

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            <CalculateIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
            Benchmark 定价计算器
          </Typography>

          {/* Pricing Method Tabs */}
          <Tabs
            value={pricingMethod}
            onChange={(_, v) => setPricingMethod(v)}
            sx={{ mb: 3 }}
          >
            <Tab value="dateRange" label="日期区间均价" />
            <Tab value="contractMonth" label="合约月份定价" />
            <Tab value="spotPremium" label="现货+溢价" />
          </Tabs>

          {/* Product Selector */}
          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>产品 / Product</InputLabel>
            <Select
              value={selectedProduct}
              label="产品 / Product"
              onChange={(e) => setSelectedProduct(e.target.value)}
            >
              {XGROUP_PRODUCTS.map(product => (
                <MenuItem key={product.code} value={product.code}>
                  {product.code} - {product.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <Divider sx={{ my: 2 }} />

          {/* Date Range Average Form */}
          {pricingMethod === 'dateRange' && (
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <DatePicker
                  label="开始日期 / Start Date"
                  value={startDate}
                  onChange={setStartDate}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <DatePicker
                  label="结束日期 / End Date"
                  value={endDate}
                  onChange={setEndDate}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>合约月份 (可选)</InputLabel>
                  <Select
                    value={selectedContractMonth}
                    label="合约月份 (可选)"
                    onChange={(e) => setSelectedContractMonth(e.target.value)}
                  >
                    <MenuItem value="">全部</MenuItem>
                    {contractMonths.map(month => (
                      <MenuItem key={month} value={month}>{month}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>价格类型</InputLabel>
                  <Select
                    value={priceType}
                    label="价格类型"
                    onChange={(e) => setPriceType(e.target.value as 'Spot' | 'Settlement')}
                  >
                    <MenuItem value="Spot">现货价格</MenuItem>
                    <MenuItem value="Settlement">期货结算价</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
            </Grid>
          )}

          {/* Contract Month Form */}
          {pricingMethod === 'contractMonth' && (
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>合约月份</InputLabel>
                  <Select
                    value={selectedContractMonth}
                    label="合约月份"
                    onChange={(e) => setSelectedContractMonth(e.target.value)}
                  >
                    {contractMonths.map(month => (
                      <MenuItem key={month} value={month}>{month}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6}>
                <DatePicker
                  label="价格日期 (可选)"
                  value={priceDate}
                  onChange={setPriceDate}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </Grid>
            </Grid>
          )}

          {/* Spot + Premium Form */}
          {pricingMethod === 'spotPremium' && (
            <Grid container spacing={2}>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="溢价金额 / Premium"
                  type="number"
                  value={premium}
                  onChange={(e) => setPremium(parseFloat(e.target.value) || 0)}
                  InputProps={{
                    endAdornment: isPercentage ? '%' : currentProduct?.unit === 'MT' ? '$/MT' : '$/BBL',
                  }}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={isPercentage}
                      onChange={(e) => setIsPercentage(e.target.checked)}
                    />
                  }
                  label="百分比溢价"
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <DatePicker
                  label="价格日期 (可选)"
                  value={priceDate}
                  onChange={setPriceDate}
                  slotProps={{ textField: { fullWidth: true } }}
                />
              </Grid>
            </Grid>
          )}

          {/* Calculate Button */}
          <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
            <Button
              variant="contained"
              color="primary"
              onClick={handleCalculate}
              disabled={loading}
              startIcon={loading ? <CircularProgress size={20} /> : <CalculateIcon />}
            >
              {loading ? '计算中...' : '计算价格'}
            </Button>
          </Box>

          {/* Error Display */}
          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}

          {/* Result Display */}
          {result && (
            <Paper variant="outlined" sx={{ mt: 3, p: 2, bgcolor: 'success.light' }}>
              <Typography variant="subtitle2" color="text.secondary">
                计算结果 / Calculated Price
              </Typography>
              <Typography variant="h4" color="success.dark" sx={{ mt: 1 }}>
                ${result.calculatedPrice.toFixed(4)}
                <Typography component="span" variant="body1" color="text.secondary" sx={{ ml: 1 }}>
                  / {currentProduct?.unit || 'unit'}
                </Typography>
              </Typography>

              <Grid container spacing={2} sx={{ mt: 2 }}>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">方法</Typography>
                  <Typography variant="body2">{result.method}</Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">数据点</Typography>
                  <Typography variant="body2">{result.dataPoints}</Typography>
                </Grid>
                {result.minPrice && (
                  <Grid item xs={6} sm={3}>
                    <Typography variant="caption" color="text.secondary">最低价</Typography>
                    <Typography variant="body2">${result.minPrice.toFixed(2)}</Typography>
                  </Grid>
                )}
                {result.maxPrice && (
                  <Grid item xs={6} sm={3}>
                    <Typography variant="caption" color="text.secondary">最高价</Typography>
                    <Typography variant="body2">${result.maxPrice.toFixed(2)}</Typography>
                  </Grid>
                )}
                {result.spotPrice && (
                  <Grid item xs={6} sm={3}>
                    <Typography variant="caption" color="text.secondary">现货价格</Typography>
                    <Typography variant="body2">${result.spotPrice.toFixed(2)}</Typography>
                  </Grid>
                )}
                {result.premium !== undefined && (
                  <Grid item xs={6} sm={3}>
                    <Typography variant="caption" color="text.secondary">溢价</Typography>
                    <Typography variant="body2">
                      {result.isPremiumPercentage ? `${result.premium}%` : `$${result.premium}`}
                    </Typography>
                  </Grid>
                )}
              </Grid>
            </Paper>
          )}
        </CardContent>
      </Card>
    </LocalizationProvider>
  );
};

export default BenchmarkPricingPanel;
