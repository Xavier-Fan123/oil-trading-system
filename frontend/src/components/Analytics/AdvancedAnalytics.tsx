import { useState, useMemo } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  ButtonGroup,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
} from '@mui/material';
import {
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ScatterChart,
  Scatter,
  Area,
  AreaChart,
  RadialBarChart,
  RadialBar,
} from 'recharts';
import { format, subDays } from 'date-fns';

type AnalyticsTimeRange = 'today' | '7d' | '30d' | '90d' | 'ytd';
type AnalyticsView = 'overview' | 'products' | 'partners' | 'risk' | 'performance';

// Mock data generators
const generatePortfolioData = () => [
  { name: 'Brent Crude', value: 125000000, percentage: 35, contracts: 45, color: '#8884d8' },
  { name: 'WTI Crude', value: 89000000, percentage: 25, contracts: 32, color: '#82ca9d' },
  { name: 'Marine Gas Oil', value: 71000000, percentage: 20, contracts: 28, color: '#ffc658' },
  { name: 'Jet Fuel', value: 43000000, percentage: 12, contracts: 18, color: '#ff7300' },
  { name: 'High Sulfur Fuel Oil', value: 28000000, percentage: 8, contracts: 12, color: '#00ff7f' },
];

const generatePerformanceData = (range: AnalyticsTimeRange) => {
  const days = range === 'today' ? 1 : range === '7d' ? 7 : range === '30d' ? 30 : range === '90d' ? 90 : 365;
  const data = [];
  const baseValue = 100;
  
  for (let i = 0; i < Math.min(days, 30); i++) {
    const date = subDays(new Date(), days - i);
    const performance = baseValue + (Math.random() - 0.48) * 20 + (i * 0.1);
    const volume = Math.floor(Math.random() * 5000000) + 1000000;
    
    data.push({
      date: format(date, 'MMM dd'),
      performance: Math.round(performance * 100) / 100,
      volume,
      pnl: (Math.random() - 0.5) * 2000000,
      trades: Math.floor(Math.random() * 50) + 10,
    });
  }
  return data;
};

const generateRiskMetrics = () => [
  { metric: 'VaR (95%)', current: 8.5, limit: 10.0, utilization: 85, status: 'warning' },
  { metric: 'VaR (99%)', current: 12.3, limit: 15.0, utilization: 82, status: 'normal' },
  { metric: 'Expected Shortfall', current: 15.8, limit: 20.0, utilization: 79, status: 'normal' },
  { metric: 'Max Drawdown', current: 6.2, limit: 8.0, utilization: 77, status: 'normal' },
  { metric: 'Concentration Risk', current: 35.0, limit: 40.0, utilization: 87, status: 'warning' },
  { metric: 'Stress Test Loss', current: 25.5, limit: 30.0, utilization: 85, status: 'warning' },
];

const generateTradingPartnerData = () => [
  { name: 'Shell Trading', volume: 125000000, contracts: 28, margin: 5.8, risk: 'Low' },
  { name: 'BP Trading', volume: 98000000, contracts: 22, margin: 6.2, risk: 'Low' },
  { name: 'ExxonMobil', volume: 87000000, contracts: 19, margin: 7.1, risk: 'Medium' },
  { name: 'Vitol Asia', volume: 76000000, contracts: 17, margin: 4.9, risk: 'Low' },
  { name: 'Trafigura', volume: 65000000, contracts: 15, margin: 5.5, risk: 'Medium' },
];

const generateCorrelationData = () => [
  { x: 85.5, y: 78.2, product: 'Brent vs WTI' },
  { x: 82.1, y: 165.3, product: 'Brent vs MGO' },
  { x: 78.2, y: 89.7, product: 'WTI vs Jet' },
  { x: 165.3, y: 198.5, product: 'MGO vs HSFO' },
  { x: 89.7, y: 85.5, product: 'Jet vs Brent' },
];

