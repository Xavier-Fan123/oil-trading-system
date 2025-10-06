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
  Schedule,
  CheckCircle,
  Error,
  Warning,
  Info,
} from '@mui/icons-material'
import { AlertBanner } from '@/components/Common/AlertBanner'
import { useOperationalStatus } from '@/hooks/useDashboard'

export const OperationalStatus: React.FC = () => {
  const { data: _data, isLoading, error } = useOperationalStatus()

  if (error) {
    return (
      <Card>
        <CardContent>
          <Typography color="error">Failed to load operational status</Typography>
        </CardContent>
      </Card>
    )
  }

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
      case 'delivered':
      case 'loaded':
        return 'success'
      case 'in_transit':
      case 'loading':
      case 'discharging':
        return 'info'
      case 'delayed':
      case 'pending':
        return 'warning'
      case 'cancelled':
      case 'failed':
        return 'error'
      default:
        return 'default'
    }
  }

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
      case 'delivered':
        return <CheckCircle />
      case 'in_transit':
      case 'loading':
        return <LocalShipping />
      case 'delayed':
      case 'pending':
        return <Schedule />
      default:
        return <Info />
    }
  }

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'High':
        return <Error color="error" />
      case 'Medium':
        return <Warning color="warning" />
      default:
        return <Info color="info" />
    }
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Operational Status
        </Typography>
        
        {isLoading && <LinearProgress sx={{ mb: 2 }} />}
        
        {false && (
          <AlertBanner alerts={[]} maxDisplay={2} />
        )}
        
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
                  label={0}
                  color="success"
                  size="small"
                />
              </Box>
              
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Pending Contracts
                </Typography>
                <Chip
                  label={0}
                  color="warning"
                  size="small"
                />
              </Box>
              
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">
                  Completed This Month
                </Typography>
                <Chip
                  label={0}
                  color="info"
                  size="small"
                />
              </Box>
            </Box>
          </Grid>
          
          <Grid item xs={12} md={8}>
            <Typography variant="subtitle1" gutterBottom>
              Active Shipments
            </Typography>
            <TableContainer sx={{ maxHeight: 300 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>Shipment ID</TableCell>
                    <TableCell>Vessel</TableCell>
                    <TableCell>Route</TableCell>
                    <TableCell align="center">Status</TableCell>
                    <TableCell align="right">Quantity</TableCell>
                    <TableCell>ETA</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {[]?.map((shipment: any, index: number) => (
                    <TableRow key={index}>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {shipment.shipmentId}
                        </Typography>
                      </TableCell>
                      <TableCell>{shipment.vessel}</TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {shipment.origin} → {shipment.destination}
                        </Typography>
                      </TableCell>
                      <TableCell align="center">
                        <Chip
                          label={shipment.status}
                          size="small"
                          color={getStatusColor(shipment.status)}
                          icon={getStatusIcon(shipment.status)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        {shipment.quantity.toLocaleString()} {shipment.unit}
                      </TableCell>
                      <TableCell>
                        {new Date(shipment.eta).toLocaleDateString()}
                      </TableCell>
                    </TableRow>
                  )) || []}
                </TableBody>
              </Table>
            </TableContainer>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle1" gutterBottom>
              Upcoming Deliveries
            </Typography>
            <List dense>
              {[]?.slice(0, 5).map((delivery: any, index: number) => (
                <ListItem key={index} divider>
                  <ListItemIcon>
                    <Schedule color="info" />
                  </ListItemIcon>
                  <ListItemText
                    primary={delivery.contractNumber}
                    secondary={`${delivery.counterparty} • ${delivery.quantity.toLocaleString()} ${delivery.unit} ${delivery.product} • Delivery: ${new Date(delivery.deliveryDate).toLocaleDateString()}`}
                  />
                  <Chip
                    label={delivery.status}
                    size="small"
                    color={getStatusColor(delivery.status)}
                  />
                </ListItem>
              )) || []}
            </List>
          </Grid>
          
          <Grid item xs={12} md={6}>
            <Typography variant="subtitle1" gutterBottom>
              Recent Alerts
            </Typography>
            <List dense>
              {[]?.slice(0, 5).map((alert: any, index: number) => (
                <ListItem key={index} divider>
                  <ListItemIcon>
                    {getSeverityIcon(alert.severity)}
                  </ListItemIcon>
                  <ListItemText
                    primary={alert.alertType}
                    secondary={`${alert.message} • ${new Date(alert.timestamp).toLocaleString()}`}
                  />
                  <Chip
                    label={alert.severity}
                    size="small"
                    color={
                      alert.severity === 'High' ? 'error' :
                      alert.severity === 'Medium' ? 'warning' : 'info'
                    }
                  />
                </ListItem>
              )) || []}
            </List>
          </Grid>
        </Grid>
        
        <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="caption" color="text.secondary">
            Last Updated: N/A
          </Typography>
        </Box>
      </CardContent>
    </Card>
  )
}