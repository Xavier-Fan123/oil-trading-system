import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Grid,
  Card,
  CardContent,
  Typography,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Alert,
  CircularProgress,
  Divider,
  Stack,
  Tooltip,
  IconButton,
  Tabs,
  Tab,
  FormControl,
  InputLabel,
  Select,
  MenuItem
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Assessment as AssessmentIcon,
  AccountBalance as BalanceIcon,
  AttachMoney as MoneyIcon,
  CreditCard as CreditIcon,
  BusinessCenter as BusinessIcon,
  Refresh as RefreshIcon
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
} from 'recharts';
import { 
  TradingPartnerAnalysis, 
  FinancialHealthStatus,
  CreditRisk,
  FinancialTrend,
  ChartDataPoint,
  DataQuality 
} from '../../types/financialReport';
import { tradingPartnerService } from '../../services/tradingPartnerService';
import { formatDisplayDate } from '../../utils/dateUtils';

interface TradingPartnerAnalysisProps {
  tradingPartnerId: string;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`analysis-tabpanel-${index}`}
      aria-labelledby={`analysis-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const TradingPartnerAnalysisComponent: React.FC<TradingPartnerAnalysisProps> = ({
  tradingPartnerId
}) => {
  const [analysis, setAnalysis] = useState<TradingPartnerAnalysis | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tabValue, setTabValue] = useState(0);
  const [selectedYear, setSelectedYear] = useState<number>(new Date().getFullYear());

  useEffect(() => {
    loadAnalysis();
  }, [tradingPartnerId]);

  const loadAnalysis = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await tradingPartnerService.getAnalysis(tradingPartnerId);
      setAnalysis(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trading partner analysis');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number | null | undefined) => {
    if (amount === null || amount === undefined) return 'N/A';
    return new Intl.NumberFormat('en-US', { 
      style: 'currency', 
      currency: 'USD' 
    }).format(amount);
  };

  const formatPercent = (value: number | null | undefined) => {
    if (value === null || value === undefined) return 'N/A';
    return `${(value * 100).toFixed(1)}%`;
  };

  const formatNumber = (value: number | null | undefined) => {
    if (value === null || value === undefined) return 'N/A';
    return new Intl.NumberFormat('en-US').format(value);
  };

  const getHealthStatusColor = (status: FinancialHealthStatus): 'success' | 'warning' | 'error' | 'info' => {
    switch (status) {
      case FinancialHealthStatus.Excellent: return 'success';
      case FinancialHealthStatus.Good: return 'success';
      case FinancialHealthStatus.Fair: return 'warning';
      case FinancialHealthStatus.Poor: return 'error';
      case FinancialHealthStatus.Critical: return 'error';
      default: return 'info';
    }
  };

  const getCreditRiskColor = (risk: CreditRisk): 'success' | 'warning' | 'error' | 'info' => {
    switch (risk) {
      case CreditRisk.VeryLow: return 'success';
      case CreditRisk.Low: return 'success';
      case CreditRisk.Medium: return 'warning';
      case CreditRisk.High: return 'error';
      case CreditRisk.VeryHigh: return 'error';
      default: return 'info';
    }
  };

  const getDataQualityColor = (quality: DataQuality): 'success' | 'warning' | 'error' | 'info' => {
    switch (quality) {
      case DataQuality.Excellent: return 'success';
      case DataQuality.Good: return 'success';
      case DataQuality.Fair: return 'warning';
      case DataQuality.Poor: return 'error';
      case DataQuality.Insufficient: return 'error';
      default: return 'info';
    }
  };

  const getTrendIcon = (isPositive: boolean) => {
    return isPositive ? (
      <TrendingUpIcon color="success" fontSize="small" />
    ) : (
      <TrendingDownIcon color="error" fontSize="small" />
    );
  };

  const prepareChartData = (trends: FinancialTrend[]): { revenue: ChartDataPoint[], netIncome: ChartDataPoint[], totalAssets: ChartDataPoint[] } => {
    const chartData = {
      revenue: [] as ChartDataPoint[],
      netIncome: [] as ChartDataPoint[],
      totalAssets: [] as ChartDataPoint[]
    };

    trends.forEach(trend => {
      const dataPoints = trend.periods.map(period => ({
        year: period.year,
        value: period.value,
        label: `${trend.metric}: ${formatCurrency(period.value)}`
      }));

      switch (trend.metric.toLowerCase()) {
        case 'revenue':
          chartData.revenue = dataPoints;
          break;
        case 'netincome':
          chartData.netIncome = dataPoints;
          break;
        case 'totalassets':
          chartData.totalAssets = dataPoints;
          break;
      }
    });

    return chartData;
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight={400}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert 
        severity="error" 
        action={
          <IconButton color="inherit" size="small" onClick={loadAnalysis}>
            <RefreshIcon />
          </IconButton>
        }
      >
        {error}
      </Alert>
    );
  }

  if (!analysis) {
    return (
      <Alert severity="info">
        No analysis data available for this trading partner.
      </Alert>
    );
  }

  const chartData = prepareChartData(analysis.trends);
  const availableYears = [...new Set(analysis.trends.flatMap(t => t.periods.map(p => p.year)))].sort((a, b) => b - a);

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Box>
          <Typography variant="h4" fontWeight="bold" gutterBottom>
            Trading Partner Analysis
          </Typography>
          <Typography variant="subtitle1" color="text.secondary">
            {analysis.tradingPartnerName} ({analysis.companyCode})
          </Typography>
        </Box>
        <Box display="flex" alignItems="center" gap={2}>
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Year</InputLabel>
            <Select
              value={selectedYear}
              label="Year"
              onChange={(e) => setSelectedYear(e.target.value as number)}
            >
              {availableYears.map(year => (
                <MenuItem key={year} value={year}>{year}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Tooltip title="Refresh Analysis">
            <IconButton onClick={loadAnalysis}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Key Metrics Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <CreditIcon color="primary" sx={{ fontSize: 40, mb: 1 }} />
              <Typography color="text.secondary" gutterBottom>
                Credit Limit
              </Typography>
              <Typography variant="h5" component="div">
                {formatCurrency(analysis.creditLimit)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Utilization: {formatPercent(analysis.creditUtilization)}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <MoneyIcon color="secondary" sx={{ fontSize: 40, mb: 1 }} />
              <Typography color="text.secondary" gutterBottom>
                Current Exposure
              </Typography>
              <Typography variant="h5" component="div">
                {formatCurrency(analysis.currentExposure)}
              </Typography>
              <Chip
                label={analysis.creditUtilization > 0.8 ? 'High Risk' : 'Normal'}
                color={analysis.creditUtilization > 0.8 ? 'error' : 'success'}
                size="small"
                sx={{ mt: 1 }}
              />
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <BusinessIcon color="info" sx={{ fontSize: 40, mb: 1 }} />
              <Typography color="text.secondary" gutterBottom>
                Total Cooperation
              </Typography>
              <Typography variant="h5" component="div">
                {formatCurrency(analysis.totalCooperationAmount)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Quantity: {formatNumber(analysis.totalCooperationQuantity)} MT
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <AssessmentIcon 
                color={getHealthStatusColor(analysis.financialHealthStatus)} 
                sx={{ fontSize: 40, mb: 1 }} 
              />
              <Typography color="text.secondary" gutterBottom>
                Financial Health
              </Typography>
              <Typography variant="h5" component="div">
                {analysis.financialHealthScore || 'N/A'}
              </Typography>
              <Chip
                label={analysis.financialHealthStatus}
                color={getHealthStatusColor(analysis.financialHealthStatus)}
                size="small"
                sx={{ mt: 1 }}
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs for Detailed Analysis */}
      <Paper>
        <Tabs value={tabValue} onChange={handleTabChange} aria-label="analysis tabs">
          <Tab label="Financial Trends" />
          <Tab label="Financial Ratios" />
          <Tab label="Risk Assessment" />
          <Tab label="Reports History" />
        </Tabs>

        {/* Financial Trends Tab */}
        <TabPanel value={tabValue} index={0}>
          <Grid container spacing={3}>
            <Grid item xs={12} lg={8}>
              <Typography variant="h6" gutterBottom>
                Financial Performance Trends
              </Typography>
              {chartData.revenue.length > 0 || chartData.netIncome.length > 0 || chartData.totalAssets.length > 0 ? (
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={chartData.revenue}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="year" />
                    <YAxis tickFormatter={(value) => `$${(value / 1000000).toFixed(1)}M`} />
                    <RechartsTooltip 
                      formatter={(value: number, name: string) => [formatCurrency(value), name]}
                    />
                    {chartData.revenue.length > 0 && (
                      <Line 
                        type="monotone" 
                        dataKey="value" 
                        stroke="#2196F3" 
                        strokeWidth={2}
                        name="Revenue"
                        data={chartData.revenue}
                      />
                    )}
                    {chartData.netIncome.length > 0 && (
                      <Line 
                        type="monotone" 
                        dataKey="value" 
                        stroke="#4CAF50" 
                        strokeWidth={2}
                        name="Net Income"
                        data={chartData.netIncome}
                      />
                    )}
                    {chartData.totalAssets.length > 0 && (
                      <Line 
                        type="monotone" 
                        dataKey="value" 
                        stroke="#FF9800" 
                        strokeWidth={2}
                        name="Total Assets"
                        data={chartData.totalAssets}
                      />
                    )}
                  </LineChart>
                </ResponsiveContainer>
              ) : (
                <Alert severity="info">No trend data available for chart visualization.</Alert>
              )}
            </Grid>
            
            <Grid item xs={12} lg={4}>
              <Typography variant="h6" gutterBottom>
                Growth Metrics
              </Typography>
              {analysis.trends.length > 0 ? (
                <Stack spacing={2}>
                  {analysis.trends.map((trend, index) => (
                    <Card key={index} variant="outlined">
                      <CardContent>
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="subtitle2">
                            {trend.metric}
                          </Typography>
                          {getTrendIcon(trend.isPositiveTrend)}
                        </Box>
                        <Typography variant="h6" color={trend.isPositiveTrend ? 'success.main' : 'error.main'}>
                          {trend.growthRate ? `${(trend.growthRate * 100).toFixed(1)}%` : 'N/A'}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          Year-over-Year Growth
                        </Typography>
                      </CardContent>
                    </Card>
                  ))}
                </Stack>
              ) : (
                <Alert severity="info">No growth metrics available.</Alert>
              )}
            </Grid>
          </Grid>
        </TabPanel>

        {/* Financial Ratios Tab */}
        <TabPanel value={tabValue} index={1}>
          <Typography variant="h6" gutterBottom>
            Financial Ratios Analysis
          </Typography>
          {analysis.ratios?.length > 0 ? (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Ratio</TableCell>
                    <TableCell>Category</TableCell>
                    <TableCell align="right">Value</TableCell>
                    <TableCell align="right">Benchmark</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Description</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {analysis.ratios.map((ratio, index) => (
                    <TableRow key={index} hover>
                      <TableCell>
                        <Typography variant="subtitle2">
                          {ratio.name}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={ratio.category}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" fontWeight="medium">
                          {ratio.value !== null ? ratio.value.toFixed(2) : 'N/A'}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" color="text.secondary">
                          {ratio.benchmark ? ratio.benchmark.toFixed(2) : 'N/A'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={ratio.isHealthy ? 'Healthy' : 'Warning'}
                          color={ratio.isHealthy ? 'success' : 'warning'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {ratio.description}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          ) : (
            <Alert severity="info">No financial ratios data available.</Alert>
          )}
        </TabPanel>

        {/* Risk Assessment Tab */}
        <TabPanel value={tabValue} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Credit Risk Assessment
                  </Typography>
                  <Box display="flex" alignItems="center" mb={2}>
                    <BalanceIcon 
                      color={getCreditRiskColor(analysis.creditRisk)} 
                      sx={{ mr: 1 }} 
                    />
                    <Chip
                      label={analysis.creditRisk}
                      color={getCreditRiskColor(analysis.creditRisk)}
                      sx={{ mr: 2 }}
                    />
                    <Typography variant="body2" color="text.secondary">
                      Risk Level
                    </Typography>
                  </Box>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="subtitle2" gutterBottom>
                    Data Quality
                  </Typography>
                  <Chip
                    label={analysis.dataQuality}
                    color={getDataQualityColor(analysis.dataQuality)}
                    size="small"
                  />
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Analysis Date & Reports
                  </Typography>
                  <Stack spacing={2}>
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Last Analysis
                      </Typography>
                      <Typography variant="body1">
                        {formatDisplayDate(new Date(analysis.analysisDate))}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Financial Reports Count
                      </Typography>
                      <Typography variant="h6">
                        {analysis.financialReportCount}
                      </Typography>
                    </Box>
                    {analysis.latestFinancialReport && (
                      <Box>
                        <Typography variant="body2" color="text.secondary">
                          Latest Report Period
                        </Typography>
                        <Typography variant="body1">
                          {formatDisplayDate(new Date(analysis.latestFinancialReport.reportStartDate))} - {formatDisplayDate(new Date(analysis.latestFinancialReport.reportEndDate))}
                        </Typography>
                      </Box>
                    )}
                  </Stack>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Recommendations
                  </Typography>
                  {analysis.recommendations?.length > 0 ? (
                    <Stack spacing={1}>
                      {analysis.recommendations.map((recommendation, index) => (
                        <Alert key={index} severity="info" variant="outlined">
                          {recommendation}
                        </Alert>
                      ))}
                    </Stack>
                  ) : (
                    <Alert severity="info">No specific recommendations at this time.</Alert>
                  )}
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Reports History Tab */}
        <TabPanel value={tabValue} index={3}>
          <Typography variant="h6" gutterBottom>
            Financial Reports History
          </Typography>
          {analysis.latestFinancialReport ? (
            <TableContainer component={Paper} variant="outlined">
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Report Period</TableCell>
                    <TableCell align="right">Revenue</TableCell>
                    <TableCell align="right">Net Income</TableCell>
                    <TableCell align="right">Total Assets</TableCell>
                    <TableCell>Audited</TableCell>
                    <TableCell>Created</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  <TableRow hover>
                    <TableCell>
                      <Typography variant="body2">
                        {formatDisplayDate(new Date(analysis.latestFinancialReport.reportStartDate))} - {formatDisplayDate(new Date(analysis.latestFinancialReport.reportEndDate))}
                      </Typography>
                    </TableCell>
                    <TableCell align="right">
                      {formatCurrency(analysis.latestFinancialReport.revenue)}
                    </TableCell>
                    <TableCell align="right">
                      {formatCurrency(analysis.latestFinancialReport.netIncome)}
                    </TableCell>
                    <TableCell align="right">
                      {formatCurrency(analysis.latestFinancialReport.totalAssets)}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={analysis.latestFinancialReport.isAudited ? 'Yes' : 'No'}
                        color={analysis.latestFinancialReport.isAudited ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" color="text.secondary">
                        {formatDisplayDate(new Date(analysis.latestFinancialReport.createdAt))}
                      </Typography>
                    </TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </TableContainer>
          ) : (
            <Alert severity="info">
              No financial reports available. Financial data helps improve analysis accuracy.
            </Alert>
          )}
        </TabPanel>
      </Paper>
    </Box>
  );
};