import React, { useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Typography,
  Button,
  ButtonGroup,
  Chip,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  LinearProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Tabs,
  Tab,
  Divider,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  Warning,
  CheckCircle,
  Assessment,
  Speed,
  Security,
  Settings,
  Refresh,
  MoreVert,
  ShowChart,
} from '@mui/icons-material';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  Area,
  BarChart as RechartsBarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ComposedChart,
} from 'recharts';
import { format } from 'date-fns';
import { useRisk } from '../../hooks/useRisk';

interface RiskLimit {
  id: string;
  name: string;
  type: 'VaR' | 'Position' | 'Concentration' | 'Volatility';
  limit: number;
  current: number;
  utilization: number;
  status: 'Normal' | 'Warning' | 'Breach';
  lastUpdated: Date;
}

interface StressTestScenario {
  id: string;
  name: string;
  description: string;
  shockType: string;
  severity: 'Low' | 'Medium' | 'High' | 'Extreme';
  impactOnVaR: number;
  impactOnPortfolio: number;
  probability: number;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => {
  return (
    <div role="tabpanel" hidden={value !== index}>
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  );
};

const mockRiskLimits: RiskLimit[] = [
  {
    id: '1',
    name: 'Daily VaR 95%',
    type: 'VaR',
    limit: 500000,
    current: 342000,
    utilization: 68.4,
    status: 'Normal',
    lastUpdated: new Date(),
  },
  {
    id: '2',
    name: 'Daily VaR 99%',
    type: 'VaR',
    limit: 750000,
    current: 485000,
    utilization: 64.7,
    status: 'Normal',
    lastUpdated: new Date(),
  },
  {
    id: '3',
    name: 'Position Limit - Brent',
    type: 'Position',
    limit: 100000,
    current: 85000,
    utilization: 85.0,
    status: 'Warning',
    lastUpdated: new Date(),
  },
  {
    id: '4',
    name: 'Concentration Risk',
    type: 'Concentration',
    limit: 40,
    current: 35,
    utilization: 87.5,
    status: 'Warning',
    lastUpdated: new Date(),
  },
];

const mockStressTests: StressTestScenario[] = [
  {
    id: '1',
    name: 'Oil Price Crash',
    description: 'Sudden 20% drop in oil prices due to oversupply',
    shockType: 'Price Shock',
    severity: 'High',
    impactOnVaR: 1.45,
    impactOnPortfolio: -1.2,
    probability: 15,
  },
  {
    id: '2',
    name: 'Geopolitical Crisis',
    description: 'Supply disruption from major oil producing region',
    shockType: 'Supply Shock',
    severity: 'Extreme',
    impactOnVaR: 2.1,
    impactOnPortfolio: 0.8,
    probability: 8,
  },
  {
    id: '3',
    name: 'Economic Recession',
    description: 'Global recession reducing energy demand',
    shockType: 'Demand Shock',
    severity: 'Medium',
    impactOnVaR: 1.25,
    impactOnPortfolio: -0.9,
    probability: 25,
  },
];

const varHistoricalData = Array.from({ length: 30 }, (_, i) => ({
  date: format(new Date(Date.now() - (29 - i) * 24 * 60 * 60 * 1000), 'MM/dd'),
  var95: 300000 + Math.random() * 200000,
  var99: 450000 + Math.random() * 300000,
  actualPnL: (Math.random() - 0.5) * 400000,
  limit95: 500000,
  limit99: 750000,
}));

const riskByProductData = [
  { product: 'Brent', var95: 180000, var99: 280000, exposure: 85000, color: '#8884d8' },
  { product: 'WTI', var95: 120000, var99: 190000, exposure: 65000, color: '#82ca9d' },
  { product: 'MGO', var95: 80000, var99: 125000, exposure: 45000, color: '#ffc658' },
  { product: 'ULSD', var95: 45000, var99: 70000, exposure: 30000, color: '#ff7300' },
];

const riskContributionData = [
  { name: 'Market Risk', value: 65, color: '#8884d8' },
  { name: 'Credit Risk', value: 20, color: '#82ca9d' },
  { name: 'Operational Risk', value: 10, color: '#ffc658' },
  { name: 'Liquidity Risk', value: 5, color: '#ff7300' },
];

export const EnhancedRiskDashboard: React.FC = () => {
  const { data: riskData, isLoading, error } = useRisk();
  const [activeTab, setActiveTab] = useState(0);
  // const [selectedTimeframe] = useState('1M'); // Commented out - not currently used
  const [stressTestDialog, setStressTestDialog] = useState(false);
  const [limitConfigDialog, setLimitConfigDialog] = useState(false);
  const [selectedScenario, setSelectedScenario] = useState<StressTestScenario | null>(null);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Normal': return 'success';
      case 'Warning': return 'warning';
      case 'Breach': return 'error';
      default: return 'default';
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'Low': return '#4caf50';
      case 'Medium': return '#ff9800';
      case 'High': return '#f44336';
      case 'Extreme': return '#9c27b0';
      default: return '#757575';
    }
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <Typography>Loading risk data...</Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error">
        Error loading risk data: {error.message}
      </Alert>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Risk Management Dashboard
        </Typography>
        <Box display="flex" gap={2}>
          <ButtonGroup variant="outlined" size="small">
            <Button onClick={() => {}}>1D</Button>
            <Button onClick={() => {}}>1W</Button>
            <Button onClick={() => {}}>1M</Button>
            <Button onClick={() => {}}>3M</Button>
          </ButtonGroup>
          <Button
            variant="outlined"
            startIcon={<Assessment />}
            onClick={() => setStressTestDialog(true)}
          >
            Stress Test
          </Button>
          <Button
            variant="outlined"
            startIcon={<Settings />}
            onClick={() => setLimitConfigDialog(true)}
          >
            Limits
          </Button>
          <IconButton>
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {/* Alert Summary */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12}>
          <Alert
            severity={mockRiskLimits.some(l => l.status === 'Breach') ? 'error' : 
                     mockRiskLimits.some(l => l.status === 'Warning') ? 'warning' : 'info'}
            action={
              <Button color="inherit" size="small">
                View Details
              </Button>
            }
          >
            <Typography variant="subtitle2">
              Risk Status: {mockRiskLimits.filter(l => l.status === 'Warning').length} Warning(s), {' '}
              {mockRiskLimits.filter(l => l.status === 'Breach').length} Breach(es)
            </Typography>
          </Alert>
        </Grid>
      </Grid>

