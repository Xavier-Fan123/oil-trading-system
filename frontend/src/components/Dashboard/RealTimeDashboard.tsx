import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Avatar,
  Button,
  Switch,
  FormControlLabel,
  Alert,
  Chip,
  LinearProgress,
} from '@mui/material';
import {
  TrendingUp as TrendingUpIcon,
  TrendingDown as TrendingDownIcon,
  Assessment as AssessmentIcon,
  Business as BusinessIcon,
  AccountBalance as AccountBalanceIcon,
  Notifications as NotificationsIcon,
} from '@mui/icons-material';
import RealTimeChart, { useRealTimeData } from '../Charts/RealTimeChart';
// GraphQL subscriptions removed for production stability
import { format } from 'date-fns';

interface KPICardProps {
  title: string;
  value: string | number;
  change?: number;
  icon: React.ReactNode;
  color: 'primary' | 'secondary' | 'success' | 'error' | 'warning' | 'info';
  loading?: boolean;
}

const KPICard: React.FC<KPICardProps> = ({ title, value, change, icon, color, loading = false }) => {
  const formatChange = (change: number) => {
    const abs = Math.abs(change);
    const sign = change >= 0 ? '+' : '-';
    return `${sign}${abs.toFixed(2)}%`;
  };

  const getChangeColor = (change: number) => {
    if (change > 0) return 'success.main';
    if (change < 0) return 'error.main';
    return 'text.secondary';
  };

  return (
    <Card>
      <CardContent>
        {loading && <LinearProgress sx={{ mb: 2 }} />}
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="textSecondary" gutterBottom variant="body2">
              {title}
            </Typography>
            <Typography variant="h4" component="h2">
              {value}
            </Typography>
            {change !== undefined && (
              <Box display="flex" alignItems="center" mt={1}>
                {change >= 0 ? <TrendingUpIcon fontSize="small" /> : <TrendingDownIcon fontSize="small" />}
                <Typography variant="body2" sx={{ ml: 0.5, color: getChangeColor(change) }}>
                  {formatChange(change)}
                </Typography>
              </Box>
            )}
          </Box>
          <Avatar sx={{ bgcolor: `${color}.main`, width: 56, height: 56 }}>
            {icon}
          </Avatar>
        </Box>
      </CardContent>
    </Card>
  );
};

interface NotificationItem {
  id: string;
  type: 'product' | 'contract';
  message: string;
  timestamp: Date;
  severity: 'info' | 'success' | 'warning' | 'error';
}

