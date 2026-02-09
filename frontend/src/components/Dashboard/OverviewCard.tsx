import React from 'react'
import { Grid, Typography, Box } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { KPICard } from '@/components/Common/KPICard'
import { useDashboardOverview } from '@/hooks/useDashboard'

export const OverviewCard: React.FC = () => {
  const navigate = useNavigate()
  const { data, isLoading, error } = useDashboardOverview()

  if (error) {
    return (
      <Box sx={{ p: 2 }}>
        <Typography color="error">Failed to load overview data</Typography>
      </Box>
    )
  }

  const totalExposure = data?.totalExposure || 0
  const dailyPnL = data?.dailyPnL || 0
  const var95 = data?.vaR95 || 0
  const unrealizedPnL = data?.unrealizedPnL || 0
  const activeContracts = (data?.activePurchaseContracts || 0) + (data?.activeSalesContracts || 0)
  const pendingContracts = data?.pendingContracts || 0
  const volatility = data?.portfolioVolatility || 0

  const formatTime = (iso: string | undefined) => {
    if (!iso) return 'N/A'
    return new Date(iso).toLocaleString('en-US', {
      month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
    })
  }

  return (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
        Portfolio Overview
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Total Exposure"
            value={Number((totalExposure / 1_000_000).toFixed(2))}
            prefix="$"
            suffix="M"
            isLoading={isLoading}
            color="primary"
            onClick={() => navigate('/positions')}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Daily P&L"
            value={Number((dailyPnL / 1_000).toFixed(1))}
            prefix="$"
            suffix="K"
            isLoading={isLoading}
            color={dailyPnL >= 0 ? "success" : "error"}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="VaR (95%)"
            value={Number((var95 / 1_000_000).toFixed(2))}
            prefix="$"
            suffix="M"
            isLoading={isLoading}
            color="warning"
            onClick={() => navigate('/risk')}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Unrealized P&L"
            value={Number((unrealizedPnL / 1_000).toFixed(1))}
            prefix="$"
            suffix="K"
            isLoading={isLoading}
            color={unrealizedPnL >= 0 ? "success" : "error"}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Volatility"
            value={Number(volatility.toFixed(1))}
            suffix="%"
            isLoading={isLoading}
            color="info"
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Active Contracts"
            value={activeContracts}
            isLoading={isLoading}
            color="secondary"
            onClick={() => navigate('/contracts')}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Pending Approval"
            value={pendingContracts}
            isLoading={isLoading}
            color="secondary"
            onClick={() => navigate('/contracts')}
          />
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Box sx={{ display: 'flex', alignItems: 'center', height: '100%', p: 2 }}>
            <Typography variant="body2" color="text.secondary">
              Last Updated: {formatTime(data?.calculatedAt)}
            </Typography>
          </Box>
        </Grid>
      </Grid>
    </Box>
  )
}