      {/* Navigation Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange}>
          <Tab label="Overview" icon={<Assessment />} />
          <Tab label="VaR Analysis" icon={<ShowChart />} />
          <Tab label="Stress Testing" icon={<Speed />} />
          <Tab label="Limits Monitor" icon={<Security />} />
        </Tabs>
      </Box>

      {/* Tab Panels */}
      <TabPanel value={activeTab} index={0}>
        {/* Overview Tab */}
        <Grid container spacing={3}>
          {/* Key Risk Metrics */}
          <Grid item xs={12} md={8}>
            <Card>
              <CardHeader 
                title="Portfolio Risk Metrics" 
                action={
                  <Chip 
                    label={`Last Updated: ${format(new Date(), 'HH:mm')}`}
                    size="small"
                    color="primary"
                  />
                }
              />
              <CardContent>
                <Grid container spacing={3}>
                  <Grid item xs={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="primary" fontWeight="bold">
                        ${(riskData?.riskMetrics?.var95 || 342000).toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        VaR 95% (1-day)
                      </Typography>
                      <Box display="flex" alignItems="center" justifyContent="center" mt={1}>
                        <TrendingDown color="success" sx={{ fontSize: 16, mr: 0.5 }} />
                        <Typography variant="caption" color="success.main">
                          -2.3%
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                  <Grid item xs={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="error" fontWeight="bold">
                        ${(riskData?.riskMetrics?.var99 || 485000).toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        VaR 99% (1-day)
                      </Typography>
                      <Box display="flex" alignItems="center" justifyContent="center" mt={1}>
                        <TrendingUp color="error" sx={{ fontSize: 16, mr: 0.5 }} />
                        <Typography variant="caption" color="error.main">
                          +1.2%
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                  <Grid item xs={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="info.main" fontWeight="bold">
                        ${(riskData?.riskMetrics?.expectedShortfall95 || 612000).toLocaleString()}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Expected Shortfall
                      </Typography>
                      <Box display="flex" alignItems="center" justifyContent="center" mt={1}>
                        <TrendingDown color="success" sx={{ fontSize: 16, mr: 0.5 }} />
                        <Typography variant="caption" color="success.main">
                          -0.8%
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                  <Grid item xs={6} md={3}>
                    <Box textAlign="center">
                      <Typography variant="h4" color="warning.main" fontWeight="bold">
                        15.4%
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Portfolio Volatility
                      </Typography>
                      <Box display="flex" alignItems="center" justifyContent="center" mt={1}>
                        <TrendingUp color="warning" sx={{ fontSize: 16, mr: 0.5 }} />
                        <Typography variant="caption" color="warning.main">
                          +0.3%
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            {/* VaR Chart */}
            <Card sx={{ mt: 3 }}>
              <CardHeader title="VaR Trend & P&L" />
              <CardContent>
                <Box height={300}>
                  <ResponsiveContainer width="100%" height="100%">
                    <ComposedChart data={varHistoricalData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="date" />
                      <YAxis />
                      <RechartsTooltip 
                        formatter={(value: number) => [`$${value.toLocaleString()}`, '']}
                      />
                      <Legend />
                      <Area 
                        type="monotone" 
                        dataKey="var95" 
                        fill="#8884d8" 
                        fillOpacity={0.3}
                        stroke="#8884d8"
                        name="VaR 95%"
                      />
                      <Area 
                        type="monotone" 
                        dataKey="var99" 
                        fill="#82ca9d" 
                        fillOpacity={0.2}
                        stroke="#82ca9d"
                        name="VaR 99%"
                      />
                      <Line 
                        type="monotone" 
                        dataKey="actualPnL" 
                        stroke="#ff7300"
                        strokeWidth={2}
                        name="Actual P&L"
                        dot={false}
                      />
                      <Line 
                        type="monotone" 
                        dataKey="limit95" 
                        stroke="#f44336"
                        strokeDasharray="5 5"
                        name="VaR 95% Limit"
                        dot={false}
                      />
                    </ComposedChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Risk by Product & Risk Contribution */}
          <Grid item xs={12} md={4}>
            <Card>
              <CardHeader title="Risk by Product" />
              <CardContent>
                <Box height={250}>
                  <ResponsiveContainer width="100%" height="100%">
                    <RechartsBarChart data={riskByProductData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="product" />
                      <YAxis />
                      <RechartsTooltip 
                        formatter={(value: number) => [`$${value.toLocaleString()}`, '']}
                      />
                      <Bar dataKey="var95" fill="#8884d8" name="VaR 95%" />
                    </RechartsBarChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardHeader title="Risk Contribution" />
              <CardContent>
                <Box height={200}>
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={riskContributionData}
                        cx="50%"
                        cy="50%"
                        outerRadius={80}
                        dataKey="value"
                        label={({ name, value }) => `${name}: ${value}%`}
                      >
                        {riskContributionData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={entry.color} />
                        ))}
                      </Pie>
                      <RechartsTooltip />
                    </PieChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardHeader title="Risk Alerts" />
              <CardContent>
                <List dense>
                  <ListItem>
                    <ListItemIcon>
                      <Warning color="warning" />
                    </ListItemIcon>
                    <ListItemText
                      primary="Position Limit Alert"
                      secondary="Brent exposure at 85% of limit"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Warning color="warning" />
                    </ListItemIcon>
                    <ListItemText
                      primary="Concentration Risk"
                      secondary="Single product exposure high"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <CheckCircle color="success" />
                    </ListItemIcon>
                    <ListItemText
                      primary="VaR within limits"
                      secondary="All VaR metrics normal"
                    />
                  </ListItem>
                </List>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        {/* VaR Analysis Tab */}
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Card>
              <CardHeader title="VaR Model Comparison" />
              <CardContent>
                <Box height={400}>
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={varHistoricalData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="date" />
                      <YAxis />
                      <RechartsTooltip 
                        formatter={(value: number) => [`$${value.toLocaleString()}`, '']}
                      />
                      <Legend />
                      <Line 
                        type="monotone" 
                        dataKey="var95" 
                        stroke="#8884d8"
                        name="Historical Simulation VaR 95%"
                      />
                      <Line 
                        type="monotone" 
                        dataKey="var99" 
                        stroke="#82ca9d"
                        name="GARCH(1,1) VaR 99%"
                      />
                      <Line 
                        type="monotone" 
                        dataKey="actualPnL" 
                        stroke="#ff7300"
                        name="Monte Carlo VaR"
                        strokeDasharray="3 3"
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardHeader title="VaR Backtesting" />
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Model Performance
                </Typography>
                <Box mb={2}>
                  <Typography variant="body2" color="text.secondary">
                    VaR 95% Exception Rate
                  </Typography>
                  <LinearProgress 
                    variant="determinate" 
                    value={4.2} 
                    color="success"
                    sx={{ height: 8, borderRadius: 1 }}
                  />
                  <Typography variant="caption">
                    4.2% (Expected: 5.0%)
                  </Typography>
                </Box>
                <Box mb={2}>
                  <Typography variant="body2" color="text.secondary">
                    VaR 99% Exception Rate
                  </Typography>
                  <LinearProgress 
                    variant="determinate" 
                    value={0.8} 
                    color="success"
                    sx={{ height: 8, borderRadius: 1 }}
                  />
                  <Typography variant="caption">
                    0.8% (Expected: 1.0%)
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardHeader title="Model Statistics" />
              <CardContent>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">
                      Kupiec Test
                    </Typography>
                    <Typography variant="h6" color="success.main">
                      Pass
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">
                      Christoffersen Test
                    </Typography>
                    <Typography variant="h6" color="success.main">
                      Pass
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">
                      Average VaR
                    </Typography>
                    <Typography variant="h6">
                      $425K
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">
                      Max VaR
                    </Typography>
                    <Typography variant="h6" color="error.main">
                      $680K
                    </Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        {/* Stress Testing Tab */}
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardHeader 
                title="Stress Test Scenarios" 
                action={
                  <Button
                    variant="contained"
                    onClick={() => setStressTestDialog(true)}
                  >
                    Run Stress Test
                  </Button>
                }
              />
              <CardContent>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Scenario</TableCell>
                        <TableCell>Severity</TableCell>
                        <TableCell align="right">VaR Impact</TableCell>
                        <TableCell align="right">Portfolio Impact</TableCell>
                        <TableCell align="right">Probability</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {mockStressTests.map((scenario) => (
                        <TableRow key={scenario.id}>
                          <TableCell>
                            <Box>
                              <Typography variant="subtitle2">
                                {scenario.name}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {scenario.description}
                              </Typography>
                            </Box>
                          </TableCell>
                          <TableCell>
                            <Chip 
                              label={scenario.severity}
                              size="small"
                              sx={{ 
                                bgcolor: getSeverityColor(scenario.severity),
                                color: 'white'
                              }}
                            />
                          </TableCell>
                          <TableCell align="right">
                            <Typography 
                              color={scenario.impactOnVaR > 1 ? 'error' : 'warning'}
                              fontWeight="bold"
                            >
                              {scenario.impactOnVaR.toFixed(2)}x
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            <Typography 
                              color={scenario.impactOnPortfolio > 0 ? 'success' : 'error'}
                              fontWeight="bold"
                            >
                              {scenario.impactOnPortfolio > 0 ? '+' : ''}{scenario.impactOnPortfolio.toFixed(1)}%
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            {scenario.probability}%
                          </TableCell>
                          <TableCell>
                            <Button
                              size="small"
                              onClick={() => setSelectedScenario(scenario)}
                            >
                              Run
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardHeader title="Stress Test Results" />
              <CardContent>
                {selectedScenario ? (
                  <Box>
                    <Typography variant="h6" gutterBottom>
                      {selectedScenario.name}
                    </Typography>
                    <Box mb={2}>
                      <Typography variant="body2" color="text.secondary">
                        VaR Impact
                      </Typography>
                      <Typography variant="h4" color="error.main">
                        {selectedScenario.impactOnVaR.toFixed(2)}x
                      </Typography>
                    </Box>
                    <Box mb={2}>
                      <Typography variant="body2" color="text.secondary">
                        Portfolio Impact
                      </Typography>
                      <Typography 
                        variant="h4" 
                        color={selectedScenario.impactOnPortfolio > 0 ? 'success.main' : 'error.main'}
                      >
                        {selectedScenario.impactOnPortfolio > 0 ? '+' : ''}{selectedScenario.impactOnPortfolio.toFixed(1)}%
                      </Typography>
                    </Box>
                    <Divider sx={{ my: 2 }} />
                    <Typography variant="body2" color="text.secondary">
                      {selectedScenario.description}
                    </Typography>
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    Select a scenario to view results
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        {/* Limits Monitor Tab */}
        <Card>
          <CardHeader 
            title="Risk Limits Monitor" 
            action={
              <Button
                variant="outlined"
                startIcon={<Settings />}
                onClick={() => setLimitConfigDialog(true)}
              >
                Configure Limits
              </Button>
            }
          />
          <CardContent>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Limit Name</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell align="right">Limit</TableCell>
                    <TableCell align="right">Current</TableCell>
                    <TableCell align="right">Utilization</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Last Updated</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {mockRiskLimits.map((limit) => (
                    <TableRow key={limit.id}>
                      <TableCell>
                        <Typography variant="subtitle2">
                          {limit.name}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip label={limit.type} size="small" variant="outlined" />
                      </TableCell>
                      <TableCell align="right">
                        {limit.type === 'Concentration' 
                          ? `${limit.limit}%`
                          : `$${limit.limit.toLocaleString()}`
                        }
                      </TableCell>
                      <TableCell align="right">
                        {limit.type === 'Concentration' 
                          ? `${limit.current}%`
                          : `$${limit.current.toLocaleString()}`
                        }
                      </TableCell>
                      <TableCell align="right">
                        <Box display="flex" alignItems="center">
                          <Box width="100%" mr={1}>
                            <LinearProgress
                              variant="determinate"
                              value={limit.utilization}
                              color={
                                limit.utilization > 90 ? 'error' :
                                limit.utilization > 80 ? 'warning' : 'success'
                              }
                            />
                          </Box>
                          <Typography variant="body2" sx={{ minWidth: 35 }}>
                            {limit.utilization.toFixed(1)}%
                          </Typography>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={limit.status}
                          color={getStatusColor(limit.status) as any}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {format(limit.lastUpdated, 'HH:mm:ss')}
                      </TableCell>
                      <TableCell>
                        <IconButton size="small">
                          <MoreVert />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      </TabPanel>

      {/* Stress Test Dialog */}
      <Dialog 
        open={stressTestDialog} 
        onClose={() => setStressTestDialog(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Run Stress Test</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <FormControl fullWidth>
                <InputLabel>Select Scenario</InputLabel>
                <Select
                  value=""
                  label="Select Scenario"
                >
                  {mockStressTests.map((scenario) => (
                    <MenuItem key={scenario.id} value={scenario.id}>
                      {scenario.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Custom Shock (%)"
                type="number"
                placeholder="Enter custom shock percentage"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setStressTestDialog(false)}>
            Cancel
          </Button>
          <Button variant="contained">
            Run Test
          </Button>
        </DialogActions>
      </Dialog>

      {/* Limit Configuration Dialog */}
      <Dialog 
        open={limitConfigDialog} 
        onClose={() => setLimitConfigDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Configure Risk Limits</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" paragraph>
            Risk limit configuration will be implemented in the next phase.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLimitConfigDialog(false)}>
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};