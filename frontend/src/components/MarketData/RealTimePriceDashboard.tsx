import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Typography,
  Button,
  IconButton,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Switch,
  FormControlLabel,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  Badge,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Avatar,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  TrendingFlat,
  Notifications,
  NotificationsActive,
  Settings,
  Refresh,
  PlayArrow,
  Pause,
  Warning,
  CheckCircle,
  Add,
  Delete,
  Edit,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { AdvancedChart, ChartDataPoint, ChartConfig } from '../Charts/AdvancedChart';

interface PriceData {
  symbol: string;
  name: string;
  currentPrice: number;
  previousPrice: number;
  change: number;
  changePercent: number;
  volume: number;
  high24h: number;
  low24h: number;
  lastUpdated: Date;
  trend: 'up' | 'down' | 'flat';
  alerts?: PriceAlert[];
}

interface PriceAlert {
  id: string;
  symbol: string;
  type: 'above' | 'below' | 'change';
  threshold: number;
  message: string;
  isActive: boolean;
  triggeredAt?: Date;
}

interface Watchlist {
  id: string;
  name: string;
  symbols: string[];
  isDefault?: boolean;
}

const MOCK_PRICE_DATA: PriceData[] = [
  {
    symbol: 'BRENT',
    name: 'Brent Crude Oil',
    currentPrice: 85.42,
    previousPrice: 84.15,
    change: 1.27,
    changePercent: 1.51,
    volume: 125000,
    high24h: 86.10,
    low24h: 83.95,
    lastUpdated: new Date(),
    trend: 'up',
  },
  {
    symbol: 'WTI',
    name: 'WTI Crude Oil',
    currentPrice: 81.75,
    previousPrice: 82.03,
    change: -0.28,
    changePercent: -0.34,
    volume: 98000,
    high24h: 82.85,
    low24h: 81.20,
    lastUpdated: new Date(),
    trend: 'down',
  },
  {
    symbol: 'MGO',
    name: 'Marine Gas Oil',
    currentPrice: 890.50,
    previousPrice: 888.75,
    change: 1.75,
    changePercent: 0.20,
    volume: 45000,
    high24h: 892.00,
    low24h: 886.50,
    lastUpdated: new Date(),
    trend: 'up',
  },
  {
    symbol: 'ULSD',
    name: 'Ultra Low Sulfur Diesel',
    currentPrice: 2.45,
    previousPrice: 2.43,
    change: 0.02,
    changePercent: 0.82,
    volume: 67000,
    high24h: 2.47,
    low24h: 2.41,
    lastUpdated: new Date(),
    trend: 'up',
  },
];

const MOCK_CHART_DATA: { [key: string]: ChartDataPoint[] } = {
  BRENT: Array.from({ length: 100 }, (_, i) => ({
    timestamp: new Date(Date.now() - (100 - i) * 15 * 60 * 1000), // 15-minute intervals
    value: 85 + Math.sin(i / 10) * 3 + Math.random() * 2 - 1,
    volume: Math.floor(Math.random() * 5000) + 1000,
  })),
  WTI: Array.from({ length: 100 }, (_, i) => ({
    timestamp: new Date(Date.now() - (100 - i) * 15 * 60 * 1000),
    value: 82 + Math.cos(i / 8) * 2.5 + Math.random() * 1.5 - 0.75,
    volume: Math.floor(Math.random() * 4000) + 800,
  })),
};

