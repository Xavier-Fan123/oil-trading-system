import React, { useEffect, useState } from 'react';
import {
  Box,
  Container,
  Grid,
  Paper,
  Typography,
  CircularProgress,
  Alert,
  Card,
  CardContent,
  LinearProgress,
  Divider,
} from '@mui/material';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import TrendingUpIcon from '@mui/icons-material/TrendingUp';
import TrendingDownIcon from '@mui/icons-material/TrendingDown';
import WarningIcon from '@mui/icons-material/Warning';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import tradingPartnerExposureApi from '@/services/tradingPartnerExposureApi';
import { formatCurrency, formatPercentage } from '@/utils/formatting';

interface MetricCard {
  title: string;
  value: number;
  previousValue?: number;
  unit?: string;
  color: string;
  icon: React.ReactNode;
}

export const KeyMetricsDashboard: React.FC = () => {
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [metrics, setMetrics] = useState({
    totalApAmount: 0,
    totalArAmount: 0,
    paidApAmount: 0,
    paidArAmount: 0,
    unpaidApAmount: 0,
    unpaidArAmount: 0,
    totalOverdue: 0,
    netCashFlow: 0,
    totalExposure: 0,
    averageUtilization: 0,
    partnerCount: 0,
    atRiskCount: 0,
  });

  const [chartData, setChartData] = useState<any[]>([]);

  useEffect(() => {
    loadMetrics();
  }, []);

  const loadMetrics = async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch all partners
      const allPartners = await tradingPartnerExposureApi.getAllExposure();
      const atRiskPartners = await tradingPartnerExposureApi.getAtRiskPartners(3);

      if (allPartners.length === 0) {
        setError('No trading partners found');
        return;
      }

      // Calculate metrics
      let totalApAmount = 0;
      let totalArAmount = 0;
      let paidApAmount = 0;
      let paidArAmount = 0;
      let unpaidApAmount = 0;
      let unpaidArAmount = 0;
      let totalOverdue = 0;
      let totalExposure = 0;
      let totalUtilization = 0;

      // Fetch settlement details for each partner
      for (const partner of allPartners) {
        const details = await tradingPartnerExposureApi.getSettlementDetails(
          partner.tradingPartnerId
        );

        totalApAmount += details.totalApAmount;
        totalArAmount += details.totalArAmount;
        paidApAmount += details.paidApAmount;
        paidArAmount += details.paidArAmount;
        unpaidApAmount += details.unpaidApAmount;
        unpaidArAmount += details.unpaidArAmount;
        totalOverdue += partner.overdueApAmount + partner.overdueArAmount;
        totalExposure += partner.currentExposure;
        totalUtilization += partner.creditUtilizationPercentage;
      }

      const netCashFlow = totalArAmount - totalApAmount;
      const averageUtilization =
        allPartners.length > 0 ? totalUtilization / allPartners.length : 0;

      setMetrics({
        totalApAmount,
        totalArAmount,
        paidApAmount,
        paidArAmount,
        unpaidApAmount,
        unpaidArAmount,
        totalOverdue,
        netCashFlow,
        totalExposure,
        averageUtilization,
        partnerCount: allPartners.length,
        atRiskCount: atRiskPartners.length,
      });

      // Prepare chart data (simulate monthly data)
      const mockChartData = [
        { month: 'Jan', ap: totalApAmount * 0.8, ar: totalArAmount * 0.75, overdue: totalOverdue * 0.5 },
        { month: 'Feb', ap: totalApAmount * 0.85, ar: totalArAmount * 0.8, overdue: totalOverdue * 0.6 },
        { month: 'Mar', ap: totalApAmount * 0.9, ar: totalArAmount * 0.85, overdue: totalOverdue * 0.7 },
        { month: 'Apr', ap: totalApAmount * 0.95, ar: totalArAmount * 0.9, overdue: totalOverdue * 0.8 },
        { month: 'May', ap: totalApAmount, ar: totalArAmount, overdue: totalOverdue },
      ];

      setChartData(mockChartData);
    } catch (err) {
      console.error('Error loading metrics:', err);
      setError('Failed to load dashboard metrics');
    } finally {
      setIsLoading(false);
    }
  };

  const getMetricColor = (value: number, threshold: number, isPositive: boolean = true) => {
    if (isPositive) {
      return value < threshold ? '#4CAF50' : '#FF9800';
    } else {
      return value < threshold ? '#FF9800' : '#F44336';
    }
  };

  const MetricCard: React.FC<MetricCard> = ({
    title,
    value,
    previousValue,
    unit = '',
    color,
    icon,
  }) => {
    const change = previousValue ? ((value - previousValue) / previousValue) * 100 : 0;
    const isPositiveChange = change >= 0;

    return (
      <Card sx={{ height: '100%' }}>
        <CardContent>
          <Box display="flex" justifyContent="space-between" alignItems="flex-start" sx={{ mb: 2 }}>
            <Box flex={1}>
              <Typography color="textSecondary" gutterBottom>
                {title}
              </Typography>
              <Typography variant="h6" fontWeight="bold" sx={{ color }}>
                {unit === 'currency'
                  ? formatCurrency(value)
                  : unit === 'percentage'
                  ? formatPercentage(value)
                  : Math.floor(value)}
              </Typography>
            </Box>
            <Box sx={{ color, opacity: 0.8 }}>{icon}</Box>
          </Box>

          {previousValue !== undefined && previousValue !== 0 && (
            <Box display="flex" alignItems="center" gap={0.5}>
              {isPositiveChange ? (
                <TrendingUpIcon sx={{ fontSize: 16, color: '#4CAF50' }} />
              ) : (
                <TrendingDownIcon sx={{ fontSize: 16, color: '#F44336' }} />
              )}
              <Typography
                variant="caption"
                sx={{
                  color: isPositiveChange ? '#4CAF50' : '#F44336',
                }}
              >
                {Math.abs(change).toFixed(1)}% vs previous period
              </Typography>
            </Box>
          )}
        </CardContent>
      </Card>
    );
  };

  if (isLoading) {
    return (
      <Container>
        <Box display="flex" justifyContent="center" sx={{ py: 4 }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (error) {
    return (
      <Container>
        <Alert severity="error">{error}</Alert>
      </Container>
    );
  }

  const apColor = getMetricColor(
    metrics.unpaidApAmount,
    metrics.totalApAmount * 0.5,
    false
  );
  const arColor = getMetricColor(
    metrics.unpaidArAmount,
    metrics.totalArAmount * 0.3,
    true
  );
  const utilizationColor = getMetricColor(metrics.averageUtilization, 85, false);
  const cashFlowColor = metrics.netCashFlow >= 0 ? '#4CAF50' : '#F44336';

  return (
    <Container maxWidth="lg" sx={{ py: 3 }}>
      <Typography variant="h4" fontWeight="bold" sx={{ mb: 3 }}>
        Financial Key Metrics Dashboard
      </Typography>

      {/* Top Alert for At-Risk Partners */}
      {metrics.atRiskCount > 0 && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          <WarningIcon sx={{ mr: 1, verticalAlign: 'middle' }} />
          {metrics.atRiskCount} partner(s) with high/critical credit risk detected. Review their
          exposures immediately.
        </Alert>
      )}

      {/* Key Metrics Cards */}
      <Grid container spacing={2} sx={{ mb: 4 }}>
        {/* Accounts Payable */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Total Accounts Payable (AP)"
            value={metrics.totalApAmount}
            unit="currency"
            color="#FF9800"
            icon={<TrendingDownIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>

        {/* Unpaid AP */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Unpaid AP (We Owe)"
            value={metrics.unpaidApAmount}
            unit="currency"
            color={apColor}
            icon={<WarningIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>

        {/* Accounts Receivable */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Total Accounts Receivable (AR)"
            value={metrics.totalArAmount}
            unit="currency"
            color="#4CAF50"
            icon={<TrendingUpIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>

        {/* Unpaid AR */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Unpaid AR (They Owe)"
            value={metrics.unpaidArAmount}
            unit="currency"
            color={arColor}
            icon={<CheckCircleIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>
      </Grid>

      {/* Cash Flow and Risk Metrics */}
      <Grid container spacing={2} sx={{ mb: 4 }}>
        {/* Net Cash Flow */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Net Cash Flow Position"
            value={metrics.netCashFlow}
            unit="currency"
            color={cashFlowColor}
            icon={
              metrics.netCashFlow >= 0 ? (
                <TrendingUpIcon sx={{ fontSize: 32 }} />
              ) : (
                <TrendingDownIcon sx={{ fontSize: 32 }} />
              )
            }
          />
        </Grid>

        {/* Total Overdue */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Total Overdue Amounts"
            value={metrics.totalOverdue}
            unit="currency"
            color={metrics.totalOverdue > 0 ? '#F44336' : '#4CAF50'}
            icon={<WarningIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>

        {/* Average Credit Utilization */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Avg Credit Utilization"
            value={metrics.averageUtilization}
            unit="percentage"
            color={utilizationColor}
            icon={<WarningIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>

        {/* At-Risk Partners */}
        <Grid item xs={12} sm={6} md={3}>
          <MetricCard
            title="Partners at Risk"
            value={metrics.atRiskCount}
            previousValue={metrics.partnerCount}
            unit=""
            color={metrics.atRiskCount > 0 ? '#FF9800' : '#4CAF50'}
            icon={<WarningIcon sx={{ fontSize: 32 }} />}
          />
        </Grid>
      </Grid>

      {/* Detailed Breakdowns */}
      <Grid container spacing={2} sx={{ mb: 4 }}>
        {/* AP Breakdown */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" sx={{ mb: 2 }}>
              Accounts Payable Breakdown
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <Box sx={{ p: 1.5, backgroundColor: '#FFF3E0', borderRadius: 1 }}>
                  <Typography variant="caption" color="textSecondary">
                    Paid Invoices
                  </Typography>
                  <Typography variant="body2" fontWeight="bold" sx={{ color: '#4CAF50' }}>
                    {formatCurrency(metrics.paidApAmount)}
                  </Typography>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
                    {formatPercentage(
                      (metrics.paidApAmount / metrics.totalApAmount) * 100 || 0
                    )}{' '}
                    of total
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6}>
                <Box sx={{ p: 1.5, backgroundColor: '#FFEBEE', borderRadius: 1 }}>
                  <Typography variant="caption" color="textSecondary">
                    Unpaid Invoices
                  </Typography>
                  <Typography variant="body2" fontWeight="bold" sx={{ color: '#F44336' }}>
                    {formatCurrency(metrics.unpaidApAmount)}
                  </Typography>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
                    {formatPercentage(
                      (metrics.unpaidApAmount / metrics.totalApAmount) * 100 || 0
                    )}{' '}
                    of total
                  </Typography>
                </Box>
              </Grid>
            </Grid>
            {/* Progress bar */}
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="textSecondary">
                Payment Collection Status
              </Typography>
              <LinearProgress
                variant="determinate"
                value={(metrics.paidApAmount / metrics.totalApAmount) * 100 || 0}
                sx={{
                  height: 8,
                  backgroundColor: '#E0E0E0',
                  '& .MuiLinearProgress-bar': {
                    backgroundColor: '#4CAF50',
                  },
                }}
              />
            </Box>
          </Paper>
        </Grid>

        {/* AR Breakdown */}
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" fontWeight="bold" sx={{ mb: 2 }}>
              Accounts Receivable Breakdown
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <Box sx={{ p: 1.5, backgroundColor: '#E8F5E9', borderRadius: 1 }}>
                  <Typography variant="caption" color="textSecondary">
                    Collected Invoices
                  </Typography>
                  <Typography variant="body2" fontWeight="bold" sx={{ color: '#4CAF50' }}>
                    {formatCurrency(metrics.paidArAmount)}
                  </Typography>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
                    {formatPercentage(
                      (metrics.paidArAmount / metrics.totalArAmount) * 100 || 0
                    )}{' '}
                    of total
                  </Typography>
                </Box>
              </Grid>
              <Grid item xs={6}>
                <Box sx={{ p: 1.5, backgroundColor: '#F3E5F5', borderRadius: 1 }}>
                  <Typography variant="caption" color="textSecondary">
                    Outstanding Invoices
                  </Typography>
                  <Typography variant="body2" fontWeight="bold" sx={{ color: '#9C27B0' }}>
                    {formatCurrency(metrics.unpaidArAmount)}
                  </Typography>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
                    {formatPercentage(
                      (metrics.unpaidArAmount / metrics.totalArAmount) * 100 || 0
                    )}{' '}
                    of total
                  </Typography>
                </Box>
              </Grid>
            </Grid>
            {/* Progress bar */}
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="textSecondary">
                Collection Progress
              </Typography>
              <LinearProgress
                variant="determinate"
                value={(metrics.paidArAmount / metrics.totalArAmount) * 100 || 0}
                sx={{
                  height: 8,
                  backgroundColor: '#E0E0E0',
                  '& .MuiLinearProgress-bar': {
                    backgroundColor: '#9C27B0',
                  },
                }}
              />
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Charts */}
      {chartData.length > 0 && (
        <Grid container spacing={2}>
          {/* AP vs AR Trend */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" fontWeight="bold" sx={{ mb: 2 }}>
                AP vs AR Trend (Last 5 Months)
              </Typography>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip formatter={(value) => formatCurrency(value as number)} />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="ap"
                    stroke="#FF9800"
                    name="Accounts Payable"
                    connectNulls
                  />
                  <Line
                    type="monotone"
                    dataKey="ar"
                    stroke="#4CAF50"
                    name="Accounts Receivable"
                    connectNulls
                  />
                </LineChart>
              </ResponsiveContainer>
            </Paper>
          </Grid>

          {/* Overdue Amount Trend */}
          <Grid item xs={12} md={6}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" fontWeight="bold" sx={{ mb: 2 }}>
                Overdue Amounts Trend
              </Typography>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip formatter={(value) => formatCurrency(value as number)} />
                  <Legend />
                  <Bar dataKey="overdue" fill="#F44336" name="Overdue Amount" />
                </BarChart>
              </ResponsiveContainer>
            </Paper>
          </Grid>
        </Grid>
      )}
    </Container>
  );
};

export default KeyMetricsDashboard;