const AdvancedAnalytics: React.FC = () => {
  const [timeRange, setTimeRange] = useState<AnalyticsTimeRange>('30d');
  const [view, setView] = useState<AnalyticsView>('overview');

  const portfolioData = useMemo(() => generatePortfolioData(), []);
  const performanceData = useMemo(() => generatePerformanceData(timeRange), [timeRange]);
  const riskMetrics = useMemo(() => generateRiskMetrics(), []);
  const tradingPartnerData = useMemo(() => generateTradingPartnerData(), []);
  const correlationData = useMemo(() => generateCorrelationData(), []);

  const getRiskColor = (status: string) => {
    switch (status) {
      case 'normal': return '#4caf50';
      case 'warning': return '#ff9800';
      case 'critical': return '#f44336';
      default: return '#2196f3';
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      notation: 'compact',
      maximumFractionDigits: 1,
    }).format(value);
  };

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <Box
          sx={{
            bgcolor: 'background.paper',
            border: '1px solid #ccc',
            borderRadius: 1,
            p: 1,
            boxShadow: 2,
          }}
        >
          <Typography variant="body2" fontWeight="bold">
            {label}
          </Typography>
          {payload.map((entry: any, index: number) => (
            <Typography key={index} variant="body2" color={entry.color}>
              {entry.name}: {typeof entry.value === 'number' && entry.name.includes('$') 
                ? formatCurrency(entry.value) 
                : entry.value}
            </Typography>
          ))}
        </Box>
      );
    }
    return null;
  };

  const renderOverview = () => (
    <Grid container spacing={3}>
      <Grid item xs={12} md={6}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Portfolio Distribution
            </Typography>
            <Box height={300}>
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={portfolioData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={120}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {portfolioData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: number) => formatCurrency(value)} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} md={6}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Performance Trend
            </Typography>
            <Box height={300}>
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={performanceData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" />
                  <YAxis />
                  <Tooltip content={<CustomTooltip />} />
                  <Area
                    type="monotone"
                    dataKey="performance"
                    stroke="#8884d8"
                    fill="#8884d8"
                    fillOpacity={0.3}
                  />
                </AreaChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              P&L and Volume Analysis
            </Typography>
            <Box height={300}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={performanceData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" />
                  <YAxis yAxisId="left" />
                  <YAxis yAxisId="right" orientation="right" />
                  <Tooltip content={<CustomTooltip />} />
                  <Legend />
                  <Bar yAxisId="left" dataKey="pnl" fill="#82ca9d" name="P&L ($)" />
                  <Bar yAxisId="right" dataKey="volume" fill="#8884d8" name="Volume" />
                </BarChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );

  const renderRiskAnalytics = () => (
    <Grid container spacing={3}>
      <Grid item xs={12} md={8}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Risk Metrics Utilization
            </Typography>
            <Box height={400}>
              <ResponsiveContainer width="100%" height="100%">
                <RadialBarChart
                  cx="50%"
                  cy="50%"
                  innerRadius="20%"
                  outerRadius="80%"
                  data={riskMetrics}
                >
                  <RadialBar
                    label={{ position: 'insideStart', fill: '#fff' }}
                    background
                    dataKey="utilization"
                    fill="#8884d8"
                  />
                  <Legend />
                  <Tooltip />
                </RadialBarChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12} md={4}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Risk Limits Status
            </Typography>
            <Box>
              {riskMetrics.map((metric, index) => (
                <Box key={index} mb={2}>
                  <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                    <Typography variant="body2">{metric.metric}</Typography>
                    <Chip
                      size="small"
                      label={metric.status}
                      color={metric.status === 'normal' ? 'success' : 'warning'}
                    />
                  </Box>
                  <Typography variant="body2" color="textSecondary">
                    {metric.current.toFixed(1)} / {metric.limit.toFixed(1)} ({metric.utilization}%)
                  </Typography>
                  <Box
                    sx={{
                      width: '100%',
                      height: 8,
                      bgcolor: 'grey.200',
                      borderRadius: 1,
                      overflow: 'hidden',
                    }}
                  >
                    <Box
                      sx={{
                        width: `${metric.utilization}%`,
                        height: '100%',
                        bgcolor: getRiskColor(metric.status),
                        transition: 'width 0.3s ease',
                      }}
                    />
                  </Box>
                </Box>
              ))}
            </Box>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Price Correlation Analysis
            </Typography>
            <Box height={300}>
              <ResponsiveContainer width="100%" height="100%">
                <ScatterChart data={correlationData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" dataKey="x" />
                  <YAxis type="number" dataKey="y" />
                  <Tooltip content={<CustomTooltip />} />
                  <Scatter name="Products" dataKey="y" fill="#8884d8" />
                </ScatterChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );

  const renderTradingPartners = () => (
    <Grid container spacing={3}>
      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Trading Partner Performance
            </Typography>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Partner</TableCell>
                    <TableCell align="right">Volume</TableCell>
                    <TableCell align="right">Contracts</TableCell>
                    <TableCell align="right">Margin %</TableCell>
                    <TableCell align="center">Risk Level</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {tradingPartnerData.map((partner, index) => (
                    <TableRow key={index}>
                      <TableCell component="th" scope="row">
                        {partner.name}
                      </TableCell>
                      <TableCell align="right">{formatCurrency(partner.volume)}</TableCell>
                      <TableCell align="right">{partner.contracts}</TableCell>
                      <TableCell align="right">{partner.margin.toFixed(1)}%</TableCell>
                      <TableCell align="center">
                        <Chip
                          label={partner.risk}
                          color={partner.risk === 'Low' ? 'success' : partner.risk === 'Medium' ? 'warning' : 'error'}
                          size="small"
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      </Grid>

      <Grid item xs={12}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Volume by Trading Partner
            </Typography>
            <Box height={300}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={tradingPartnerData} layout="horizontal">
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" tickFormatter={(value) => formatCurrency(value)} />
                  <YAxis type="category" dataKey="name" width={120} />
                  <Tooltip formatter={(value: number) => formatCurrency(value)} />
                  <Bar dataKey="volume" fill="#8884d8" />
                </BarChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Grid>
    </Grid>
  );

  const renderContent = () => {
    switch (view) {
      case 'overview':
        return renderOverview();
      case 'risk':
        return renderRiskAnalytics();
      case 'partners':
        return renderTradingPartners();
      default:
        return renderOverview();
    }
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5" component="h2">
          Advanced Analytics
        </Typography>
        <Box display="flex" gap={2}>
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Time Range</InputLabel>
            <Select
              value={timeRange}
              onChange={(e) => setTimeRange(e.target.value as AnalyticsTimeRange)}
              label="Time Range"
            >
              <MenuItem value="today">Today</MenuItem>
              <MenuItem value="7d">7 Days</MenuItem>
              <MenuItem value="30d">30 Days</MenuItem>
              <MenuItem value="90d">90 Days</MenuItem>
              <MenuItem value="ytd">Year to Date</MenuItem>
            </Select>
          </FormControl>
          <ButtonGroup variant="outlined" size="small">
            <Button
              variant={view === 'overview' ? 'contained' : 'outlined'}
              onClick={() => setView('overview')}
            >
              Overview
            </Button>
            <Button
              variant={view === 'risk' ? 'contained' : 'outlined'}
              onClick={() => setView('risk')}
            >
              Risk
            </Button>
            <Button
              variant={view === 'partners' ? 'contained' : 'outlined'}
              onClick={() => setView('partners')}
            >
              Partners
            </Button>
          </ButtonGroup>
        </Box>
      </Box>

      {renderContent()}
    </Box>
  );
};

export default AdvancedAnalytics;