export const RealTimePriceDashboard: React.FC = () => {
  const [priceData, setPriceData] = useState<PriceData[]>(MOCK_PRICE_DATA);
  const [selectedSymbol, setSelectedSymbol] = useState<string>('BRENT');
  const [timeRange, setTimeRange] = useState<string>('1D');
  const [isRealTimeEnabled, setIsRealTimeEnabled] = useState(true);
  const [showAlerts, setShowAlerts] = useState(false);
  const [alertDialog, setAlertDialog] = useState<{
    open: boolean;
    symbol?: string;
    alert?: Partial<PriceAlert>;
  }>({ open: false });
  const [watchlists] = useState<Watchlist[]>([
    { id: '1', name: 'Crude Oils', symbols: ['BRENT', 'WTI'], isDefault: true },
    { id: '2', name: 'Refined Products', symbols: ['MGO', 'ULSD'] },
  ]);
  const [activeWatchlist, setActiveWatchlist] = useState<string>('1');
  const [alerts, setAlerts] = useState<PriceAlert[]>([]);

  // Simulate real-time price updates
  useEffect(() => {
    if (!isRealTimeEnabled) return;

    const interval = setInterval(() => {
      setPriceData(prevData => 
        prevData.map(item => {
          const volatility = 0.002; // 0.2% volatility
          const randomChange = (Math.random() - 0.5) * 2 * volatility;
          const newPrice = item.currentPrice * (1 + randomChange);
          const change = newPrice - item.previousPrice;
          const changePercent = (change / item.previousPrice) * 100;
          
          return {
            ...item,
            currentPrice: Number(newPrice.toFixed(2)),
            change: Number(change.toFixed(2)),
            changePercent: Number(changePercent.toFixed(2)),
            lastUpdated: new Date(),
            trend: change > 0 ? 'up' : change < 0 ? 'down' : 'flat',
          };
        })
      );
    }, 5000); // Update every 5 seconds

    return () => clearInterval(interval);
  }, [isRealTimeEnabled]);

  const currentWatchlist = watchlists.find(w => w.id === activeWatchlist);
  const filteredPriceData = currentWatchlist 
    ? priceData.filter(item => currentWatchlist.symbols.includes(item.symbol))
    : priceData;

  const selectedPriceData = priceData.find(item => item.symbol === selectedSymbol);

  const chartConfig: ChartConfig = {
    type: 'line',
    title: `${selectedPriceData?.name || selectedSymbol} Price Chart`,
    yAxisLabel: 'Price (USD)',
    indicators: [],
    referenceLines: selectedPriceData ? [
      { value: selectedPriceData.high24h, label: '24h High', color: '#4caf50' },
      { value: selectedPriceData.low24h, label: '24h Low', color: '#f44336' },
    ] : [],
    enableZoom: true,
    showBrush: true,
    enableCrosshair: true,
  };

  const getTrendIcon = (trend: string) => {
    switch (trend) {
      case 'up': return <TrendingUp color="success" />;
      case 'down': return <TrendingDown color="error" />;
      default: return <TrendingFlat color="disabled" />;
    }
  };

  const getTrendColor = (change: number): 'success' | 'error' | 'default' => {
    if (change > 0) return 'success';
    if (change < 0) return 'error';
    return 'default';
  };

  const handleCreateAlert = () => {
    setAlertDialog({ open: true, symbol: selectedSymbol });
  };

  const handleSaveAlert = () => {
    if (alertDialog.alert && alertDialog.symbol) {
      const newAlert: PriceAlert = {
        id: Date.now().toString(),
        symbol: alertDialog.symbol,
        type: alertDialog.alert.type || 'above',
        threshold: alertDialog.alert.threshold || 0,
        message: alertDialog.alert.message || '',
        isActive: true,
      };
      setAlerts(prev => [...prev, newAlert]);
      setAlertDialog({ open: false });
    }
  };

  const activeAlerts = alerts.filter(alert => alert.isActive).length;

  return (
    <Box>
      <Grid container spacing={3}>
        {/* Header Controls */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Typography variant="h5" component="h1">
                  Real-Time Price Dashboard
                </Typography>
                
                <Box display="flex" alignItems="center" gap={2}>
                  {/* Watchlist Selector */}
                  <FormControl size="small" sx={{ minWidth: 150 }}>
                    <InputLabel>Watchlist</InputLabel>
                    <Select
                      value={activeWatchlist}
                      label="Watchlist"
                      onChange={(e) => setActiveWatchlist(e.target.value)}
                    >
                      {watchlists.map(watchlist => (
                        <MenuItem key={watchlist.id} value={watchlist.id}>
                          {watchlist.name}
                          {watchlist.isDefault && <Chip label="Default" size="small" sx={{ ml: 1 }} />}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  {/* Real-time Toggle */}
                  <FormControlLabel
                    control={
                      <Switch
                        checked={isRealTimeEnabled}
                        onChange={(e) => setIsRealTimeEnabled(e.target.checked)}
                        color="primary"
                      />
                    }
                    label="Real-time"
                  />

                  {/* Alerts Button */}
                  <Badge badgeContent={activeAlerts} color="error">
                    <Button
                      variant="outlined"
                      startIcon={activeAlerts > 0 ? <NotificationsActive /> : <Notifications />}
                      onClick={() => setShowAlerts(true)}
                    >
                      Alerts
                    </Button>
                  </Badge>

                  {/* Controls */}
                  <Button
                    variant="outlined"
                    startIcon={<Add />}
                    onClick={handleCreateAlert}
                  >
                    New Alert
                  </Button>

                  <IconButton>
                    <Settings />
                  </IconButton>

                  <IconButton>
                    <Refresh />
                  </IconButton>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Price Grid */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardHeader 
              title="Market Overview" 
              action={
                isRealTimeEnabled ? (
                  <Chip 
                    icon={<PlayArrow />} 
                    label="Live" 
                    color="success" 
                    size="small" 
                  />
                ) : (
                  <Chip 
                    icon={<Pause />} 
                    label="Paused" 
                    color="default" 
                    size="small" 
                  />
                )
              }
            />
            <CardContent sx={{ p: 0 }}>
              <List>
                {filteredPriceData.map((item) => (
                  <ListItem
                    key={item.symbol}
                    button
                    selected={selectedSymbol === item.symbol}
                    onClick={() => setSelectedSymbol(item.symbol)}
                    divider
                  >
                    <ListItemIcon>
                      <Avatar sx={{ bgcolor: selectedSymbol === item.symbol ? 'primary.main' : 'grey.300' }}>
                        {item.symbol[0]}
                      </Avatar>
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="subtitle2" fontWeight="bold">
                            {item.symbol}
                          </Typography>
                          <Box display="flex" alignItems="center" gap={0.5}>
                            {getTrendIcon(item.trend)}
                            <Typography 
                              variant="body2" 
                              color={getTrendColor(item.change)}
                              fontWeight="bold"
                            >
                              ${item.currentPrice.toFixed(2)}
                            </Typography>
                          </Box>
                        </Box>
                      }
                      secondary={
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="caption" color="text.secondary">
                            {item.name}
                          </Typography>
                          <Box display="flex" gap={1}>
                            <Typography 
                              variant="caption" 
                              color={getTrendColor(item.change)}
                            >
                              {item.change > 0 ? '+' : ''}{item.change.toFixed(2)}
                            </Typography>
                            <Typography 
                              variant="caption" 
                              color={getTrendColor(item.change)}
                            >
                              ({item.changePercent > 0 ? '+' : ''}{item.changePercent.toFixed(2)}%)
                            </Typography>
                          </Box>
                        </Box>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            </CardContent>
          </Card>

          {/* Selected Price Details */}
          {selectedPriceData && (
            <Card sx={{ mt: 2 }}>
              <CardHeader title={`${selectedPriceData.symbol} Details`} />
              <CardContent>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">24h High</Typography>
                    <Typography variant="body2" color="success.main" fontWeight="bold">
                      ${selectedPriceData.high24h.toFixed(2)}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">24h Low</Typography>
                    <Typography variant="body2" color="error.main" fontWeight="bold">
                      ${selectedPriceData.low24h.toFixed(2)}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">Volume</Typography>
                    <Typography variant="body2" fontWeight="bold">
                      {selectedPriceData.volume.toLocaleString()}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="caption" color="text.secondary">Last Updated</Typography>
                    <Typography variant="body2">
                      {format(selectedPriceData.lastUpdated, 'HH:mm:ss')}
                    </Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          )}
        </Grid>

        {/* Chart */}
        <Grid item xs={12} md={8}>
          <AdvancedChart
            data={MOCK_CHART_DATA[selectedSymbol] || []}
            config={chartConfig}
            height={500}
            timeRange={timeRange}
            onTimeRangeChange={setTimeRange}
            onRefresh={() => console.log('Refreshing chart data')}
          />
        </Grid>

        {/* Price Table */}
        <Grid item xs={12}>
          <Card>
            <CardHeader title="Detailed Price Information" />
            <CardContent>
              <TableContainer>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Symbol</TableCell>
                      <TableCell>Name</TableCell>
                      <TableCell align="right">Current Price</TableCell>
                      <TableCell align="right">Change</TableCell>
                      <TableCell align="right">Change %</TableCell>
                      <TableCell align="right">24h High</TableCell>
                      <TableCell align="right">24h Low</TableCell>
                      <TableCell align="right">Volume</TableCell>
                      <TableCell>Last Updated</TableCell>
                      <TableCell>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {filteredPriceData.map((item) => (
                      <TableRow 
                        key={item.symbol}
                        hover
                        selected={selectedSymbol === item.symbol}
                        onClick={() => setSelectedSymbol(item.symbol)}
                        sx={{ cursor: 'pointer' }}
                      >
                        <TableCell>
                          <Box display="flex" alignItems="center" gap={1}>
                            {getTrendIcon(item.trend)}
                            <Typography fontWeight="bold">{item.symbol}</Typography>
                          </Box>
                        </TableCell>
                        <TableCell>{item.name}</TableCell>
                        <TableCell align="right">
                          <Typography fontWeight="bold">
                            ${item.currentPrice.toFixed(2)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography color={getTrendColor(item.change)}>
                            {item.change > 0 ? '+' : ''}{item.change.toFixed(2)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography color={getTrendColor(item.change)}>
                            {item.changePercent > 0 ? '+' : ''}{item.changePercent.toFixed(2)}%
                          </Typography>
                        </TableCell>
                        <TableCell align="right" sx={{ color: 'success.main' }}>
                          ${item.high24h.toFixed(2)}
                        </TableCell>
                        <TableCell align="right" sx={{ color: 'error.main' }}>
                          ${item.low24h.toFixed(2)}
                        </TableCell>
                        <TableCell align="right">
                          {item.volume.toLocaleString()}
                        </TableCell>
                        <TableCell>
                          {format(item.lastUpdated, 'HH:mm:ss')}
                        </TableCell>
                        <TableCell>
                          <Button
                            size="small"
                            onClick={(e) => {
                              e.stopPropagation();
                              setAlertDialog({ open: true, symbol: item.symbol });
                            }}
                          >
                            Alert
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
      </Grid>

      {/* Create Alert Dialog */}
      <Dialog 
        open={alertDialog.open} 
        onClose={() => setAlertDialog({ open: false })}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create Price Alert</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Symbol"
                value={alertDialog.symbol || ''}
                disabled
              />
            </Grid>
            <Grid item xs={12}>
              <FormControl fullWidth>
                <InputLabel>Alert Type</InputLabel>
                <Select
                  value={alertDialog.alert?.type || 'above'}
                  label="Alert Type"
                  onChange={(e) => setAlertDialog(prev => ({
                    ...prev,
                    alert: { ...prev.alert, type: e.target.value as 'above' | 'below' | 'change' }
                  }))}
                >
                  <MenuItem value="above">Price Above</MenuItem>
                  <MenuItem value="below">Price Below</MenuItem>
                  <MenuItem value="change">Price Change %</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Threshold"
                type="number"
                value={alertDialog.alert?.threshold || ''}
                onChange={(e) => setAlertDialog(prev => ({
                  ...prev,
                  alert: { ...prev.alert, threshold: parseFloat(e.target.value) || 0 }
                }))}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Message"
                multiline
                rows={2}
                value={alertDialog.alert?.message || ''}
                onChange={(e) => setAlertDialog(prev => ({
                  ...prev,
                  alert: { ...prev.alert, message: e.target.value }
                }))}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAlertDialog({ open: false })}>
            Cancel
          </Button>
          <Button onClick={handleSaveAlert} variant="contained">
            Create Alert
          </Button>
        </DialogActions>
      </Dialog>

      {/* Alerts Dialog */}
      <Dialog 
        open={showAlerts} 
        onClose={() => setShowAlerts(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Price Alerts</DialogTitle>
        <DialogContent>
          {alerts.length > 0 ? (
            <List>
              {alerts.map((alert) => (
                <ListItem key={alert.id} divider>
                  <ListItemIcon>
                    {alert.isActive ? <CheckCircle color="success" /> : <Warning color="warning" />}
                  </ListItemIcon>
                  <ListItemText
                    primary={`${alert.symbol} ${alert.type} ${alert.threshold}`}
                    secondary={alert.message}
                  />
                  <IconButton size="small">
                    <Edit />
                  </IconButton>
                  <IconButton size="small" color="error">
                    <Delete />
                  </IconButton>
                </ListItem>
              ))}
            </List>
          ) : (
            <Alert severity="info">
              No price alerts configured. Create alerts to get notified of important price movements.
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowAlerts(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};