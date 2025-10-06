import { useState, useMemo } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  ButtonGroup,
  IconButton,
  Menu,
  MenuItem,
  Chip,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Switch,
  FormControlLabel,
  Grid,
  TextField,
} from '@mui/material';
import {
  Settings,
  Fullscreen,
  Download,
  Refresh,
  TrendingUp,
  ShowChart,
  Timeline,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ResponsiveContainer,
  Brush,
  ReferenceLine,
} from 'recharts';
import { format, subDays } from 'date-fns';

export interface ChartDataPoint {
  timestamp: Date;
  value: number;
  open?: number;
  high?: number;
  low?: number;
  close?: number;
  volume?: number;
  [key: string]: any;
}

export interface TechnicalIndicator {
  key: string;
  name: string;
  enabled: boolean;
  color: string;
  parameters?: { [key: string]: number };
}

export interface ChartConfig {
  type: 'line' | 'area' | 'bar' | 'candlestick';
  title: string;
  yAxisLabel?: string;
  indicators?: TechnicalIndicator[];
  referenceLines?: { value: number; label: string; color: string }[];
  enableZoom?: boolean;
  showBrush?: boolean;
  enableCrosshair?: boolean;
  theme?: 'light' | 'dark';
}

interface AdvancedChartProps {
  data: ChartDataPoint[];
  config: ChartConfig;
  height?: number;
  timeRange?: string;
  onTimeRangeChange?: (range: string) => void;
  onRefresh?: () => void;
}

const TIME_RANGES = [
  { key: '1D', label: '1 Day', days: 1 },
  { key: '1W', label: '1 Week', days: 7 },
  { key: '1M', label: '1 Month', days: 30 },
  { key: '3M', label: '3 Months', days: 90 },
  { key: '6M', label: '6 Months', days: 180 },
  { key: '1Y', label: '1 Year', days: 365 },
  { key: 'ALL', label: 'All Time', days: null },
];

const DEFAULT_INDICATORS: TechnicalIndicator[] = [
  { key: 'sma20', name: 'SMA (20)', enabled: false, color: '#ff7300', parameters: { period: 20 } },
  { key: 'sma50', name: 'SMA (50)', enabled: false, color: '#8884d8', parameters: { period: 50 } },
  { key: 'ema12', name: 'EMA (12)', enabled: false, color: '#82ca9d', parameters: { period: 12 } },
  { key: 'bollinger', name: 'Bollinger Bands', enabled: false, color: '#ffc658', parameters: { period: 20, deviation: 2 } },
  { key: 'rsi', name: 'RSI (14)', enabled: false, color: '#ff0000', parameters: { period: 14 } },
  { key: 'macd', name: 'MACD', enabled: false, color: '#00ff00', parameters: { fast: 12, slow: 26, signal: 9 } },
];

// Simple Moving Average calculation
const calculateSMA = (data: ChartDataPoint[], period: number): number[] => {
  const sma: number[] = [];
  for (let i = 0; i < data.length; i++) {
    if (i < period - 1) {
      sma.push(NaN);
    } else {
      const sum = data.slice(i - period + 1, i + 1).reduce((acc, point) => acc + point.value, 0);
      sma.push(sum / period);
    }
  }
  return sma;
};

// Exponential Moving Average calculation
const calculateEMA = (data: ChartDataPoint[], period: number): number[] => {
  const ema: number[] = [];
  const multiplier = 2 / (period + 1);
  
  for (let i = 0; i < data.length; i++) {
    if (i === 0) {
      ema.push(data[i].value);
    } else {
      ema.push((data[i].value - ema[i - 1]) * multiplier + ema[i - 1]);
    }
  }
  return ema;
};

// RSI calculation
const calculateRSI = (data: ChartDataPoint[], period: number): number[] => {
  const rsi: number[] = [];
  const gains: number[] = [];
  const losses: number[] = [];
  
  for (let i = 1; i < data.length; i++) {
    const change = data[i].value - data[i - 1].value;
    gains.push(change > 0 ? change : 0);
    losses.push(change < 0 ? Math.abs(change) : 0);
  }
  
  for (let i = 0; i < gains.length; i++) {
    if (i < period - 1) {
      rsi.push(NaN);
    } else {
      const avgGain = gains.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period;
      const avgLoss = losses.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period;
      const rs = avgGain / (avgLoss || 0.0001);
      rsi.push(100 - (100 / (1 + rs)));
    }
  }
  
  return [NaN, ...rsi]; // Add NaN for first data point since we start from index 1
};

