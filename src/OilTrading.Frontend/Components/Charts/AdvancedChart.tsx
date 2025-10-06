import React, { useState, useEffect, useMemo } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  AreaChart,
  Area,
  BarChart,
  Bar,
  CandlestickChart,
  Brush,
  ReferenceLine,
  ReferenceArea,
  ScatterChart,
  Scatter
} from 'recharts';
import { format, parseISO, subDays, subMonths, subYears } from 'date-fns';

// 图表类型定义
export type ChartType = 'line' | 'area' | 'bar' | 'candlestick' | 'scatter' | 'combo';
export type TimeRange = '1D' | '1W' | '1M' | '3M' | '6M' | '1Y' | '2Y' | 'ALL';

// 技术指标类型
export type TechnicalIndicator = 'SMA' | 'EMA' | 'MACD' | 'RSI' | 'BB' | 'VOLUME';

// 数据点接口
export interface ChartDataPoint {
  timestamp: string;
  date: Date;
  open?: number;
  high?: number;
  low?: number;
  close: number;
  volume?: number;
  value?: number;
  [key: string]: any;
}

// 图表配置接口
export interface ChartConfig {
  type: ChartType;
  title: string;
  xAxisLabel?: string;
  yAxisLabel?: string;
  showGrid?: boolean;
  showTooltip?: boolean;
  showLegend?: boolean;
  showBrush?: boolean;
  enableZoom?: boolean;
  indicators?: TechnicalIndicator[];
  referenceLines?: ReferenceLine[];
  height?: number;
  colors?: string[];
}

// 参考线接口
export interface ReferenceLine {
  value: number;
  label: string;
  color: string;
  strokeDasharray?: string;
}

// 组件属性接口
export interface AdvancedChartProps {
  data: ChartDataPoint[];
  config: ChartConfig;
  timeRange?: TimeRange;
  onTimeRangeChange?: (range: TimeRange) => void;
  onDataPointClick?: (dataPoint: ChartDataPoint) => void;
  loading?: boolean;
  error?: string;
}

// 技术指标计算函数
class TechnicalAnalysis {
  // 简单移动平均线
  static calculateSMA(data: ChartDataPoint[], period: number = 20): ChartDataPoint[] {
    return data.map((point, index) => {
      if (index < period - 1) {
        return { ...point, [`SMA${period}`]: null };
      }
      
      const sum = data.slice(index - period + 1, index + 1)
        .reduce((acc, p) => acc + (p.close || p.value || 0), 0);
      
      return { ...point, [`SMA${period}`]: sum / period };
    });
  }

  // 指数移动平均线
  static calculateEMA(data: ChartDataPoint[], period: number = 20): ChartDataPoint[] {
    const multiplier = 2 / (period + 1);
    let ema = data[0]?.close || data[0]?.value || 0;
    
    return data.map((point, index) => {
      if (index === 0) {
        ema = point.close || point.value || 0;
      } else {
        ema = ((point.close || point.value || 0) - ema) * multiplier + ema;
      }
      
      return { ...point, [`EMA${period}`]: ema };
    });
  }

  // RSI 计算
  static calculateRSI(data: ChartDataPoint[], period: number = 14): ChartDataPoint[] {
    const changes = data.map((point, index) => {
      if (index === 0) return 0;
      const current = point.close || point.value || 0;
      const previous = data[index - 1].close || data[index - 1].value || 0;
      return current - previous;
    });

    return data.map((point, index) => {
      if (index < period) {
        return { ...point, RSI: null };
      }

      const gains = changes.slice(index - period + 1, index + 1)
        .filter(change => change > 0)
        .reduce((sum, gain) => sum + gain, 0) / period;
      
      const losses = Math.abs(changes.slice(index - period + 1, index + 1)
        .filter(change => change < 0)
        .reduce((sum, loss) => sum + loss, 0)) / period;

      const rs = gains / (losses || 1);
      const rsi = 100 - (100 / (1 + rs));

      return { ...point, RSI: rsi };
    });
  }

  // 布林带计算
  static calculateBollingerBands(data: ChartDataPoint[], period: number = 20, stdDev: number = 2): ChartDataPoint[] {
    return data.map((point, index) => {
      if (index < period - 1) {
        return { ...point, BB_Upper: null, BB_Middle: null, BB_Lower: null };
      }

      const slice = data.slice(index - period + 1, index + 1);
      const values = slice.map(p => p.close || p.value || 0);
      const mean = values.reduce((sum, val) => sum + val, 0) / period;
      const variance = values.reduce((sum, val) => sum + Math.pow(val - mean, 2), 0) / period;
      const standardDeviation = Math.sqrt(variance);

      return {
        ...point,
        BB_Upper: mean + (stdDev * standardDeviation),
        BB_Middle: mean,
        BB_Lower: mean - (stdDev * standardDeviation)
      };
    });
  }
}

