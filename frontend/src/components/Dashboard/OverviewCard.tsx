import React from 'react'
import { Grid, Typography, Box } from '@mui/material'
import { KPICard } from '@/components/Common/KPICard'
import { useDashboardOverview } from '@/hooks/useDashboard'

export const OverviewCard: React.FC = () => {
  const { data, isLoading, error } = useDashboardOverview()

  if (error) {
    return (
      <Box sx={{ p: 2 }}>
        <Typography color="error">Failed to load overview data</Typography>
      </Box>
    )
  }

  return (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ mb: 3 }}>
        Portfolio Overview
      </Typography>
      
      <Grid container spacing={3}>
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Total Position"
            value={0}
            prefix="$"
            suffix="M"
            isLoading={isLoading}
            color="primary"
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Daily P&L"
            value={0}
            prefix="$"
            suffix="K"
            isLoading={isLoading}
            color={"success"}
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="VaR (95%)"
            value={0}
            prefix="$"
            suffix="M"
            isLoading={isLoading}
            color="warning"
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Unrealized P&L"
            value={0}
            prefix="$"
            suffix="K"
            isLoading={isLoading}
            color={"success"}
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Realization Ratio"
            value={0}
            suffix="%"
            isLoading={isLoading}
            color="info"
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Active Contracts"
            value={data?.activeContracts || 0}
            isLoading={isLoading}
            color="secondary"
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <KPICard
            title="Pending Shipments"
            value={0}
            isLoading={isLoading}
            color="secondary"
          />
        </Grid>
        
        <Grid item xs={12} sm={6} md={3}>
          <Box sx={{ display: 'flex', alignItems: 'center', height: '100%', p: 2 }}>
            <Typography variant="body2" color="text.secondary">
              Last Updated: N/A
            </Typography>
          </Box>
        </Grid>
      </Grid>
    </Box>
  )
}