export const AdvancedChart: React.FC<AdvancedChartProps> = ({
  data,
  config,
  height = 400,
  timeRange = '1M',
  onTimeRangeChange,
  onRefresh,
}) => {
  const [settingsAnchor, setSettingsAnchor] = useState<null | HTMLElement>(null);
  const [indicatorsDialog, setIndicatorsDialog] = useState(false);
  const [enabledIndicators, setEnabledIndicators] = useState<TechnicalIndicator[]>(DEFAULT_INDICATORS);
  // Zoom functionality disabled for now
  // const [zoomDomain, setZoomDomain] = useState<{ startIndex?: number; endIndex?: number }>({});

  // Filter data based on time range
  const filteredData = useMemo(() => {
    if (timeRange === 'ALL') return data;
    
    const range = TIME_RANGES.find(r => r.key === timeRange);
    if (!range || !range.days) return data;
    
    const cutoffDate = subDays(new Date(), range.days);
    return data.filter(point => point.timestamp >= cutoffDate);
  }, [data, timeRange]);

  // Calculate technical indicators
  const enrichedData = useMemo(() => {
    const enriched = [...filteredData];
    
    enabledIndicators.forEach(indicator => {
      if (!indicator.enabled) return;
      
      switch (indicator.key) {
        case 'sma20':
        case 'sma50':
          const period = indicator.parameters?.period || 20;
          const smaValues = calculateSMA(filteredData, period);
          smaValues.forEach((value, index) => {
            if (enriched[index]) {
              enriched[index][indicator.key] = value;
            }
          });
          break;
          
        case 'ema12':
          const emaPeriod = indicator.parameters?.period || 12;
          const emaValues = calculateEMA(filteredData, emaPeriod);
          emaValues.forEach((value, index) => {
            if (enriched[index]) {
              enriched[index][indicator.key] = value;
            }
          });
          break;
          
        case 'rsi':
          const rsiPeriod = indicator.parameters?.period || 14;
          const rsiValues = calculateRSI(filteredData, rsiPeriod);
          rsiValues.forEach((value, index) => {
            if (enriched[index]) {
              enriched[index][indicator.key] = value;
            }
          });
          break;
      }
    });
    
    return enriched;
  }, [filteredData, enabledIndicators]);

  const handleIndicatorToggle = (indicatorKey: string) => {
    setEnabledIndicators(prev => 
      prev.map(indicator => 
        indicator.key === indicatorKey 
          ? { ...indicator, enabled: !indicator.enabled }
          : indicator
      )
    );
  };

  const renderChart = () => {
    const commonProps = {
      data: enrichedData,
      margin: { top: 20, right: 30, left: 20, bottom: 5 },
    };

    switch (config.type) {
      case 'area':
        return (
          <AreaChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis label={{ value: config.yAxisLabel, angle: -90, position: 'insideLeft' }} />
            <RechartsTooltip 
              labelFormatter={(value) => format(new Date(value), 'MMM dd, yyyy HH:mm')}
              formatter={(value: number) => [value.toFixed(2), 'Price']}
            />
            <Legend />
            
            <Area
              type="monotone"
              dataKey="value"
              stroke="#8884d8"
              fill="#8884d8"
              fillOpacity={0.3}
              name="Price"
            />
            
            {/* Technical Indicators */}
            {enabledIndicators.filter(i => i.enabled).map(indicator => (
              <Area
                key={indicator.key}
                type="monotone"
                dataKey={indicator.key}
                stroke={indicator.color}
                fill="none"
                name={indicator.name}
                strokeWidth={2}
              />
            ))}
            
            {/* Reference Lines */}
            {config.referenceLines?.map((line, index) => (
              <ReferenceLine
                key={index}
                y={line.value}
                stroke={line.color}
                strokeDasharray="3 3"
                label={line.label}
              />
            ))}
            
            {config.showBrush && (
              <Brush
                dataKey="timestamp"
                height={30}
                stroke="#8884d8"
                tickFormatter={(value) => format(new Date(value), 'MMM dd')}
              />
            )}
          </AreaChart>
        );

      case 'bar':
        return (
          <BarChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis label={{ value: config.yAxisLabel, angle: -90, position: 'insideLeft' }} />
            <RechartsTooltip 
              labelFormatter={(value) => format(new Date(value), 'MMM dd, yyyy HH:mm')}
              formatter={(value: number) => [value.toFixed(2), 'Price']}
            />
            <Legend />
            
            <Bar dataKey="value" fill="#8884d8" name="Price" />
            
            {config.referenceLines?.map((line, index) => (
              <ReferenceLine
                key={index}
                y={line.value}
                stroke={line.color}
                strokeDasharray="3 3"
                label={line.label}
              />
            ))}
          </BarChart>
        );

      default: // line chart
        return (
          <LineChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={(value) => format(new Date(value), 'MMM dd')}
            />
            <YAxis label={{ value: config.yAxisLabel, angle: -90, position: 'insideLeft' }} />
            <RechartsTooltip 
              labelFormatter={(value) => format(new Date(value), 'MMM dd, yyyy HH:mm')}
              formatter={(value: number) => [value?.toFixed(2) || 'N/A', 'Price']}
            />
            <Legend />
            
            <Line
              type="monotone"
              dataKey="value"
              stroke="#8884d8"
              strokeWidth={2}
              dot={false}
              name="Price"
            />
            
            {/* Technical Indicators */}
            {enabledIndicators.filter(i => i.enabled).map(indicator => (
              <Line
                key={indicator.key}
                type="monotone"
                dataKey={indicator.key}
                stroke={indicator.color}
                strokeWidth={2}
                dot={false}
                name={indicator.name}
                connectNulls={false}
              />
            ))}
            
            {/* Reference Lines */}
            {config.referenceLines?.map((line, index) => (
              <ReferenceLine
                key={index}
                y={line.value}
                stroke={line.color}
                strokeDasharray="3 3"
                label={line.label}
              />
            ))}
            
            {config.showBrush && (
              <Brush
                dataKey="timestamp"
                height={30}
                stroke="#8884d8"
                tickFormatter={(value) => format(new Date(value), 'MMM dd')}
              />
            )}
          </LineChart>
        );
    }
  };

  return (
    <Paper sx={{ p: 2 }}>
      {/* Chart Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6">{config.title}</Typography>
        
        <Box display="flex" alignItems="center" gap={1}>
          {/* Time Range Selector */}
          <ButtonGroup size="small" variant="outlined">
            {TIME_RANGES.map((range) => (
              <Button
                key={range.key}
                variant={timeRange === range.key ? 'contained' : 'outlined'}
                onClick={() => onTimeRangeChange?.(range.key)}
              >
                {range.label}
              </Button>
            ))}
          </ButtonGroup>
          
          {/* Indicators Chips */}
          <Box display="flex" gap={0.5}>
            {enabledIndicators.filter(i => i.enabled).map(indicator => (
              <Chip
                key={indicator.key}
                label={indicator.name}
                size="small"
                color="primary"
                variant="outlined"
                sx={{ borderColor: indicator.color, color: indicator.color }}
              />
            ))}
          </Box>
          
          {/* Chart Controls */}
          <Tooltip title="Technical Indicators">
            <IconButton size="small" onClick={() => setIndicatorsDialog(true)}>
              <TrendingUp />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Chart Settings">
            <IconButton size="small" onClick={(e) => setSettingsAnchor(e.currentTarget)}>
              <Settings />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Refresh Data">
            <IconButton size="small" onClick={onRefresh}>
              <Refresh />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Download Chart">
            <IconButton size="small">
              <Download />
            </IconButton>
          </Tooltip>
          
          <Tooltip title="Fullscreen">
            <IconButton size="small">
              <Fullscreen />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Chart */}
      <Box height={height}>
        <ResponsiveContainer width="100%" height="100%">
          {renderChart()}
        </ResponsiveContainer>
      </Box>

      {/* Settings Menu */}
      <Menu
        anchorEl={settingsAnchor}
        open={Boolean(settingsAnchor)}
        onClose={() => setSettingsAnchor(null)}
      >
        <MenuItem onClick={() => setIndicatorsDialog(true)}>
          <TrendingUp sx={{ mr: 1 }} />
          Technical Indicators
        </MenuItem>
        <MenuItem>
          <ShowChart sx={{ mr: 1 }} />
          Chart Type
        </MenuItem>
        <MenuItem>
          <Timeline sx={{ mr: 1 }} />
          Time Intervals
        </MenuItem>
      </Menu>

      {/* Technical Indicators Dialog */}
      <Dialog 
        open={indicatorsDialog} 
        onClose={() => setIndicatorsDialog(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Technical Indicators</DialogTitle>
        <DialogContent>
          <Grid container spacing={2}>
            {enabledIndicators.map((indicator) => (
              <Grid item xs={12} sm={6} key={indicator.key}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={indicator.enabled}
                      onChange={() => handleIndicatorToggle(indicator.key)}
                      color="primary"
                    />
                  }
                  label={
                    <Box display="flex" alignItems="center" gap={1}>
                      <Box
                        sx={{
                          width: 12,
                          height: 12,
                          backgroundColor: indicator.color,
                          borderRadius: '50%',
                        }}
                      />
                      {indicator.name}
                    </Box>
                  }
                />
                {indicator.enabled && indicator.parameters && (
                  <Box sx={{ ml: 4, mt: 1 }}>
                    {Object.entries(indicator.parameters).map(([key, value]) => (
                      <TextField
                        key={key}
                        label={key}
                        type="number"
                        size="small"
                        value={value}
                        sx={{ mr: 1, width: 80 }}
                        InputProps={{ inputProps: { min: 1, max: 200 } }}
                      />
                    ))}
                  </Box>
                )}
              </Grid>
            ))}
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIndicatorsDialog(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
};