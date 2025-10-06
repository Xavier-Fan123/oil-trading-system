import React from 'react'
import { 
  Container, 
  Grid, 
  Typography, 
  Box, 
  AppBar, 
  Toolbar,
  IconButton,
  Chip
} from '@mui/material'
import { Refresh, Settings, Notifications } from '@mui/icons-material'
import { OverviewCard } from '@/components/Dashboard/OverviewCard'
import { TradingMetrics } from '@/components/Dashboard/TradingMetrics'
import { PerformanceChart } from '@/components/Dashboard/PerformanceChart'
import { MarketInsights } from '@/components/Dashboard/MarketInsights'
import { OperationalStatus } from '@/components/Dashboard/OperationalStatus'
import { SettlementStatus } from '@/components/Dashboard/SettlementStatus'
import { RecentSettlements } from '@/components/Dashboard/RecentSettlements'
import { PendingSettlements } from '@/components/Dashboard/PendingSettlements'
import { PnLChart } from '@/components/Charts/PnLChart'
import { VaRChart } from '@/components/Charts/VaRChart'
import { PositionChart } from '@/components/Charts/PositionChart'
import { useQueryClient } from '@tanstack/react-query'
import { usePerformanceAnalytics } from '@/hooks/useDashboard'
import { useRisk } from '@/hooks/useRisk'
import { usePositionsSummary } from '@/hooks/usePositions'

export const Dashboard: React.FC = () => {
  const queryClient = useQueryClient()
  
  // Fetch real data from APIs
  const { data: performanceData } = usePerformanceAnalytics()
  const { data: riskData } = useRisk()
  const { data: positionsData } = usePositionsSummary()
  
  const handleRefresh = () => {
    queryClient.invalidateQueries()
  }

  // Transform data for charts with fallbacks
  const pnlData = performanceData?.performanceData || []
  const varData = riskData ? [
    { 
      period: 'Current', 
      portfolioVar95: riskData.riskMetrics?.var95 || 0, 
      portfolioVar99: riskData.riskMetrics?.var99 || 0, 
      portfolioExpectedShortfall: riskData.riskMetrics?.expectedShortfall95 || 0, 
      diversificationBenefit: 15.2, 
      componentContribution: 100 
    }
  ] : []
  
  const positionChartData: any[] = []  // Mock data - positions endpoint needs updating
  
  const totalExposure = positionsData?.totalValue || 0

  const getCurrentTime = () => {
    return new Date().toLocaleString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short'
    })
  }

  return (
    <Box sx={{ flexGrow: 1 }}>
      <AppBar position="static" color="default" elevation={1}>
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            Oil Trading Dashboard
          </Typography>
          
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Chip 
              label={`Live • ${getCurrentTime()}`}
              color="success"
              variant="outlined"
              size="small"
            />
            
            <IconButton color="inherit" onClick={handleRefresh} title="Refresh Data" aria-label="Refresh Data">
              <Refresh />
            </IconButton>
            
            <IconButton color="inherit" title="Notifications" aria-label="Notifications">
              <Notifications />
            </IconButton>
            
            <IconButton color="inherit" title="Settings" aria-label="Settings">
              <Settings />
            </IconButton>
          </Box>
        </Toolbar>
      </AppBar>

      <Container maxWidth={false} sx={{ mt: 3, mb: 3 }}>
        <Grid container spacing={3}>
          {/* Overview Cards */}
          <Grid item xs={12}>
            <OverviewCard />
          </Grid>

          {/* Main Charts Row */}
          <Grid item xs={12} lg={8}>
            <PnLChart data={pnlData} height={400} />
          </Grid>
          
          <Grid item xs={12} lg={4}>
            <PositionChart 
              data={positionChartData} 
              totalExposure={totalExposure}
              height={400} 
            />
          </Grid>

          {/* Trading Metrics and Performance */}
          <Grid item xs={12} lg={6}>
            <TradingMetrics />
          </Grid>
          
          <Grid item xs={12} lg={6}>
            <PerformanceChart />
          </Grid>

          {/* Settlement Management Section */}
          <Grid item xs={12} lg={4}>
            <SettlementStatus height={400} />
          </Grid>
          
          <Grid item xs={12} lg={4}>
            <PendingSettlements height={400} />
          </Grid>
          
          <Grid item xs={12} lg={4}>
            <RecentSettlements height={400} />
          </Grid>

          {/* Risk Analysis */}
          <Grid item xs={12}>
            <VaRChart data={varData} height={350} />
          </Grid>

          {/* Market Insights and Operations */}
          <Grid item xs={12} lg={8}>
            <MarketInsights />
          </Grid>
          
          <Grid item xs={12} lg={4}>
            <OperationalStatus />
          </Grid>
        </Grid>
      </Container>
    </Box>
  )
}