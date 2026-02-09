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
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  CircularProgress,
  Alert,
  Chip,
  Tooltip,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  ShowChart as ChartIcon,
  Assessment as AssessmentIcon,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ResponsiveContainer,
  AreaChart,
  Area,
} from 'recharts';
import { marketDataApi } from '@/services/marketDataApi';
import { XGROUP_PRODUCTS, type VaRMetrics, type BasisDataPoint } from '@/types/marketData';

interface ProductTabProps {
  productCode: string;
  productName: string;
}

export const MarketPriceDashboard: React.FC = () => {
  const [selectedProduct, setSelectedProduct] = useState(XGROUP_PRODUCTS[0].code);
  const [priceHistory, setPriceHistory] = useState<any[]>([]);
  const [basisData, setBasisData] = useState<BasisDataPoint[]>([]);
  const [varMetrics, setVarMetrics] = useState<VaRMetrics | null>(null);
  const [contractMonths, setContractMonths] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadData = async (productCode: string) => {
    setLoading(true);
    setError(null);

    try {
      // Load data in parallel
      const endDate = new Date();
      const startDate = new Date();
      startDate.setDate(startDate.getDate() - 90); // Last 90 days

      const [historyData, contractMonthsData, varData] = await Promise.all([
        marketDataApi.getPriceHistory(
          productCode,
          startDate.toISOString().split('T')[0],
          endDate.toISOString().split('T')[0]
        ).catch(() => []),
        marketDataApi.getBenchmarkContractMonths(productCode).catch(() => []),
        marketDataApi.getVaRMetrics(productCode, 252).catch(() => null),
      ]);

      setPriceHistory(historyData);
      setContractMonths(contractMonthsData);
      setVarMetrics(varData);

      // Load basis data if we have contract months
      if (contractMonthsData.length > 0) {
        try {
          const basisResult = await marketDataApi.getBasisAnalysis(
            productCode,
            contractMonthsData[0],
            startDate.toISOString().split('T')[0],
            endDate.toISOString().split('T')[0]
          );
          setBasisData(basisResult?.basisHistory || []);
        } catch {
          setBasisData([]);
        }
      }
    } catch (err: any) {
      setError(err.message || 'Error loading market data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData(selectedProduct);
  }, [selectedProduct]);

  const handleProductChange = (_: React.SyntheticEvent, newValue: string) => {
    setSelectedProduct(newValue);
  };

  const formatPrice = (price: number, unit: string) => {
    return `$${price.toFixed(2)}/${unit === 'MT' ? 'MT' : 'BBL'}`;
  };

  const formatPercent = (value: number) => {
    return `${(value * 100).toFixed(2)}%`;
  };

  const currentProduct = XGROUP_PRODUCTS.find(p => p.code === selectedProduct);

  // Prepare chart data
  const chartData = priceHistory
    .filter(p => p.priceType === 'Spot' || !p.contractMonth)
    .slice(-60)
    .map(p => ({
      date: new Date(p.priceDate).toLocaleDateString('zh-CN', { month: 'short', day: 'numeric' }),
      price: p.price,
    }));

  // Prepare forward curve data
  const forwardCurveData = contractMonths.slice(0, 12).map(month => {
    const monthPrices = priceHistory.filter(p =>
      p.contractMonth === month && p.priceType === 'FuturesSettlement'
    );
    const latestPrice = monthPrices.length > 0
      ? monthPrices[monthPrices.length - 1].price
      : null;
    return {
      month,
      price: latestPrice,
    };
  }).filter(d => d.price !== null);

  // Prepare basis chart data
  const basisChartData = basisData.slice(-60).map(b => ({
    date: new Date(b.date).toLocaleDateString('zh-CN', { month: 'short', day: 'numeric' }),
    basis: b.basis,
    basisPercent: b.basisPercent,
  }));

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
        市场价格看板 / Market Price Dashboard
      </Typography>

      {/* Product Selector */}
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={selectedProduct}
          onChange={handleProductChange}
          variant="scrollable"
          scrollButtons="auto"
        >
          {XGROUP_PRODUCTS.map(product => (
            <Tab
              key={product.code}
              value={product.code}
              label={product.code}
              sx={{ minWidth: 100 }}
            />
          ))}
        </Tabs>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      ) : (
        <Grid container spacing={3}>
          {/* Price Curve & Table Row */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <ChartIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                  现货价格趋势 / Spot Price Trend
                </Typography>
                <Box sx={{ height: 300 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={chartData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="date" fontSize={12} />
                      <YAxis fontSize={12} />
                      <RechartsTooltip />
                      <Line
                        type="monotone"
                        dataKey="price"
                        stroke="#1976d2"
                        strokeWidth={2}
                        dot={false}
                        name="现货价格"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <ChartIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                  远期曲线 / Forward Curve
                </Typography>
                <Box sx={{ height: 300 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={forwardCurveData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="month" fontSize={12} />
                      <YAxis fontSize={12} />
                      <RechartsTooltip />
                      <Line
                        type="monotone"
                        dataKey="price"
                        stroke="#2e7d32"
                        strokeWidth={2}
                        name="期货结算价"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Price Table */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  合约价格表 / Contract Prices
                </Typography>
                <TableContainer sx={{ maxHeight: 300 }}>
                  <Table size="small" stickyHeader>
                    <TableHead>
                      <TableRow>
                        <TableCell>合约月份</TableCell>
                        <TableCell align="right">结算价</TableCell>
                        <TableCell align="right">现货价</TableCell>
                        <TableCell align="right">基差</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {contractMonths.slice(0, 13).map(month => {
                        const futuresPrice = priceHistory
                          .filter(p => p.contractMonth === month)
                          .slice(-1)[0]?.price;
                        const spotPrice = priceHistory
                          .filter(p => !p.contractMonth && p.priceType === 'Spot')
                          .slice(-1)[0]?.price;
                        const basis = futuresPrice && spotPrice
                          ? futuresPrice - spotPrice
                          : null;

                        return (
                          <TableRow key={month}>
                            <TableCell>{month}</TableCell>
                            <TableCell align="right">
                              {futuresPrice ? `$${futuresPrice.toFixed(2)}` : '-'}
                            </TableCell>
                            <TableCell align="right">
                              {spotPrice ? `$${spotPrice.toFixed(2)}` : '-'}
                            </TableCell>
                            <TableCell align="right">
                              {basis !== null ? (
                                <Chip
                                  size="small"
                                  label={`${basis >= 0 ? '+' : ''}$${basis.toFixed(2)}`}
                                  color={basis >= 0 ? 'success' : 'error'}
                                  variant="outlined"
                                />
                              ) : '-'}
                            </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>

          {/* Basis Analysis */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  <AssessmentIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
                  基差分析 / Basis Analysis
                </Typography>
                {basisChartData.length > 0 ? (
                  <Box sx={{ height: 250 }}>
                    <ResponsiveContainer width="100%" height="100%">
                      <AreaChart data={basisChartData}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="date" fontSize={12} />
                        <YAxis fontSize={12} />
                        <RechartsTooltip />
                        <Area
                          type="monotone"
                          dataKey="basis"
                          stroke="#ed6c02"
                          fill="#ed6c02"
                          fillOpacity={0.3}
                          name="基差"
                        />
                      </AreaChart>
                    </ResponsiveContainer>
                  </Box>
                ) : (
                  <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
                    暂无基差数据
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* VaR Metrics */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  VaR 风险指标 / Value at Risk Metrics
                </Typography>
                {varMetrics ? (
                  <Grid container spacing={2}>
                    <Grid item xs={6} sm={3}>
                      <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="subtitle2" color="text.secondary">
                          1日 VaR (95%)
                        </Typography>
                        <Typography variant="h5" color="error">
                          ${varMetrics.var1Day95.toFixed(2)}
                        </Typography>
                      </Paper>
                    </Grid>
                    <Grid item xs={6} sm={3}>
                      <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="subtitle2" color="text.secondary">
                          1日 VaR (99%)
                        </Typography>
                        <Typography variant="h5" color="error">
                          ${varMetrics.var1Day99.toFixed(2)}
                        </Typography>
                      </Paper>
                    </Grid>
                    <Grid item xs={6} sm={3}>
                      <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="subtitle2" color="text.secondary">
                          10日 VaR (95%)
                        </Typography>
                        <Typography variant="h5" color="warning.main">
                          ${varMetrics.var10Day95.toFixed(2)}
                        </Typography>
                      </Paper>
                    </Grid>
                    <Grid item xs={6} sm={3}>
                      <Paper variant="outlined" sx={{ p: 2, textAlign: 'center' }}>
                        <Typography variant="subtitle2" color="text.secondary">
                          年化波动率
                        </Typography>
                        <Typography variant="h5" color="primary">
                          {formatPercent(varMetrics.annualizedVolatility)}
                        </Typography>
                      </Paper>
                    </Grid>
                  </Grid>
                ) : (
                  <Typography color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                    暂无VaR数据 (需要至少30天价格历史)
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}
    </Box>
  );
};

export default MarketPriceDashboard;