// 主要图表组件
export const AdvancedChart: React.FC<AdvancedChartProps> = ({
  data,
  config,
  timeRange = '1M',
  onTimeRangeChange,
  onDataPointClick,
  loading = false,
  error
}) => {
  const [selectedRange, setSelectedRange] = useState<TimeRange>(timeRange);
  const [zoomDomain, setZoomDomain] = useState<[number, number] | null>(null);
  const [crosshairData, setCrosshairData] = useState<any>(null);

  // 时间范围按钮
  const timeRanges: TimeRange[] = ['1D', '1W', '1M', '3M', '6M', '1Y', '2Y', 'ALL'];

  // 根据时间范围过滤数据
  const filteredData = useMemo(() => {
    if (!data || data.length === 0) return [];

    let startDate: Date;
    const now = new Date();

    switch (selectedRange) {
      case '1D':
        startDate = subDays(now, 1);
        break;
      case '1W':
        startDate = subDays(now, 7);
        break;
      case '1M':
        startDate = subMonths(now, 1);
        break;
      case '3M':
        startDate = subMonths(now, 3);
        break;
      case '6M':
        startDate = subMonths(now, 6);
        break;
      case '1Y':
        startDate = subYears(now, 1);
        break;
      case '2Y':
        startDate = subYears(now, 2);
        break;
      default:
        return data;
    }

    return data.filter(point => point.date >= startDate);
  }, [data, selectedRange]);

  // 应用技术指标
  const enhancedData = useMemo(() => {
    if (!config.indicators || config.indicators.length === 0) {
      return filteredData;
    }

    let result = [...filteredData];

    config.indicators.forEach(indicator => {
      switch (indicator) {
        case 'SMA':
          result = TechnicalAnalysis.calculateSMA(result, 20);
          break;
        case 'EMA':
          result = TechnicalAnalysis.calculateEMA(result, 20);
          break;
        case 'RSI':
          result = TechnicalAnalysis.calculateRSI(result);
          break;
        case 'BB':
          result = TechnicalAnalysis.calculateBollingerBands(result);
          break;
      }
    });

    return result;
  }, [filteredData, config.indicators]);

  // 处理时间范围变化
  const handleTimeRangeChange = (range: TimeRange) => {
    setSelectedRange(range);
    onTimeRangeChange?.(range);
    setZoomDomain(null); // 重置缩放
  };

  // 处理数据点点击
  const handleDataPointClick = (data: any) => {
    onDataPointClick?.(data);
  };

  // 自定义工具提示
  const CustomTooltip = ({ active, payload, label }: any) => {
    if (!active || !payload || !payload.length) return null;

    const data = payload[0].payload;
    const date = format(parseISO(label), 'yyyy-MM-dd HH:mm');

    return (
      <div className="bg-white p-3 border border-gray-300 rounded shadow-lg">
        <p className="font-semibold text-gray-800">{date}</p>
        {payload.map((entry: any, index: number) => (
          <p key={index} style={{ color: entry.color }}>
            {entry.name}: {typeof entry.value === 'number' ? entry.value.toFixed(2) : entry.value}
          </p>
        ))}
      </div>
    );
  };

  // 渲染不同类型的图表
  const renderChart = () => {
    const commonProps = {
      data: enhancedData,
      margin: { top: 20, right: 30, left: 20, bottom: 5 },
      onMouseMove: (e: any) => setCrosshairData(e)
    };

    switch (config.type) {
      case 'line':
        return (
          <LineChart {...commonProps}>
            {config.showGrid && <CartesianGrid strokeDasharray="3 3" />}
            <XAxis 
              dataKey="timestamp" 
              tickFormatter={(value) => format(parseISO(value), 'MM/dd')}
            />
            <YAxis domain={['dataMin - 5', 'dataMax + 5']} />
            {config.showTooltip && <Tooltip content={<CustomTooltip />} />}
            {config.showLegend && <Legend />}
            
            <Line 
              type="monotone" 
              dataKey="close" 
              stroke="#8884d8" 
              strokeWidth={2}
              dot={false}
              activeDot={{ r: 6 }}
            />
            
            {/* 技术指标线 */}
            {config.indicators?.includes('SMA') && (
              <Line type="monotone" dataKey="SMA20" stroke="#ff7300" strokeWidth={1} dot={false} />
            )}
            {config.indicators?.includes('EMA') && (
              <Line type="monotone" dataKey="EMA20" stroke="#00ff00" strokeWidth={1} dot={false} />
            )}
            
            {/* 参考线 */}
            {config.referenceLines?.map((refLine, index) => (
              <ReferenceLine 
                key={index}
                y={refLine.value} 
                stroke={refLine.color}
                strokeDasharray={refLine.strokeDasharray || "5 5"}
                label={refLine.label}
              />
            ))}
            
            {config.showBrush && <Brush />}
          </LineChart>
        );

      case 'area':
        return (
          <AreaChart {...commonProps}>
            {config.showGrid && <CartesianGrid strokeDasharray="3 3" />}
            <XAxis dataKey="timestamp" tickFormatter={(value) => format(parseISO(value), 'MM/dd')} />
            <YAxis />
            {config.showTooltip && <Tooltip content={<CustomTooltip />} />}
            {config.showLegend && <Legend />}
            
            <defs>
              <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#8884d8" stopOpacity={0.8}/>
                <stop offset="95%" stopColor="#8884d8" stopOpacity={0.1}/>
              </linearGradient>
            </defs>
            
            <Area 
              type="monotone" 
              dataKey="close" 
              stroke="#8884d8" 
              fillOpacity={1} 
              fill="url(#colorValue)" 
            />
            
            {config.showBrush && <Brush />}
          </AreaChart>
        );

      case 'bar':
        return (
          <BarChart {...commonProps}>
            {config.showGrid && <CartesianGrid strokeDasharray="3 3" />}
            <XAxis dataKey="timestamp" tickFormatter={(value) => format(parseISO(value), 'MM/dd')} />
            <YAxis />
            {config.showTooltip && <Tooltip content={<CustomTooltip />} />}
            {config.showLegend && <Legend />}
            
            <Bar dataKey="volume" fill="#8884d8" />
            {config.showBrush && <Brush />}
          </BarChart>
        );

      case 'scatter':
        return (
          <ScatterChart {...commonProps}>
            {config.showGrid && <CartesianGrid strokeDasharray="3 3" />}
            <XAxis dataKey="volume" type="number" />
            <YAxis dataKey="close" type="number" />
            {config.showTooltip && <Tooltip content={<CustomTooltip />} />}
            {config.showLegend && <Legend />}
            
            <Scatter name="Price vs Volume" data={enhancedData} fill="#8884d8" />
          </ScatterChart>
        );

      default:
        return null;
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-red-500 text-center">
          <p className="text-xl font-semibold">Error loading chart</p>
          <p className="text-sm">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full bg-white rounded-lg shadow-lg p-6">
      {/* 图表标题和控制 */}
      <div className="flex justify-between items-center mb-6">
        <h3 className="text-xl font-semibold text-gray-800">{config.title}</h3>
        
        {/* 时间范围选择器 */}
        <div className="flex space-x-2">
          {timeRanges.map(range => (
            <button
              key={range}
              onClick={() => handleTimeRangeChange(range)}
              className={`px-3 py-1 text-sm font-medium rounded ${
                selectedRange === range
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
              }`}
            >
              {range}
            </button>
          ))}
        </div>
      </div>

      {/* 技术指标显示 */}
      {config.indicators && config.indicators.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-4">
          {config.indicators.map(indicator => (
            <span
              key={indicator}
              className="px-2 py-1 bg-blue-100 text-blue-800 text-xs font-medium rounded"
            >
              {indicator}
            </span>
          ))}
        </div>
      )}

      {/* 主图表区域 */}
      <div style={{ width: '100%', height: config.height || 400 }}>
        <ResponsiveContainer width="100%" height="100%">
          {renderChart()}
        </ResponsiveContainer>
      </div>

      {/* 图表信息面板 */}
      {crosshairData && (
        <div className="mt-4 p-3 bg-gray-50 rounded">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <span className="text-gray-600">Date:</span>
              <span className="ml-2 font-medium">
                {crosshairData.activeLabel && format(parseISO(crosshairData.activeLabel), 'yyyy-MM-dd')}
              </span>
            </div>
            <div>
              <span className="text-gray-600">Value:</span>
              <span className="ml-2 font-medium">
                {crosshairData.activePayload?.[0]?.value?.toFixed(2)}
              </span>
            </div>
            {crosshairData.activePayload?.[0]?.payload?.volume && (
              <div>
                <span className="text-gray-600">Volume:</span>
                <span className="ml-2 font-medium">
                  {crosshairData.activePayload[0].payload.volume.toLocaleString()}
                </span>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

// 预设图表配置
export const ChartPresets = {
  priceChart: (): ChartConfig => ({
    type: 'line',
    title: 'Price Chart',
    xAxisLabel: 'Time',
    yAxisLabel: 'Price (USD)',
    showGrid: true,
    showTooltip: true,
    showLegend: true,
    showBrush: true,
    enableZoom: true,
    indicators: ['SMA', 'EMA'],
    height: 400
  }),

  volumeChart: (): ChartConfig => ({
    type: 'bar',
    title: 'Volume Chart',
    xAxisLabel: 'Time',
    yAxisLabel: 'Volume',
    showGrid: true,
    showTooltip: true,
    showLegend: false,
    height: 200
  }),

  rsiChart: (): ChartConfig => ({
    type: 'line',
    title: 'RSI (14)',
    xAxisLabel: 'Time',
    yAxisLabel: 'RSI',
    showGrid: true,
    showTooltip: true,
    showLegend: false,
    indicators: ['RSI'],
    referenceLines: [
      { value: 70, label: 'Overbought', color: '#ff0000', strokeDasharray: '5 5' },
      { value: 30, label: 'Oversold', color: '#00ff00', strokeDasharray: '5 5' }
    ],
    height: 150
  })
};

export default AdvancedChart;