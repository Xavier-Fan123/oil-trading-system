import React, { useEffect, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Grid,
  Paper,
  Typography,
  Alert,
  Tab,
  Tabs,
} from '@mui/material';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { settlementAnalyticsApi, SettlementDashboardSummary } from '@/services/settlementAnalyticsApi';

/**
 * Settlement Analytics Dashboard Component
 * Displays comprehensive settlement analytics, metrics, and KPIs with visualizations
 */
export const SettlementAnalyticsDashboard: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<SettlementDashboardSummary | null>(null);
  const [daysToAnalyze, setDaysToAnalyze] = useState(30);
  const [tabValue, setTabValue] = useState('overview');

  const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#FF6B6B', '#4ECDC4'];

  useEffect(() => {
    fetchDashboardData();
  }, [daysToAnalyze]);

  const fetchDashboardData = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await settlementAnalyticsApi.getDashboardSummary(daysToAnalyze);
      setSummary(data);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Failed to fetch settlement analytics'
      );
      console.error('Error fetching settlement analytics:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  if (!summary) {
    return <Alert severity="warning">No data available</Alert>;
  }

  const { analytics, metrics } = summary;

  return (
    <Box>
      {/* Header */}
      <Box mb={3}>
        <Typography variant="h4" gutterBottom>
          Settlement Analytics Dashboard
        </Typography>
        <Typography variant="body2" color="textSecondary">
          Analysis Period: Last {daysToAnalyze} days (Generated:{' '}
          {new Date(summary.generatedAt).toLocaleString()})
        </Typography>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs value={tabValue} onChange={(e, value) => setTabValue(value)}>
          <Tab label="Overview" value="overview" />
          <Tab label="Daily Trends" value="trends" />
          <Tab label="Currency Analysis" value="currency" />
          <Tab label="Status Distribution" value="status" />
          <Tab label="Top Partners" value="partners" />
        </Tabs>
      </Box>

      {/* Overview Tab */}
      {tabValue === 'overview' && (
        <Box>
          <Grid container spacing={3}>
            {/* Key Metrics */}
            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography color="textSecondary" gutterBottom>
                    Total Settlement Value
                  </Typography>
                  <Typography variant="h6">
                    ${metrics.totalSettlementValue.toLocaleString('en-US', {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2,
                    })}
                  </Typography>
                  <Typography variant="caption" color="primary">
                    {metrics.settlementValueTrend >= 0 ? '↑' : '↓'}{' '}
                    {Math.abs(metrics.settlementValueTrend).toFixed(1)}%
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography color="textSecondary" gutterBottom>
                    Settlements Count
                  </Typography>
                  <Typography variant="h6">
                    {metrics.totalSettlementCount}
                  </Typography>
                  <Typography variant="caption" color="primary">
                    {metrics.settlementCountTrend >= 0 ? '↑' : '↓'}{' '}
                    {Math.abs(metrics.settlementCountTrend).toFixed(1)}%
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography color="textSecondary" gutterBottom>
                    Success Rate
                  </Typography>
                  <Typography variant="h6">
                    {metrics.successRate.toFixed(1)}%
                  </Typography>
                  <Typography variant="caption" color="primary">
                    Errors: {metrics.settlementsWithErrors}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Card>
                <CardContent>
                  <Typography color="textSecondary" gutterBottom>
                    SLA Compliance
                  </Typography>
                  <Typography variant="h6">
                    {analytics.slaComplianceRate.toFixed(1)}%
                  </Typography>
                  <Typography variant="caption" color="primary">
                    30-day SLA target
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {/* Analytics Summary */}
            <Grid item xs={12} md={6}>
              <Card>
                <CardHeader title="Amount Statistics" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography color="textSecondary" variant="caption">
                        Total Amount
                      </Typography>
                      <Typography variant="h6">
                        ${analytics.totalAmount.toLocaleString('en-US', {
                          minimumFractionDigits: 2,
                        })}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography color="textSecondary" variant="caption">
                        Average Amount
                      </Typography>
                      <Typography variant="h6">
                        ${analytics.averageAmount.toLocaleString('en-US', {
                          minimumFractionDigits: 2,
                        })}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography color="textSecondary" variant="caption">
                        Minimum Amount
                      </Typography>
                      <Typography variant="h6">
                        ${analytics.minimumAmount.toLocaleString('en-US', {
                          minimumFractionDigits: 2,
                        })}
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography color="textSecondary" variant="caption">
                        Maximum Amount
                      </Typography>
                      <Typography variant="h6">
                        ${analytics.maximumAmount.toLocaleString('en-US', {
                          minimumFractionDigits: 2,
                        })}
                      </Typography>
                    </Grid>
                    <Grid item xs={12}>
                      <Typography color="textSecondary" variant="caption">
                        Avg Processing Time
                      </Typography>
                      <Typography variant="h6">
                        {analytics.averageProcessingTimeDays.toFixed(1)} days
                      </Typography>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={6}>
              <Card>
                <CardHeader title="Settlement Breakdown" />
                <CardContent>
                  {Object.entries(analytics.settlementsByType).map(([type, count]) => (
                    <Box key={type} display="flex" justifyContent="space-between" mb={1}>
                      <Typography>{type}</Typography>
                      <Typography sx={{ fontWeight: 'bold' }}>{count}</Typography>
                    </Box>
                  ))}
                  <Box mt={2}>
                    <Typography color="textSecondary" variant="caption">
                      Currencies
                    </Typography>
                    <Typography variant="body2">
                      {Object.keys(analytics.settlementsByCurrency).join(', ')}
                    </Typography>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        </Box>
      )}

      {/* Daily Trends Tab */}
      {tabValue === 'trends' && (
        <Box>
          <Paper>
            <Box p={2}>
              <Typography variant="h6" gutterBottom>
                Daily Settlement Trends
              </Typography>
              <ResponsiveContainer width="100%" height={400}>
                <LineChart data={analytics.dailyTrends}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Line type="monotone" dataKey="settlementCount" stroke="#8884d8" />
                  <Line type="monotone" dataKey="totalAmount" stroke="#82ca9d" />
                </LineChart>
              </ResponsiveContainer>
            </Box>
          </Paper>
        </Box>
      )}

      {/* Currency Analysis Tab */}
      {tabValue === 'currency' && (
        <Box>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper>
                <Box p={2}>
                  <Typography variant="h6" gutterBottom>
                    Currency Distribution
                  </Typography>
                  <ResponsiveContainer width="100%" height={300}>
                    <PieChart>
                      <Pie
                        data={analytics.currencyBreakdown}
                        dataKey="settlementCount"
                        nameKey="currency"
                        cx="50%"
                        cy="50%"
                        outerRadius={80}
                        label
                      >
                        {analytics.currencyBreakdown.map((entry, index) => (
                          <Cell
                            key={`cell-${index}`}
                            fill={COLORS[index % COLORS.length]}
                          />
                        ))}
                      </Pie>
                      <Tooltip />
                    </PieChart>
                  </ResponsiveContainer>
                </Box>
              </Paper>
            </Grid>

            <Grid item xs={12} md={6}>
              <Paper>
                <Box p={2}>
                  <Typography variant="h6" gutterBottom>
                    Currency Breakdown
                  </Typography>
                  {analytics.currencyBreakdown.map((currency) => (
                    <Box key={currency.currency} mb={2}>
                      <Box display="flex" justifyContent="space-between" mb={0.5}>
                        <Typography>{currency.currency}</Typography>
                        <Typography>
                          {currency.percentageOfTotal.toFixed(1)}%
                        </Typography>
                      </Box>
                      <Box bgcolor="#f0f0f0" height={8} borderRadius={4}>
                        <Box
                          bgcolor="#1976d2"
                          height="100%"
                          borderRadius={4}
                          width={`${currency.percentageOfTotal}%`}
                        />
                      </Box>
                      <Typography variant="caption" color="textSecondary">
                        ${currency.totalAmount.toLocaleString('en-US', {
                          minimumFractionDigits: 2,
                        })} ({currency.settlementCount} settlements)
                      </Typography>
                    </Box>
                  ))}
                </Box>
              </Paper>
            </Grid>
          </Grid>
        </Box>
      )}

      {/* Status Distribution Tab */}
      {tabValue === 'status' && (
        <Box>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Paper>
                <Box p={2}>
                  <Typography variant="h6" gutterBottom>
                    Status Distribution
                  </Typography>
                  <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={analytics.statusDistribution}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="status" />
                      <YAxis />
                      <Tooltip />
                      <Bar dataKey="count" fill="#8884d8" />
                    </BarChart>
                  </ResponsiveContainer>
                </Box>
              </Paper>
            </Grid>

            <Grid item xs={12} md={6}>
              <Paper>
                <Box p={2}>
                  <Typography variant="h6" gutterBottom>
                    Status Percentages
                  </Typography>
                  {analytics.statusDistribution.map((status) => (
                    <Box key={status.status} mb={2}>
                      <Box display="flex" justifyContent="space-between" mb={0.5}>
                        <Typography>{status.status}</Typography>
                        <Typography>
                          {status.percentage.toFixed(1)}% ({status.count})
                        </Typography>
                      </Box>
                      <Box bgcolor="#f0f0f0" height={8} borderRadius={4}>
                        <Box
                          bgcolor="#82ca9d"
                          height="100%"
                          borderRadius={4}
                          width={`${status.percentage}%`}
                        />
                      </Box>
                    </Box>
                  ))}
                </Box>
              </Paper>
            </Grid>
          </Grid>
        </Box>
      )}

      {/* Top Partners Tab */}
      {tabValue === 'partners' && (
        <Box>
          <Paper>
            <Box p={2}>
              <Typography variant="h6" gutterBottom>
                Top Trading Partners by Settlement Volume
              </Typography>
              <ResponsiveContainer width="100%" height={400}>
                <BarChart data={analytics.topPartners}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="partnerName" angle={-45} textAnchor="end" height={100} />
                  <YAxis />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="totalAmount" fill="#8884d8" />
                  <Bar dataKey="averageAmount" fill="#82ca9d" />
                </BarChart>
              </ResponsiveContainer>

              {/* Partner Details Table */}
              <Box mt={3}>
                <Typography variant="subtitle2" gutterBottom>
                  Partner Details
                </Typography>
                {analytics.topPartners.map((partner) => (
                  <Card key={partner.partnerId} sx={{ mb: 1 }}>
                    <CardContent>
                      <Box display="flex" justifyContent="space-between" mb={1}>
                        <Typography variant="subtitle1" fontWeight="bold">
                          {partner.partnerName}
                        </Typography>
                        <Typography variant="caption">
                          {partner.settlementType}
                        </Typography>
                      </Box>
                      <Grid container spacing={2}>
                        <Grid item xs={6} sm={3}>
                          <Typography color="textSecondary" variant="caption">
                            Settlements
                          </Typography>
                          <Typography variant="h6">
                            {partner.settlementCount}
                          </Typography>
                        </Grid>
                        <Grid item xs={6} sm={3}>
                          <Typography color="textSecondary" variant="caption">
                            Total Amount
                          </Typography>
                          <Typography variant="h6">
                            ${partner.totalAmount.toLocaleString('en-US', {
                              minimumFractionDigits: 2,
                            })}
                          </Typography>
                        </Grid>
                        <Grid item xs={6} sm={3}>
                          <Typography color="textSecondary" variant="caption">
                            Average
                          </Typography>
                          <Typography variant="h6">
                            ${partner.averageAmount.toLocaleString('en-US', {
                              minimumFractionDigits: 2,
                            })}
                          </Typography>
                        </Grid>
                      </Grid>
                    </CardContent>
                  </Card>
                ))}
              </Box>
            </Box>
          </Paper>
        </Box>
      )}
    </Box>
  );
};

export default SettlementAnalyticsDashboard;
