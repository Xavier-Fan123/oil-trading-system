import React, { useState, useMemo } from 'react';
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
  ToggleButton,
  ToggleButtonGroup,
  Chip,
  Autocomplete,
  TextField,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  ViewList as ViewListIcon,
  History as HistoryIcon,
  DateRange as DateRangeIcon,
  TrendingUp as TrendingUpIcon,
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
  ComposedChart,
  Bar,
} from 'recharts';
import { usePriceHistory, useLatestPrices } from '@/hooks/useMarketData';
import {
  BaseProduct,
  extractBaseProductCode,
  ProductCodeResolver,
  MarketType,
} from '@/types/marketData';

interface MarketDataHistoryProps {
  onTabChange: (tab: 'upload' | 'latest' | 'history') => void;
}

export const MarketDataHistory: React.FC<MarketDataHistoryProps> = ({ onTabChange }) => {
  // ========== 4-TIER HIERARCHICAL SELECTION STATE ==========
  // TIER 1: Base Product Selection (no contract months in dropdown)
  const [selectedBaseProduct, setSelectedBaseProduct] = useState<string>('');

  // TIER 2: Region Selection (conditional - visible only for spot prices)
  const [selectedRegion, setSelectedRegion] = useState<string>('');

  // TIER 3: Price Type Selection (Spot / Futures Settlement / Futures Close)
  const [priceType, setPriceType] = useState<'Spot' | 'FuturesSettlement' | 'FuturesClose'>('Spot');

  // TIER 4: Contract Month Selection (conditional - visible only for futures)
  const [selectedContractMonth, setSelectedContractMonth] = useState<string>('');

  // Date range selection
  const [startDate, setStartDate] = useState<Date | null>(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) // 30 days ago
  );
  const [endDate, setEndDate] = useState<Date | null>(new Date());

  // Visualization type
  const [visualizationType, setVisualizationType] = useState<'history' | 'forward-curve' | 'spread'>('history');

  // Get available products from latest prices
  const { data: latestPrices } = useLatestPrices();
  const spotPrices = latestPrices?.spotPrices || [];
  const futuresPrices = latestPrices?.futuresPrices || [];

  // ========== TIER 1: Extract Base Products from DATA ==========
  // FIX: Extract actual product categories from uploaded data
  const baseProducts = useMemo<BaseProduct[]>(() => {
    // Extract unique base product codes from API data
    const productCodesSet = new Set<string>();

    // Helper function to extract base product code from full product code
    const extractBaseCode = (fullCode: string): string | null => {
      // Remove contract month suffix (e.g., "2511", "2601")
      let baseCode = fullCode.replace(/\s+\d{4}$/, '').trim();

      // Skip derivative products for base product list (they're shown separately)
      if (baseCode.includes(' TS') ||    // Time Spreads
          baseCode.includes(' Brt') ||   // Brent spreads
          baseCode.includes(' EW') ||    // East-West spreads
          baseCode.includes(' EFS') ||   // Exchange for Swaps
          baseCode.includes('/')) {      // All crack spreads
        return null; // Skip derivatives in TIER 1
      }

      return baseCode;
    };

    // Extract from futures prices
    futuresPrices.forEach(futures => {
      const baseCode = extractBaseCode(futures.productCode);
      if (baseCode) productCodesSet.add(baseCode);
    });

    // Extract from spot prices
    spotPrices.forEach(spot => {
      const baseCode = extractBaseCode(spot.productCode);
      if (baseCode) productCodesSet.add(baseCode);
    });

    // Convert to base product array with display names
    // Use Map to deduplicate by display name (multiple codes can map to same display name)
    const productMap = new Map<string, BaseProduct>();

    Array.from(productCodesSet).forEach(code => {
      // Try to resolve display name from ProductCodeResolver
      const dbCode = ProductCodeResolver.resolveToDBCode(code);
      const displayName = dbCode ? ProductCodeResolver.getDisplayName(dbCode) : null;
      const availableRegions = dbCode ? ProductCodeResolver.getAvailableRegions(dbCode) : [];

      // Fallback: Create professional display name from code
      const finalDisplayName = displayName || code
        .replace('GO 10ppm', 'Gasoil 10ppm (Ultra Low Sulfur)')
        .replace('SG380', 'HSFO 380 CST (High Sulfur Fuel Oil)')
        .replace('SG180', 'HSFO 180 CST (High Sulfur Fuel Oil)')
        .replace('MF 0.5', 'VLSFO 0.5% S (IMO 2020)')
        .replace('MOPJ', 'Jet Fuel (Kerosene)')
        .replace('92R', 'Gasoline 92 RON')
        .replace('Sing Hi5', 'Gasoline 95 RON (Hi-5)')
        .replace('Brt Fut', 'Brent Crude Futures')
        .replace('Brt Swp', 'Brent Crude Swaps')
        .replace('Visco', 'Viscosity Spread');

      // Only add if this display name hasn't been added yet (deduplication)
      if (!productMap.has(finalDisplayName)) {
        productMap.set(finalDisplayName, {
          name: finalDisplayName,
          code: dbCode || code,
          availableRegions: availableRegions,
        });
      }
    });

    // Convert Map to array and sort by display name
    const products = Array.from(productMap.values());
    return products.sort((a, b) => a.name.localeCompare(b.name));
  }, [spotPrices, futuresPrices]);

  // ========== TIER 2: Extract Available Regions for Selected Base Product ==========
  const availableRegions = useMemo<string[]>(() => {
    if (!selectedBaseProduct || priceType !== 'Spot') return [];

    const baseProduct = baseProducts.find(bp => bp.name === selectedBaseProduct);
    return baseProduct?.availableRegions || [];
  }, [selectedBaseProduct, baseProducts, priceType]);

  // Auto-select region if only one available
  React.useEffect(() => {
    if (availableRegions.length === 1 && !selectedRegion) {
      setSelectedRegion(availableRegions[0]);
    } else if (availableRegions.length === 0) {
      setSelectedRegion('');
    } else if (selectedRegion && !availableRegions.includes(selectedRegion)) {
      setSelectedRegion('');
    }
  }, [availableRegions, selectedRegion]);

  // ========== TIER 4: Extract Available Contract Months for Selected Product ==========
  const availableContractMonths = useMemo<string[]>(() => {
    if (priceType === 'Spot' || !selectedBaseProduct) return [];

    // Find base product
    const baseProduct = baseProducts.find(bp => bp.name === selectedBaseProduct);
    if (!baseProduct) return [];

    // Extract contract months from futures prices
    // Match by base product code (compare extracted base codes)
    return Array.from(new Set(
      futuresPrices
        .filter(f => {
          const productCode = f.productCode;

          // Comprehensive derivative detection - MUST match the logic in actualProductCode
          const isDerivative =
            productCode.includes('_TS') ||      // Time Spreads
            productCode.includes('_BRT') ||     // Brent spreads (uppercase)
            productCode.includes('_Brt') ||     // Brent spreads (mixed case)
            productCode.includes('_EW') ||      // Europe-West spreads
            productCode.includes('_EFS') ||     // Exchange for Swaps
            productCode.includes('/') ||        // Crack spreads (contains slash)
            productCode.includes('_SPREAD');    // Generic spreads

          if (isDerivative) return false;

          // Extract base code and resolve to DB code for comparison
          const futureBaseCode = extractBaseProductCode(productCode);
          const futureDBCode = ProductCodeResolver.resolveToDBCode(futureBaseCode);

          return futureDBCode === baseProduct.code;
        })
        .map(f => f.contractMonth)
        .filter(m => m && m.trim())
    )).sort();
  }, [selectedBaseProduct, baseProducts, futuresPrices, priceType]);

  // ========== RESOLVE ACTUAL PRODUCT CODE FOR API CALLS (PROFESSIONAL VERSION) ==========
  const actualProductCode = useMemo<string>(() => {
    if (!selectedBaseProduct) return '';

    const baseProduct = baseProducts.find(bp => bp.name === selectedBaseProduct);
    if (!baseProduct) return '';

    // Use ProductCodeResolver to get the correct API code based on market type and region
    if (priceType === 'Spot') {
      // Physical Spot: Resolve with region awareness
      const apiCode = ProductCodeResolver.resolveToAPICode(
        baseProduct.code,
        MarketType.PhysicalSpot,
        selectedRegion || undefined
      );
      return apiCode || baseProduct.code;
    } else {
      // Exchange Futures: Resolve to futures product code
      // If contract month is selected, find the exact product code from API response
      if (selectedContractMonth) {
        // CRITICAL FIX: Filter out ALL derivative products before searching for base futures
        // Derivative products use specific suffixes/patterns to indicate their type
        const baseFuturesPrices = futuresPrices.filter(f => {
          const productCode = f.productCode;

          // Comprehensive derivative detection (covers ALL formats):
          // 1. Underscore-based derivatives: _TS, _BRT, _Brt, _EW, _EFS, _SPREAD
          // 2. Slash-based crack spreads: Contains "/" (e.g., "GO/_380")
          //
          // Examples:
          // - "SG380_TS" → Time Spread (derivative) ❌
          // - "380_EW" → Europe-West Spread (derivative) ❌
          // - "GO/_380" → Crack Spread (derivative) ❌
          // - "SG380" → Base Futures (keep) ✅
          // - "GO_10PPM" → Base Futures (keep, underscore is part of product name) ✅

          const isDerivative =
            productCode.includes('_TS') ||      // Time Spreads
            productCode.includes('_BRT') ||     // Brent spreads (uppercase)
            productCode.includes('_Brt') ||     // Brent spreads (mixed case)
            productCode.includes('_EW') ||      // Europe-West spreads
            productCode.includes('_EFS') ||     // Exchange for Swaps
            productCode.includes('/') ||        // Crack spreads (contains slash)
            productCode.includes('_SPREAD');    // Generic spreads

          return !isDerivative;
        });

        const matchingFutures = baseFuturesPrices.find(f => {
          const futureBaseCode = extractBaseProductCode(f.productCode);
          // Resolve API code to DB code before comparison
          const futureDBCode = ProductCodeResolver.resolveToDBCode(futureBaseCode);
          return futureDBCode === baseProduct.code && f.contractMonth === selectedContractMonth;
        });

        // CRITICAL FIX: Return the BASE product code (e.g., "SG380"), NOT the full code with contract month (e.g., "SG380 2605")
        // The contract month is already passed separately via selectedContractMonth parameter
        // Using the full code would cause API query issues (spaces in URLs, duplicate month info)
        if (matchingFutures) {
          const extractedCode = extractBaseProductCode(matchingFutures.productCode);
          console.log('[DEBUG] Contract month selected:', {
            originalProductCode: matchingFutures.productCode,
            extractedBaseCode: extractedCode,
            selectedContractMonth: selectedContractMonth,
            baseProductCode: baseProduct.code,
            filteredCount: baseFuturesPrices.length,
            totalFuturesCount: futuresPrices.length
          });
          return extractedCode;
        }

        console.log('[DEBUG] No matching futures found for:', {
          baseProductCode: baseProduct.code,
          selectedContractMonth: selectedContractMonth,
          filteredCount: baseFuturesPrices.length
        });

        return baseProduct.code;
      }

      // No contract month selected - use futures product code from PRODUCT_REGISTRY
      const futuresCode = ProductCodeResolver.resolveToAPICode(
        baseProduct.code,
        MarketType.ExchangeFutures
      );
      return futuresCode || baseProduct.code;
    }
  }, [selectedBaseProduct, baseProducts, priceType, selectedRegion, selectedContractMonth, futuresPrices]);

  // ========== DATA FETCHING ==========
  const { data: historyData, isLoading, error, refetch } = usePriceHistory(
    actualProductCode,
    startDate?.toISOString(),
    endDate?.toISOString(),
    priceType !== 'Spot' ? priceType : undefined,
    selectedContractMonth || undefined,
    selectedRegion || undefined,
    !!actualProductCode
  );

  // ========== EVENT HANDLERS ==========
  const handleBaseProductChange = (newBaseProduct: string | null) => {
    setSelectedBaseProduct(newBaseProduct || '');
    setSelectedRegion(''); // Reset region when base product changes
    setSelectedContractMonth(''); // Reset contract month when product changes
  };

  const handlePriceTypeChange = (newPriceType: string) => {
    setPriceType(newPriceType as 'Spot' | 'FuturesSettlement' | 'FuturesClose');
    setSelectedContractMonth(''); // Reset contract month when price type changes
    if (newPriceType === 'Spot') {
      setSelectedContractMonth(''); // Clear contract month for spot prices
    } else {
      setSelectedRegion(''); // Clear region for futures
    }
  };

  const handleDateRangeChange = () => {
    if (actualProductCode) {
      refetch();
    }
  };

  // Prepare chart data for history view
  const chartData = historyData?.map(price => ({
    date: new Date(price.priceDate).toLocaleDateString(),
    price: price.price,
    contractMonth: price.contractMonth,
  })) || [];

  // Prepare forward curve data (all contract months sorted)
  const forwardCurveData = historyData?.reduce((acc, price) => {
    // Group by contract month and get latest price for each
    const existing = acc.find(d => d.contractMonth === price.contractMonth);
    if (!existing || new Date(price.priceDate) > new Date(existing.date)) {
      return acc.filter(d => d.contractMonth !== price.contractMonth).concat({
        contractMonth: price.contractMonth || 'Spot',
        price: price.price,
        date: price.priceDate,
      });
    }
    return acc;
  }, [] as Array<{ contractMonth: string; price: number; date: Date | string }>)
    .sort((a, b) => (a.contractMonth || 'z').localeCompare(b.contractMonth || 'z')) || [];

  const formatTooltipValue = (value: number, name: string) => {
    return [`$${value?.toFixed(2) || 'N/A'}`, name];
  };

  const selectedProductInfo = spotPrices.find(p => p.productCode === actualProductCode);

  // Calculate 24h change from history data (if available)
  const calculatePriceChange = () => {
    if (!historyData || historyData.length < 2) {
      return { change: null, changePercent: null };
    }

    // Get oldest and newest prices
    const sortedByDate = [...historyData].sort((a, b) =>
      new Date(a.priceDate).getTime() - new Date(b.priceDate).getTime()
    );

    const oldestPrice = sortedByDate[0]?.price || 0;
    const newestPrice = sortedByDate[sortedByDate.length - 1]?.price || 0;

    const change = newestPrice - oldestPrice;
    const changePercent = oldestPrice !== 0 ? (change / oldestPrice) * 100 : 0;

    return { change, changePercent };
  };

  const { change, changePercent } = calculatePriceChange();

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

      {/* ========== TIER 1: BASE PRODUCT SELECTION (No Contract Months) ========== */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="subtitle2" sx={{ mb: 2, fontWeight: 600, color: 'primary.main' }}>
            TIER 1: Base Product Selection
          </Typography>
          <Grid container spacing={3} alignItems="center">
            <Grid item xs={12} md={4}>
              <Autocomplete
                options={baseProducts.map(bp => bp.name)}
                value={selectedBaseProduct || null}
                onChange={(_, newValue) => handleBaseProductChange(newValue)}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Select Base Product"
                    placeholder="Search products..."
                  />
                )}
                fullWidth
              />
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
                disabled={!actualProductCode || isLoading}
                startIcon={<DateRangeIcon />}
              >
                Update
              </Button>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* ========== TIER 2: REGION SELECTION (Conditional - Spot Only) ========== */}
      {selectedBaseProduct && priceType === 'Spot' && availableRegions.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="subtitle2" sx={{ mb: 2, fontWeight: 600, color: 'secondary.main' }}>
              TIER 2: Region Selection (Spot Prices)
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel>Select Region</InputLabel>
                  <Select
                    value={selectedRegion}
                    label="Select Region"
                    onChange={(e) => setSelectedRegion(e.target.value)}
                  >
                    <MenuItem value="">
                      <em>All Regions</em>
                    </MenuItem>
                    {availableRegions.map((region) => (
                      <MenuItem key={region} value={region}>
                        {region}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              {selectedRegion && (
                <Grid item xs={12} md={6} sx={{ display: 'flex', alignItems: 'center' }}>
                  <Chip
                    label={`Region: ${selectedRegion}`}
                    color="secondary"
                    variant="outlined"
                  />
                </Grid>
              )}
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* ========== TIER 3: PRICE TYPE & VISUALIZATION ========== */}
      {selectedBaseProduct && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="subtitle2" sx={{ mb: 2, fontWeight: 600, color: 'primary.main' }}>
              TIER 3: Price Type & Analysis Type
            </Typography>
            <Grid container spacing={3}>
              {/* Price Type Selection */}
              <Grid item xs={12} md={6}>
                <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
                  Price Type
                </Typography>
                <ToggleButtonGroup
                  value={priceType}
                  exclusive
                  onChange={(_, newPriceType) => {
                    if (newPriceType !== null) {
                      handlePriceTypeChange(newPriceType);
                    }
                  }}
                  fullWidth
                >
                  <ToggleButton value="Spot" aria-label="spot prices">
                    Spot
                  </ToggleButton>
                  <ToggleButton value="FuturesSettlement" aria-label="futures settlement">
                    Futures (Settlement)
                  </ToggleButton>
                  <ToggleButton value="FuturesClose" aria-label="futures close">
                    Futures (Close)
                  </ToggleButton>
                </ToggleButtonGroup>
              </Grid>

              {/* Visualization Type */}
              <Grid item xs={12}>
                <Typography variant="body2" sx={{ mb: 1, fontWeight: 500 }}>
                  Visualization Type
                </Typography>
                <ToggleButtonGroup
                  value={visualizationType}
                  exclusive
                  onChange={(_, newVisType) => {
                    if (newVisType !== null) {
                      setVisualizationType(newVisType);
                    }
                  }}
                  fullWidth
                >
                  <ToggleButton value="history" aria-label="history view">
                    <HistoryIcon sx={{ mr: 1 }} />
                    Price History
                  </ToggleButton>
                  <ToggleButton value="forward-curve" aria-label="forward curve">
                    <TrendingUpIcon sx={{ mr: 1 }} />
                    Forward Curve
                  </ToggleButton>
                  <ToggleButton value="spread" aria-label="calendar spread">
                    Spread Analysis
                  </ToggleButton>
                </ToggleButtonGroup>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* ========== TIER 4: CONTRACT MONTH SELECTION (Conditional - Futures Only) ========== */}
      {selectedBaseProduct && priceType !== 'Spot' && availableContractMonths.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="subtitle2" sx={{ mb: 2, fontWeight: 600, color: 'success.main' }}>
              TIER 4: Contract Month Selection (Futures)
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth>
                  <InputLabel>Select Contract Month</InputLabel>
                  <Select
                    value={selectedContractMonth}
                    label="Select Contract Month"
                    onChange={(e) => setSelectedContractMonth(e.target.value)}
                  >
                    <MenuItem value="">
                      <em>All Months</em>
                    </MenuItem>
                    {availableContractMonths.map((month) => (
                      <MenuItem key={month} value={month}>
                        {month}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              {selectedContractMonth && (
                <Grid item xs={12} md={6} sx={{ display: 'flex', alignItems: 'center' }}>
                  <Chip
                    label={`Contract Month: ${selectedContractMonth}`}
                    color="success"
                    variant="outlined"
                  />
                </Grid>
              )}
            </Grid>
          </CardContent>
        </Card>
      )}

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
                  ${selectedProductInfo.price?.toFixed(2) || 'N/A'}
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
                  Date Range Change
                </Typography>
                <Typography
                  variant="h5"
                  color={change !== null && change >= 0 ? 'success.main' : 'error.main'}
                >
                  {change !== null
                    ? `${change >= 0 ? '+' : ''}${change.toFixed(2)}`
                    : 'N/A'
                  }
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {changePercent !== null
                    ? `${changePercent >= 0 ? '+' : ''}${changePercent.toFixed(2)}%`
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
                  Price Type
                </Typography>
                <Typography variant="h6">
                  {priceType === 'Spot' ? 'Spot' : 'Futures'}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {priceType}
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
      {!selectedBaseProduct && (
        <Card sx={{ textAlign: 'center', py: 8 }}>
          <CardContent>
            <HistoryIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
            <Typography variant="h6" gutterBottom>
              Select a Base Product
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Choose a base product from the autocomplete dropdown above to view its price history and trends.
            </Typography>
          </CardContent>
        </Card>
      )}

      {/* No data available */}
      {selectedBaseProduct && historyData && historyData.length === 0 && !isLoading && (
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

      {/* ========== VISUALIZATION ========== */}
      {selectedBaseProduct && historyData && historyData.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom sx={{ mb: 3 }}>
              {visualizationType === 'history' && `Price History - ${selectedBaseProduct}${selectedContractMonth ? ` (${selectedContractMonth})` : ''}${selectedRegion ? ` - ${selectedRegion}` : ''}`}
              {visualizationType === 'forward-curve' && `Forward Curve - ${selectedBaseProduct} (${priceType})`}
              {visualizationType === 'spread' && `Calendar Spread - ${selectedBaseProduct}`}
            </Typography>

            {/* HISTORY VIEW: Time-Series Line Chart */}
            {visualizationType === 'history' && (
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
                  </LineChart>
                </ResponsiveContainer>
              </Box>
            )}

            {/* FORWARD CURVE VIEW: All Contract Months */}
            {visualizationType === 'forward-curve' && forwardCurveData.length > 0 && (
              <Box sx={{ height: 400 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <ComposedChart
                    data={forwardCurveData}
                    margin={{ top: 20, right: 30, left: 0, bottom: 5 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis
                      dataKey="contractMonth"
                      tick={{ fontSize: 12 }}
                      angle={-45}
                      textAnchor="end"
                      height={80}
                    />
                    <YAxis
                      tick={{ fontSize: 12 }}
                      tickFormatter={(value) => `$${value.toFixed(2)}`}
                      yAxisId="left"
                    />
                    <Tooltip
                      formatter={(value: number) => `$${value.toFixed(2)}`}
                      labelFormatter={(label) => `Month: ${label}`}
                      contentStyle={{
                        backgroundColor: '#1a1d29',
                        border: '1px solid #2a2d3a',
                        borderRadius: '8px',
                        color: '#ffffff',
                      }}
                    />
                    <Legend />
                    <Bar
                      dataKey="price"
                      fill="#2196f3"
                      yAxisId="left"
                      name="Price"
                      radius={[8, 8, 0, 0]}
                    />
                    <Line
                      type="monotone"
                      dataKey="price"
                      stroke="#ff7300"
                      strokeWidth={2}
                      yAxisId="left"
                      name="Trend"
                      dot={{ fill: '#ff7300', r: 4 }}
                    />
                  </ComposedChart>
                </ResponsiveContainer>
              </Box>
            )}

            {/* SPREAD VIEW: Calendar Spread Analysis */}
            {visualizationType === 'spread' && priceType !== 'Spot' && availableContractMonths.length >= 2 && (
              <Box>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  Calendar spreads show the price difference between two contract months. A positive spread indicates contango (futures premium), while negative indicates backwardation.
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
                        stroke="#9c27b0"
                        strokeWidth={2}
                        dot={{ fill: '#9c27b0', strokeWidth: 2, r: 3 }}
                        name={`${selectedContractMonth ? selectedContractMonth + ' Spread' : 'Calendar Spread'}`}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </Box>
              </Box>
            )}

            {/* Forward Curve or Spread not available */}
            {visualizationType === 'forward-curve' && forwardCurveData.length === 0 && (
              <Alert severity="info" sx={{ mt: 2 }}>
                No forward curve data available. Select a futures price type to view contract months.
              </Alert>
            )}

            {visualizationType === 'spread' && (priceType === 'Spot' || availableContractMonths.length < 2) && (
              <Alert severity="info" sx={{ mt: 2 }}>
                Calendar spread analysis requires futures prices with at least 2 contract months. Select a futures price type first.
              </Alert>
            )}
          </CardContent>
        </Card>
      )}
    </Box>
  );
};