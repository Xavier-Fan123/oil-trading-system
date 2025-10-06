import React, { useEffect, useState } from 'react';
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
  Tooltip,
  Legend,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts';
import { Box, Card, CardContent, Typography, FormControl, InputLabel, Select, MenuItem, Switch, FormControlLabel } from '@mui/material';
import { format } from 'date-fns';

export type ChartType = 'line' | 'area' | 'bar';
export type TimeRange = '1H' | '4H' | '1D' | '7D' | '30D';

export interface ChartDataPoint {
  timestamp: string;
  value: number;
  volume?: number;
  high?: number;
  low?: number;
  open?: number;
  close?: number;
}

export interface RealTimeChartProps {
  title: string;
  data: ChartDataPoint[];
  chartType?: ChartType;
  timeRange?: TimeRange;
  onTimeRangeChange?: (range: TimeRange) => void;
  referenceLines?: Array<{ value: number; label: string; color: string }>;
  enableRealTime?: boolean;
  onRealTimeToggle?: (enabled: boolean) => void;
  showVolume?: boolean;
  color?: string;
  height?: number;
}

const RealTimeChart: React.FC<RealTimeChartProps> = ({
  title,
  data,
  chartType = 'line',
  timeRange = '1D',
  onTimeRangeChange,
  referenceLines = [],
  enableRealTime = false,
  onRealTimeToggle,
  showVolume = false,
  color = '#2563eb',
  height = 400,
}) => {
  const [localChartType, setLocalChartType] = useState<ChartType>(chartType);
  const [realTimeEnabled, setRealTimeEnabled] = useState(enableRealTime);

  const timeRangeOptions = [
    { value: '1H' as TimeRange, label: '1 Hour' },
    { value: '4H' as TimeRange, label: '4 Hours' },
    { value: '1D' as TimeRange, label: '1 Day' },
    { value: '7D' as TimeRange, label: '7 Days' },
    { value: '30D' as TimeRange, label: '30 Days' },
  ];

  const chartTypeOptions = [
    { value: 'line' as ChartType, label: 'Line Chart' },
    { value: 'area' as ChartType, label: 'Area Chart' },
    { value: 'bar' as ChartType, label: 'Bar Chart' },
  ];

  const formatXAxisTick = (tickItem: string) => {
    const date = new Date(tickItem);
    switch (timeRange) {
      case '1H':
      case '4H':
        return format(date, 'HH:mm');
      case '1D':
        return format(date, 'HH:mm');
      case '7D':
        return format(date, 'MMM dd');
      case '30D':
        return format(date, 'MMM dd');
      default:
        return format(date, 'MMM dd');
    }
  };

  const formatTooltipLabel = (label: string) => {
    return format(new Date(label), 'MMM dd, yyyy HH:mm:ss');
  };

  const formatTooltipValue = (value: number, name: string) => {
    if (name === 'value' || name === 'close' || name === 'open' || name === 'high' || name === 'low') {
      return [new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value), name];
    }
    if (name === 'volume') {
      return [new Intl.NumberFormat('en-US', { notation: 'compact' }).format(value), name];
    }
    return [value, name];
  };

  const handleRealTimeToggle = (checked: boolean) => {
    setRealTimeEnabled(checked);
    onRealTimeToggle?.(checked);
  };

  const renderChart = () => {
    const commonProps = {
      data,
      margin: { top: 5, right: 30, left: 20, bottom: 5 },
    };

    const commonChildElements = (
      <>
        <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
        <XAxis
          dataKey="timestamp"
          tickFormatter={formatXAxisTick}
          stroke="#666"
          fontSize={12}
        />
        <YAxis
          stroke="#666"
          fontSize={12}
          tickFormatter={(value) => new Intl.NumberFormat('en-US', { 
            notation: 'compact',
            style: 'currency',
            currency: 'USD'
          }).format(value)}
        />
        <Tooltip
          labelFormatter={formatTooltipLabel}
          formatter={formatTooltipValue}
          contentStyle={{
            backgroundColor: 'rgba(255, 255, 255, 0.95)',
            border: '1px solid #ccc',
            borderRadius: '4px',
            boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
          }}
        />
        <Legend />
        {referenceLines.map((line, index) => (
          <ReferenceLine
            key={index}
            y={line.value}
            stroke={line.color}
            strokeDasharray="5 5"
            label={line.label}
          />
        ))}
      </>
    );

    switch (localChartType) {
      case 'area':
        return (
          <AreaChart {...commonProps}>
            {commonChildElements}
            <Area
              type="monotone"
              dataKey="value"
              stroke={color}
              fill={color}
              fillOpacity={0.3}
              strokeWidth={2}
            />
            {showVolume && (
              <Area
                type="monotone"
                dataKey="volume"
                stroke="#8884d8"
                fill="#8884d8"
                fillOpacity={0.1}
                strokeWidth={1}
                yAxisId="volume"
              />
            )}
          </AreaChart>
        );
      case 'bar':
        return (
          <BarChart {...commonProps}>
            {commonChildElements}
            <Bar dataKey="value" fill={color} />
            {showVolume && (
              <Bar dataKey="volume" fill="#8884d8" yAxisId="volume" />
            )}
          </BarChart>
        );
      case 'line':
      default:
        return (
          <LineChart {...commonProps}>
            {commonChildElements}
            <Line
              type="monotone"
              dataKey="value"
              stroke={color}
              strokeWidth={2}
              dot={false}
              activeDot={{ r: 4, fill: color }}
            />
            {data[0]?.high && (
              <>
                <Line
                  type="monotone"
                  dataKey="high"
                  stroke="#22c55e"
                  strokeWidth={1}
                  strokeDasharray="3 3"
                  dot={false}
                />
                <Line
                  type="monotone"
                  dataKey="low"
                  stroke="#ef4444"
                  strokeWidth={1}
                  strokeDasharray="3 3"
                  dot={false}
                />
              </>
            )}
            {showVolume && (
              <Line
                type="monotone"
                dataKey="volume"
                stroke="#8884d8"
                strokeWidth={1}
                yAxisId="volume"
                dot={false}
              />
            )}
          </LineChart>
        );
    }
  };

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6" component="h3">
            {title}
            {realTimeEnabled && (
              <Typography component="span" color="success.main" sx={{ ml: 1 }}>
                • Live
              </Typography>
            )}
          </Typography>
          <Box display="flex" gap={1} alignItems="center">
            {onRealTimeToggle && (
              <FormControlLabel
                control={
                  <Switch
                    checked={realTimeEnabled}
                    onChange={(e) => handleRealTimeToggle(e.target.checked)}
                    color="primary"
                    size="small"
                  />
                }
                label="Live"
                labelPlacement="start"
                sx={{ mr: 2 }}
              />
            )}
            <FormControl size="small" sx={{ minWidth: 100 }}>
              <InputLabel>Type</InputLabel>
              <Select
                value={localChartType}
                onChange={(e) => setLocalChartType(e.target.value as ChartType)}
                label="Type"
              >
                {chartTypeOptions.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    {option.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            {onTimeRangeChange && (
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <InputLabel>Range</InputLabel>
                <Select
                  value={timeRange}
                  onChange={(e) => onTimeRangeChange(e.target.value as TimeRange)}
                  label="Range"
                >
                  {timeRangeOptions.map((option) => (
                    <MenuItem key={option.value} value={option.value}>
                      {option.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          </Box>
        </Box>
        
        <Box height={height}>
          <ResponsiveContainer width="100%" height="100%">
            {renderChart()}
          </ResponsiveContainer>
        </Box>
        
        {data.length > 0 && (
          <Box mt={2} display="flex" justifyContent="space-between" alignItems="center">
            <Typography variant="body2" color="textSecondary">
              Last Updated: {format(new Date(data[data.length - 1]?.timestamp || new Date()), 'MMM dd, yyyy HH:mm:ss')}
            </Typography>
            <Typography variant="body2" color="textSecondary">
              {data.length} data points
            </Typography>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

// Hook for generating mock real-time data
export const useRealTimeData = (
  initialValue: number = 75.50,
  volatility: number = 0.02,
  intervalMs: number = 5000,
  enabled: boolean = false
) => {
  const [data, setData] = useState<ChartDataPoint[]>(() => {
    const now = new Date();
    const initialData: ChartDataPoint[] = [];
    for (let i = 0; i < 50; i++) {
      const timestamp = new Date(now.getTime() - (50 - i) * intervalMs);
      const value = initialValue + (Math.random() - 0.5) * initialValue * volatility;
      initialData.push({
        timestamp: timestamp.toISOString(),
        value: Math.round(value * 100) / 100,
        volume: Math.floor(Math.random() * 1000000) + 100000,
      });
    }
    return initialData;
  });

  useEffect(() => {
    if (!enabled) return;

    const interval = setInterval(() => {
      setData(prevData => {
        const lastValue = prevData[prevData.length - 1]?.value || initialValue;
        const change = (Math.random() - 0.5) * lastValue * volatility;
        const newValue = Math.max(0, lastValue + change);
        
        const newDataPoint: ChartDataPoint = {
          timestamp: new Date().toISOString(),
          value: Math.round(newValue * 100) / 100,
          volume: Math.floor(Math.random() * 1000000) + 100000,
        };

        // Keep only last 100 data points
        const newData = [...prevData.slice(-99), newDataPoint];
        return newData;
      });
    }, intervalMs);

    return () => clearInterval(interval);
  }, [enabled, intervalMs, initialValue, volatility]);

  return data;
};

export default RealTimeChart;