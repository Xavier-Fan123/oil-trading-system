import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Alert,
  Chip,
  LinearProgress,
  Tabs,
  Tab,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
} from '@mui/icons-material';
import {
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  LineChart,
  Line,
} from 'recharts';
import { format } from 'date-fns';
import {
  useRiskCalculation,
  usePortfolioSummary,
  useBacktest,
  useRecalculateRisk
} from '@/hooks/useRisk';
import { RiskMetrics, StressTestResult, ProductRisk } from '@/services/riskApi';

const COLORS = ['#8884d8', '#82ca9d', '#ffc658', '#ff7300', '#0088fe', '#00c49f'];

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index} style={{ marginTop: 16 }}>
    {value === index && children}
  </div>
);

const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
};

const formatPercentage = (value: number, decimals: number = 2): string => {
  return `${(value * 100).toFixed(decimals)}%`;
};

const RiskMetricsCard: React.FC<{ title: string; metrics?: RiskMetrics }> = ({ title, metrics }) => (
  <Card>
    <CardContent>
      <Typography variant="h6" gutterBottom>
        {title}
      </Typography>
      {!metrics ? (
        <Alert severity="info">No risk metrics data available</Alert>
      ) : (
        <Grid container spacing={2}>
          <Grid item xs={6}>
            <Typography variant="body2" color="textSecondary">
              Portfolio Value
            </Typography>
            <Typography variant="h6">
              {formatCurrency(metrics.portfolioValue || 0)}
            </Typography>
          </Grid>
          <Grid item xs={6}>
            <Typography variant="body2" color="textSecondary">
              VaR (95%)
            </Typography>
            <Typography variant="h6" color="error">
              {formatCurrency(metrics.var95 || 0)}
            </Typography>
          </Grid>
          <Grid item xs={6}>
            <Typography variant="body2" color="textSecondary">
              VaR (99%)
            </Typography>
            <Typography variant="h6" color="error">
              {formatCurrency(metrics.var99 || 0)}
            </Typography>
          </Grid>
          <Grid item xs={6}>
            <Typography variant="body2" color="textSecondary">
              Volatility
            </Typography>
            <Typography variant="h6">
              {formatPercentage(metrics.portfolioVolatility || 0, 1)}
            </Typography>
          </Grid>
          <Grid item xs={6}>
            <Typography variant="body2" color="textSecondary">
              Max Drawdown
            </Typography>
            <Typography variant="h6" color="warning.main">
              {formatPercentage(metrics.maxDrawdown || 0, 1)}
            </Typography>
          </Grid>
          <Grid item xs={6}>
            <Typography variant="body2" color="textSecondary">
              Positions
            </Typography>
            <Typography variant="h6">
              {metrics.numberOfPositions || 0}
            </Typography>
          </Grid>
        </Grid>
      )}
    </CardContent>
  </Card>
);

const StressTestChart: React.FC<{ stressTests?: StressTestResult[] }> = ({ stressTests }) => (
  <Card>
    <CardContent>
      <Typography variant="h6" gutterBottom>
        Stress Test Results
      </Typography>
      {!stressTests || stressTests.length === 0 ? (
        <Alert severity="info">No stress test data available</Alert>
      ) : (
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={stressTests} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="scenarioName" 
              angle={-45}
              textAnchor="end"
              height={100}
            />
            <YAxis tickFormatter={(value) => formatCurrency(value)} />
            <RechartsTooltip formatter={(value) => formatCurrency(Number(value))} />
            <Bar 
              dataKey="portfolioChange" 
              fill="#8884d8"
            />
          </BarChart>
        </ResponsiveContainer>
      )}
    </CardContent>
  </Card>
);

const ProductRiskChart: React.FC<{ productRisks?: ProductRisk[] }> = ({ productRisks }) => (
  <Card>
    <CardContent>
      <Typography variant="h6" gutterBottom>
        Risk by Product
      </Typography>
      {!productRisks || productRisks.length === 0 ? (
        <Alert severity="info">No product risk data available</Alert>
      ) : (
        <ResponsiveContainer width="100%" height={300}>
          <PieChart>
            <Pie
              data={productRisks}
              dataKey="var95"
              nameKey="productType"
              cx="50%"
              cy="50%"
              outerRadius={80}
              label={({ productType, percent }) => `${productType} ${(percent * 100).toFixed(0)}%`}
            >
              {productRisks.map((_, index) => (
                <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
              ))}
            </Pie>
            <RechartsTooltip formatter={(value) => formatCurrency(Number(value))} />
          </PieChart>
        </ResponsiveContainer>
      )}
    </CardContent>
  </Card>
);

