import React from 'react'
import {
  Card,
  CardContent,
  Typography,
  Grid,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Box,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
} from '@mui/material'
import {
  LocalShipping,
  CheckCircle,
  Warning,
  Info,
} from '@mui/icons-material'
import { useOperationalStatus } from '@/hooks/useDashboard'

export const OperationalStatus: React.FC = () => {
  const { data, isLoading, error } = useOperationalStatus()

  if (error) {
    return (
      <Card>
        <CardContent>
          <Typography color="error">Failed to load operational status</Typography>
        </CardContent>
      </Card>
    )
  }

  const activeContracts = (data?.contractsInLaycan || 0) + (data?.contractsAwaitingExecution || 0)
  const pendingDeliveries = data?.pendingDeliveries || 0
  const completedDeliveries = data?.completedDeliveries || 0
  const activeShipments = data?.activeShipments || 0
  const upcomingLaycans = data?.upcomingLaycans || []
  const systemHealth = data?.systemHealth

  const getHealthColor = (status: string): 'success' | 'warning' | 'error' | 'default' => {
    switch (status?.toLowerCase()) {
      case 'healthy': return 'success'
      case 'degraded': return 'warning'
      case 'unhealthy': return 'error'
      default: return 'default'
    }
  }

  const getHealthIcon = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'healthy': return <CheckCircle color="success" />
      case 'degraded': return <Warning color="warning" />
      default: return <Info color="info" />
    }
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Operational Status
        </Typography>

        {isLoading && <LinearProgress sx={{ mb: 2 }} />}

        <Grid container spacing={3}>
          <Grid item xs={12} md={4}>
            <Typography variant="subtitle1" gutterBottom>
              Contract Summary
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Active Contracts
                </Typography>
                <Chip
                  label={activeContracts}
                  color="success"
                  size="small"
                />
              </Box>

              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Pending Deliveries
                </Typography>
                <Chip
                  label={pendingDeliveries}
                  color="warning"
                  size="small"
                />
              </Box>

              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Completed Deliveries
                </Typography>
                <Chip
                  label={completedDeliveries}
                  color="info"
                  size="small"
                />
              </Box>

              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Active Shipments
                </Typography>
                <Chip
                  label={activeShipments}
                  color="primary"
                  size="small"
                  icon={<LocalShipping />}
                />
              </Box>
            </Box>
          </Grid>

          <Grid item xs={12} md={8}>
            <Typography variant="subtitle1" gutterBottom>
              Upcoming Laycans
            </Typography>
            <TableContainer sx={{ maxHeight: 300 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>Contract</TableCell>
                    <TableCell>Type</TableCell>
                    <TableCell>Product</TableCell>
                    <TableCell align="right">Quantity</TableCell>
                    <TableCell>Laycan Period</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {upcomingLaycans.map((laycan, index) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {laycan.contractNumber}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={laycan.contractType}
                          size="small"
                          color={laycan.contractType === 'Purchase' ? 'primary' : 'secondary'}
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>{laycan.product}</TableCell>
                      <TableCell align="right">
                        {laycan.quantity.toLocaleString()} MT
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {new Date(laycan.laycanStart).toLocaleDateString()} - {new Date(laycan.laycanEnd).toLocaleDateString()}
                        </Typography>
                      </TableCell>
                    </TableRow>
                  ))}
                  {upcomingLaycans.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        <Typography variant="body2" color="text.secondary">No upcoming laycans</Typography>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>

          <Grid item xs={12}>
            <Typography variant="subtitle1" gutterBottom>
              System Health
            </Typography>
            <List dense>
              {systemHealth ? (
                <>
                  <ListItem divider>
                    <ListItemIcon>
                      {getHealthIcon(systemHealth.databaseStatus)}
                    </ListItemIcon>
                    <ListItemText
                      primary="Database"
                      secondary={systemHealth.databaseStatus}
                    />
                    <Chip
                      label={systemHealth.databaseStatus}
                      size="small"
                      color={getHealthColor(systemHealth.databaseStatus)}
                    />
                  </ListItem>
                  <ListItem divider>
                    <ListItemIcon>
                      {getHealthIcon(systemHealth.cacheStatus)}
                    </ListItemIcon>
                    <ListItemText
                      primary="Cache (Redis)"
                      secondary={`${systemHealth.cacheStatus} | Hit Ratio: ${((data?.cacheHitRatio || 0) * 100).toFixed(0)}%`}
                    />
                    <Chip
                      label={systemHealth.cacheStatus}
                      size="small"
                      color={getHealthColor(systemHealth.cacheStatus)}
                    />
                  </ListItem>
                  <ListItem divider>
                    <ListItemIcon>
                      {getHealthIcon(systemHealth.marketDataStatus)}
                    </ListItemIcon>
                    <ListItemText
                      primary="Market Data"
                      secondary={systemHealth.marketDataStatus}
                    />
                    <Chip
                      label={systemHealth.marketDataStatus}
                      size="small"
                      color={getHealthColor(systemHealth.marketDataStatus)}
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      {getHealthIcon(systemHealth.overallStatus)}
                    </ListItemIcon>
                    <ListItemText
                      primary="Overall System"
                      secondary={systemHealth.overallStatus}
                    />
                    <Chip
                      label={systemHealth.overallStatus}
                      size="small"
                      color={getHealthColor(systemHealth.overallStatus)}
                    />
                  </ListItem>
                </>
              ) : (
                <ListItem>
                  <ListItemText
                    primary="System health data unavailable"
                    secondary="Data will appear when the backend is fully operational"
                  />
                </ListItem>
              )}
            </List>
          </Grid>
        </Grid>

        <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">
            Last Data Refresh: {data?.lastDataRefresh ? new Date(data.lastDataRefresh).toLocaleString() : 'N/A'} | Last Updated: {data?.calculatedAt ? new Date(data.calculatedAt).toLocaleString() : 'N/A'}
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}