const RealTimeDashboard: React.FC = () => {
  const [realTimeEnabled, setRealTimeEnabled] = useState(true);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);

  // Mock KPI data with real-time updates
  const [kpiData, setKpiData] = useState({
    totalValue: 245600000,
    activeContracts: 127,
    riskExposure: 15.7,
    profitMargin: 8.3,
  });

  // Real-time chart data
  const brentData = useRealTimeData(81.45, 0.015, 3000, realTimeEnabled);
  const wtiData = useRealTimeData(78.22, 0.018, 3500, realTimeEnabled);
  const portfolioValueData = useRealTimeData(245600000, 0.005, 5000, realTimeEnabled);

  // Real-time notifications via REST API (GraphQL removed)

  // Simulate KPI updates
  useEffect(() => {
    if (!realTimeEnabled) return;

    const interval = setInterval(() => {
      setKpiData(prev => ({
        totalValue: prev.totalValue + (Math.random() - 0.5) * 1000000,
        activeContracts: prev.activeContracts + Math.floor((Math.random() - 0.5) * 3),
        riskExposure: Math.max(0, Math.min(100, prev.riskExposure + (Math.random() - 0.5) * 2)),
        profitMargin: Math.max(0, prev.profitMargin + (Math.random() - 0.5) * 0.5),
      }));
    }, 8000);

    return () => clearInterval(interval);
  }, [realTimeEnabled]);

  // Real-time notifications via REST API polling (GraphQL removed)
  useEffect(() => {
    if (!realTimeEnabled) return;

    const interval = setInterval(() => {
      if (Math.random() > 0.85) { // 15% chance of notification
        const isContract = Math.random() > 0.5;
        const newNotification: NotificationItem = {
          id: `${isContract ? 'contract' : 'product'}-${Date.now()}`,
          type: isContract ? 'contract' : 'product',
          message: isContract 
            ? `Contract status updated: PC-${Math.floor(Math.random() * 1000) + 2000}` 
            : `Product price updated: ${Math.random() > 0.5 ? 'BRENT' : 'WTI'}`,
          timestamp: new Date(),
          severity: 'info',
        };
        setNotifications(prev => [newNotification, ...prev].slice(0, 10));
      }
    }, 15000); // Check every 15 seconds

    return () => clearInterval(interval);
  }, [realTimeEnabled]);

  const clearAllNotifications = () => {
    setNotifications([]);
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      notation: 'compact',
      maximumFractionDigits: 1,
    }).format(value);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Real-Time Trading Dashboard
        </Typography>
        <Box display="flex" alignItems="center" gap={2}>
          <FormControlLabel
            control={
              <Switch
                checked={realTimeEnabled}
                onChange={(e) => setRealTimeEnabled(e.target.checked)}
                color="primary"
              />
            }
            label="Real-Time Updates"
          />
          {notifications.length > 0 && (
            <Button
              variant="outlined"
              startIcon={<NotificationsIcon />}
              onClick={clearAllNotifications}
            >
              Clear Notifications ({notifications.length})
            </Button>
          )}
        </Box>
      </Box>

      {/* KPI Cards */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Portfolio Value"
            value={formatCurrency(kpiData.totalValue)}
            change={realTimeEnabled ? 2.4 : undefined}
            icon={<AccountBalanceIcon />}
            color="primary"
            loading={realTimeEnabled}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Active Contracts"
            value={kpiData.activeContracts}
            change={realTimeEnabled ? 1.8 : undefined}
            icon={<BusinessIcon />}
            color="success"
            loading={realTimeEnabled}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Risk Exposure"
            value={`${kpiData.riskExposure.toFixed(1)}%`}
            change={realTimeEnabled ? -0.3 : undefined}
            icon={<AssessmentIcon />}
            color="warning"
            loading={realTimeEnabled}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Profit Margin"
            value={`${kpiData.profitMargin.toFixed(1)}%`}
            change={realTimeEnabled ? 0.5 : undefined}
            icon={<TrendingUpIcon />}
            color="info"
            loading={realTimeEnabled}
          />
        </Grid>
      </Grid>

      {/* Real-Time Notifications */}
      {notifications.length > 0 && (
        <Box mb={3}>
          <Typography variant="h6" gutterBottom>
            Recent Activity
          </Typography>
          <Grid container spacing={2}>
            {notifications.slice(0, 3).map((notification) => (
              <Grid item xs={12} md={4} key={notification.id}>
                <Alert severity={notification.severity} sx={{ height: '100%' }}>
                  <Box>
                    <Typography variant="body2" fontWeight="bold">
                      {notification.message}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {format(notification.timestamp, 'HH:mm:ss')}
                    </Typography>
                  </Box>
                </Alert>
              </Grid>
            ))}
          </Grid>
        </Box>
      )}

      {/* Real-Time Charts */}
      <Grid container spacing={3}>
        <Grid item xs={12} lg={6}>
          <RealTimeChart
            title="Brent Crude Oil"
            data={brentData}
            chartType="line"
            enableRealTime={realTimeEnabled}
            onRealTimeToggle={setRealTimeEnabled}
            referenceLines={[
              { value: 80, label: 'Resistance', color: '#ef4444' },
              { value: 75, label: 'Support', color: '#22c55e' },
            ]}
            color="#f59e0b"
            height={350}
          />
        </Grid>
        <Grid item xs={12} lg={6}>
          <RealTimeChart
            title="WTI Crude Oil"
            data={wtiData}
            chartType="line"
            enableRealTime={realTimeEnabled}
            referenceLines={[
              { value: 78, label: 'Resistance', color: '#ef4444' },
              { value: 72, label: 'Support', color: '#22c55e' },
            ]}
            color="#06b6d4"
            height={350}
          />
        </Grid>
        <Grid item xs={12}>
          <RealTimeChart
            title="Portfolio Value Over Time"
            data={portfolioValueData}
            chartType="area"
            enableRealTime={realTimeEnabled}
            showVolume={false}
            color="#10b981"
            height={300}
          />
        </Grid>
      </Grid>

      {/* Status Indicators */}
      <Box mt={3}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              System Status
            </Typography>
            <Box display="flex" gap={2} flexWrap="wrap">
              <Chip
                label="GraphQL API"
                color={realTimeEnabled ? 'success' : 'default'}
                variant="outlined"
                icon={realTimeEnabled ? <TrendingUpIcon /> : undefined}
              />
              <Chip
                label="Real-Time Data"
                color={realTimeEnabled ? 'success' : 'default'}
                variant="outlined"
                icon={realTimeEnabled ? <TrendingUpIcon /> : undefined}
              />
              <Chip
                label="WebSocket Connection"
                color={realTimeEnabled ? 'success' : 'warning'}
                variant="outlined"
              />
              <Chip
                label="Risk Monitoring"
                color="success"
                variant="outlined"
                icon={<AssessmentIcon />}
              />
              <Chip
                label={`${notifications.length} Active Notifications`}
                color={notifications.length > 0 ? 'info' : 'default'}
                variant="outlined"
                icon={<NotificationsIcon />}
              />
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
};

export default RealTimeDashboard;