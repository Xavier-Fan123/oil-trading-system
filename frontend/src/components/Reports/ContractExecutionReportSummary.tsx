import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  Grid,
  LinearProgress,
  Skeleton,
  Stack,
  Typography,
  Paper,
  Alert,
} from '@mui/material';
import type { ContractExecutionReportFilter } from '@/types/reports';
import { ContractExecutionReportDto } from '@/types/reports';

interface ReportSummaryStats {
  totalReports: number;
  completedReports: number;
  delayedReports: number;
  onTrackReports: number;
  cancelledReports: number;
  averageExecutionPercentage: number;
  totalContractValue: number;
  paidSettledAmount: number;
  unpaidSettledAmount: number;
  paymentCompletionRate: number;
}

interface ContractExecutionReportSummaryProps {
  reports: ContractExecutionReportDto[];
  isLoading?: boolean;
  filters?: ContractExecutionReportFilter;
}

export const ContractExecutionReportSummary: React.FC<
  ContractExecutionReportSummaryProps
> = ({ reports, isLoading = false }) => {
  const [stats, setStats] = useState<ReportSummaryStats | null>(null);

  useEffect(() => {
    if (reports && reports.length > 0) {
      calculateStats();
    } else {
      setStats(null);
    }
  }, [reports]);

  const calculateStats = () => {
    const totalReports = reports.length;
    const completedReports = reports.filter(r => r.executionStatus === 'Completed').length;
    const delayedReports = reports.filter(r => r.executionStatus === 'Delayed').length;
    const onTrackReports = reports.filter(r => r.executionStatus === 'OnTrack').length;
    const cancelledReports = reports.filter(r => r.executionStatus === 'Cancelled').length;

    const totalExecutionPercentage = reports.reduce((sum, r) => sum + r.executionPercentage, 0);
    const averageExecutionPercentage = totalReports > 0 ? totalExecutionPercentage / totalReports : 0;

    const totalContractValue = reports.reduce((sum, r) => sum + (r.contractValue || 0), 0);
    const paidSettledAmount = reports.reduce((sum, r) => sum + r.paidSettledAmount, 0);
    const unpaidSettledAmount = reports.reduce((sum, r) => sum + r.unpaidSettledAmount, 0);

    const totalSettledAmount = paidSettledAmount + unpaidSettledAmount;
    const paymentCompletionRate =
      totalSettledAmount > 0 ? (paidSettledAmount / totalSettledAmount) * 100 : 0;

    setStats({
      totalReports,
      completedReports,
      delayedReports,
      onTrackReports,
      cancelledReports,
      averageExecutionPercentage,
      totalContractValue,
      paidSettledAmount,
      unpaidSettledAmount,
      paymentCompletionRate,
    });
  };

  if (isLoading) {
    return (
      <Box sx={{ mb: 3 }}>
        <Grid container spacing={2}>
          {[1, 2, 3, 4].map((i) => (
            <Grid item xs={12} sm={6} md={3} key={i}>
              <Skeleton variant="rectangular" height={120} />
            </Grid>
          ))}
        </Grid>
      </Box>
    );
  }

  if (!stats) {
    return (
      <Alert severity="info" sx={{ mb: 3 }}>
        No reports available. Adjust your filters or create new reports.
      </Alert>
    );
  }

  const StatCard: React.FC<{
    title: string;
    value: string | number;
    subtitle?: string;
    color?: 'primary' | 'success' | 'warning' | 'error' | 'info';
  }> = ({ title, value, subtitle, color = 'primary' }) => (
    <Paper elevation={2} sx={{ p: 2 }}>
      <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
        {title}
      </Typography>
      <Typography
        variant="h5"
        sx={{
          fontWeight: 600,
          color: `${color}.main`,
          mb: 0.5,
        }}
      >
        {value}
      </Typography>
      {subtitle && (
        <Typography variant="caption" color="textSecondary">
          {subtitle}
        </Typography>
      )}
    </Paper>
  );

  return (
    <Box sx={{ mb: 3 }}>
      {/* Key Metrics Row 1 */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Reports"
            value={stats.totalReports}
            subtitle={`Filtered results: ${stats.totalReports}`}
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Average Execution %"
            value={`${stats.averageExecutionPercentage.toFixed(1)}%`}
            color="success"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Contract Value"
            value={`${stats.totalContractValue.toLocaleString()}`}
            subtitle="Sum of all contract values"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Payment Completion"
            value={`${stats.paymentCompletionRate.toFixed(1)}%`}
            color={stats.paymentCompletionRate >= 80 ? 'success' : 'warning'}
          />
        </Grid>
      </Grid>

      {/* Execution Status Breakdown */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          title="Execution Status Breakdown"
          titleTypographyProps={{ variant: 'subtitle1' }}
        />
        <CardContent>
          <Grid container spacing={3}>
            <Grid item xs={12} sm={6} md={2.4}>
              <Stack spacing={1}>
                <Typography variant="caption" color="textSecondary">
                  Completed
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#4caf50' }}>
                  {stats.completedReports}
                </Typography>
                <Box sx={{ width: '100%', height: 8, backgroundColor: '#e0e0e0', borderRadius: 1 }}>
                  <Box
                    sx={{
                      height: '100%',
                      width: `${(stats.completedReports / stats.totalReports) * 100}%`,
                      backgroundColor: '#4caf50',
                      borderRadius: 1,
                    }}
                  />
                </Box>
              </Stack>
            </Grid>

            <Grid item xs={12} sm={6} md={2.4}>
              <Stack spacing={1}>
                <Typography variant="caption" color="textSecondary">
                  On Track
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#2196f3' }}>
                  {stats.onTrackReports}
                </Typography>
                <Box sx={{ width: '100%', height: 8, backgroundColor: '#e0e0e0', borderRadius: 1 }}>
                  <Box
                    sx={{
                      height: '100%',
                      width: `${(stats.onTrackReports / stats.totalReports) * 100}%`,
                      backgroundColor: '#2196f3',
                      borderRadius: 1,
                    }}
                  />
                </Box>
              </Stack>
            </Grid>

            <Grid item xs={12} sm={6} md={2.4}>
              <Stack spacing={1}>
                <Typography variant="caption" color="textSecondary">
                  Delayed
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#ff9800' }}>
                  {stats.delayedReports}
                </Typography>
                <Box sx={{ width: '100%', height: 8, backgroundColor: '#e0e0e0', borderRadius: 1 }}>
                  <Box
                    sx={{
                      height: '100%',
                      width: `${(stats.delayedReports / stats.totalReports) * 100}%`,
                      backgroundColor: '#ff9800',
                      borderRadius: 1,
                    }}
                  />
                </Box>
              </Stack>
            </Grid>

            <Grid item xs={12} sm={6} md={2.4}>
              <Stack spacing={1}>
                <Typography variant="caption" color="textSecondary">
                  Cancelled
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#f44336' }}>
                  {stats.cancelledReports}
                </Typography>
                <Box sx={{ width: '100%', height: 8, backgroundColor: '#e0e0e0', borderRadius: 1 }}>
                  <Box
                    sx={{
                      height: '100%',
                      width: `${(stats.cancelledReports / stats.totalReports) * 100}%`,
                      backgroundColor: '#f44336',
                      borderRadius: 1,
                    }}
                  />
                </Box>
              </Stack>
            </Grid>

            <Grid item xs={12} sm={6} md={2.4}>
              <Stack spacing={1}>
                <Typography variant="caption" color="textSecondary">
                  Success Rate
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#8bc34a' }}>
                  {stats.totalReports > 0
                    ? ((stats.completedReports / stats.totalReports) * 100).toFixed(1)
                    : '0'}
                  %
                </Typography>
                <Box sx={{ width: '100%', height: 8, backgroundColor: '#e0e0e0', borderRadius: 1 }}>
                  <Box
                    sx={{
                      height: '100%',
                      width: `${
                        stats.totalReports > 0
                          ? (stats.completedReports / stats.totalReports) * 100
                          : 0
                      }%`,
                      backgroundColor: '#8bc34a',
                      borderRadius: 1,
                    }}
                  />
                </Box>
              </Stack>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Settlement Summary */}
      <Card>
        <CardHeader
          title="Settlement Summary"
          titleTypographyProps={{ variant: 'subtitle1' }}
        />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={4}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#f5f5f5' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                  Total Settled
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, mb: 1 }}>
                  {(stats.paidSettledAmount + stats.unpaidSettledAmount).toLocaleString()}
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={100}
                  sx={{ mb: 1 }}
                />
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6} md={4}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#e8f5e9' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                  Paid Amount
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#4caf50', mb: 1 }}>
                  {stats.paidSettledAmount.toLocaleString()}
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={stats.paymentCompletionRate}
                  sx={{ mb: 1, backgroundColor: '#e0e0e0' }}
                />
                <Typography variant="caption" color="textSecondary">
                  {stats.paymentCompletionRate.toFixed(1)}% of total
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6} md={4}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#fff3e0' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                  Unpaid Amount
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#ff9800', mb: 1 }}>
                  {stats.unpaidSettledAmount.toLocaleString()}
                </Typography>
                <LinearProgress
                  variant="determinate"
                  value={100 - stats.paymentCompletionRate}
                  sx={{ mb: 1, backgroundColor: '#e0e0e0' }}
                />
                <Typography variant="caption" color="textSecondary">
                  {(100 - stats.paymentCompletionRate).toFixed(1)}% of total
                </Typography>
              </Paper>
            </Grid>
          </Grid>
        </CardContent>
      </Card>
    </Box>
  );
};