const BacktestChart: React.FC<{ backtestData?: any[] }> = ({ backtestData }) => (
  <Card>
    <CardContent>
      <Typography variant="h6" gutterBottom>
        VaR Backtesting
      </Typography>
      {!backtestData || backtestData.length === 0 ? (
        <Alert severity="info">No backtesting data available</Alert>
      ) : (
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={backtestData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="date"
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis tickFormatter={(value) => formatCurrency(value)} />
            <RechartsTooltip 
              labelFormatter={(value) => format(new Date(value), 'MMM dd, yyyy')}
              formatter={(value, name) => [formatCurrency(Number(value)), name]}
            />
            <Line 
              type="monotone" 
              dataKey="predictedVaR" 
              stroke="#8884d8" 
              name="Predicted VaR"
              strokeDasharray="5 5"
            />
            <Line 
              type="monotone" 
              dataKey="actualPnL" 
              stroke="#82ca9d" 
              name="Actual P&L"
            />
          </LineChart>
        </ResponsiveContainer>
      )}
    </CardContent>
  </Card>
);

export const RiskDashboard: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  
  const { data: riskData, isLoading: riskLoading, error: riskError } = useRiskCalculation();
  const { data: portfolioData, isLoading: portfolioLoading } = usePortfolioSummary();
  const { data: backtestData, isLoading: backtestLoading } = useBacktest(30);
  const recalculateRisk = useRecalculateRisk();

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleRefresh = () => {
    recalculateRisk.mutate();
  };

  if (riskLoading || portfolioLoading) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Risk Management Dashboard
        </Typography>
        <LinearProgress />
        <Box mt={2}>
          <Typography>Loading risk calculations...</Typography>
        </Box>
      </Box>
    );
  }

  if (riskError) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          Risk Management Dashboard
        </Typography>
        <Alert severity="error">
          Error loading risk data: {riskError.message}
        </Alert>
      </Box>
    );
  }

  const alerts = portfolioData?.alerts || [];
  const breachedLimits = riskData?.breachedLimits || [];

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Risk Management Dashboard
        </Typography>
        <Box>
          <Tooltip title="Refresh Risk Calculations">
            <span>
              <IconButton onClick={handleRefresh} disabled={recalculateRisk.isPending}>
                <RefreshIcon />
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      </Box>

      {/* Alerts */}
      {((alerts && alerts.length > 0) || (breachedLimits && breachedLimits.length > 0)) && (
        <Box mb={3}>
          {breachedLimits?.map((limit, index) => (
            <Alert severity="error" key={index} sx={{ mb: 1 }}>
              <Box display="flex" alignItems="center">
                <ErrorIcon sx={{ mr: 1 }} />
                Risk Limit Breach: {limit}
              </Box>
            </Alert>
          ))}
          {alerts?.map((alert, index) => (
            <Alert severity={alert.level} key={index} sx={{ mb: 1 }}>
              <Box display="flex" alignItems="center">
                {alert.level === 'warning' && <WarningIcon sx={{ mr: 1 }} />}
                {alert.level === 'info' && <InfoIcon sx={{ mr: 1 }} />}
                {alert.level === 'error' && <ErrorIcon sx={{ mr: 1 }} />}
                {alert.message}
              </Box>
            </Alert>
          ))}
        </Box>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="Overview" />
          <Tab label="Stress Tests" />
          <Tab label="Product Risk" />
          <Tab label="Backtesting" />
          <Tab label="Limits" />
        </Tabs>
      </Paper>

      {/* Tab Panels */}
      <TabPanel value={tabValue} index={0}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <RiskMetricsCard
              title="Current Risk Metrics"
              metrics={riskData?.riskMetrics}
            />
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Portfolio Overview
                </Typography>
                <Box mb={2}>
                  <Typography variant="body2" color="textSecondary">
                    Total Portfolio Value
                  </Typography>
                  <Typography variant="h4" color="primary">
                    {formatCurrency(portfolioData?.totalValue || 0)}
                  </Typography>
                </Box>
                <Box mb={2}>
                  <Typography variant="body2" color="textSecondary">
                    Active Positions
                  </Typography>
                  <Typography variant="h5">
                    {portfolioData?.totalPositions || 0}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="body2" color="textSecondary">
                    Last Updated
                  </Typography>
                  <Typography variant="body2">
                    {riskData?.timestamp ? format(new Date(riskData.timestamp), 'PPpp') : 'N/A'}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12}>
            <StressTestChart stressTests={riskData?.stressTests} />
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <StressTestChart stressTests={riskData?.stressTests} />
          </Grid>
          <Grid item xs={12}>
            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Scenario</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell align="right">Portfolio Change</TableCell>
                    <TableCell align="right">% Change</TableCell>
                    <TableCell align="right">New Portfolio Value</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {riskData?.stressTests?.map((test, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {test.scenarioName}
                        </Typography>
                      </TableCell>
                      <TableCell>{test.description}</TableCell>
                      <TableCell align="right">
                        <Box display="flex" alignItems="center" justifyContent="flex-end">
                          {test.portfolioChange < 0 ? (
                            <TrendingDownIcon color="error" sx={{ mr: 1 }} />
                          ) : (
                            <TrendingUpIcon color="success" sx={{ mr: 1 }} />
                          )}
                          <Typography 
                            color={test.portfolioChange < 0 ? 'error' : 'success.main'}
                            fontWeight="medium"
                          >
                            {formatCurrency(test.portfolioChange)}
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell align="right">
                        <Chip
                          label={formatPercentage(test.percentageChange / 100, 1)}
                          color={test.percentageChange < 0 ? 'error' : 'success'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell align="right">
                        {formatCurrency(test.newPortfolioValue)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <ProductRiskChart productRisks={riskData?.productRisks} />
          </Grid>
          <Grid item xs={12} md={6}>
            <TableContainer component={Paper}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Product</TableCell>
                    <TableCell align="right">Exposure</TableCell>
                    <TableCell align="right">VaR 95%</TableCell>
                    <TableCell align="right">Volatility</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {riskData?.productRisks?.map((product, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {product.productType}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        {formatCurrency(product.exposure)}
                      </TableCell>
                      <TableCell align="right">
                        <Typography color="error">
                          {formatCurrency(product.var95)}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        {formatPercentage(product.volatility, 1)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={3}>
        <Grid container spacing={3}>
          <Grid item xs={12}>
            {backtestLoading ? (
              <LinearProgress />
            ) : backtestData ? (
              <BacktestChart backtestData={backtestData} />
            ) : (
              <Alert severity="info">No backtesting data available</Alert>
            )}
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={tabValue} index={4}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Risk Limits
                </Typography>
                {riskData?.riskLimits && (
                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        VaR 95% Limit
                      </Typography>
                      <Typography variant="h6">
                        {formatCurrency(riskData.riskLimits.var95Limit)}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        VaR 99% Limit
                      </Typography>
                      <Typography variant="h6">
                        {formatCurrency(riskData.riskLimits.var99Limit)}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        Concentration Limit
                      </Typography>
                      <Typography variant="h6">
                        {formatPercentage(riskData.riskLimits.concentrationLimit, 0)}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" color="textSecondary">
                        Max Drawdown Limit
                      </Typography>
                      <Typography variant="h6">
                        {formatPercentage(riskData.riskLimits.maxDrawdownLimit, 0)}
                      </Typography>
                    </Grid>
                  </Grid>
                )}
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Current Utilization
                </Typography>
                {riskData?.riskMetrics && riskData?.riskLimits && (
                  <Box>
                    <Box mb={2}>
                      <Typography variant="body2" color="textSecondary">
                        VaR 95% Usage
                      </Typography>
                      <LinearProgress
                        variant="determinate"
                        value={(riskData.riskMetrics.var95 / riskData.riskLimits.var95Limit) * 100}
                        color={
                          (riskData.riskMetrics.var95 / riskData.riskLimits.var95Limit) > 0.8
                            ? 'error'
                            : (riskData.riskMetrics.var95 / riskData.riskLimits.var95Limit) > 0.6
                            ? 'warning'
                            : 'primary'
                        }
                        sx={{ height: 8, borderRadius: 4 }}
                      />
                      <Typography variant="body2">
                        {formatPercentage(riskData.riskMetrics.var95 / riskData.riskLimits.var95Limit, 1)} utilized
                      </Typography>
                    </Box>
                    <Box mb={2}>
                      <Typography variant="body2" color="textSecondary">
                        VaR 99% Usage
                      </Typography>
                      <LinearProgress
                        variant="determinate"
                        value={(riskData.riskMetrics.var99 / riskData.riskLimits.var99Limit) * 100}
                        color={
                          (riskData.riskMetrics.var99 / riskData.riskLimits.var99Limit) > 0.8
                            ? 'error'
                            : (riskData.riskMetrics.var99 / riskData.riskLimits.var99Limit) > 0.6
                            ? 'warning'
                            : 'primary'
                        }
                        sx={{ height: 8, borderRadius: 4 }}
                      />
                      <Typography variant="body2">
                        {formatPercentage(riskData.riskMetrics.var99 / riskData.riskLimits.var99Limit, 1)} utilized
                      </Typography>
                    </Box>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>
    </Box>
  );
};