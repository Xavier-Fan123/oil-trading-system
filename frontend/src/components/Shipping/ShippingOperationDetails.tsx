import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Grid,
  Card,
  CardContent,
  Chip,
  Box,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Alert,
  CircularProgress
} from '@mui/material';
import {
  DirectionsBoat as ShipIcon,
  Schedule as ScheduleIcon,
  LocalShipping as LoadingIcon,
  LocationOn as DischargeIcon,
  Refresh as RefreshIcon,
  Receipt as SettlementIcon
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useShippingOperation } from '@/hooks/useShipping';

interface ShippingOperationDetailsProps {
  open: boolean;
  onClose: () => void;
  operationId: string | null;
  onEdit?: (operationId: string) => void;
}

export const ShippingOperationDetails: React.FC<ShippingOperationDetailsProps> = React.memo(({
  open,
  onClose,
  operationId,
  onEdit
}) => {
  const navigate = useNavigate();
  const { data: operation, isLoading, error, refetch } = useShippingOperation(
    operationId || '',
    !!operationId && open
  );

  if (!open || !operationId) return null;

  if (isLoading) {
    return (
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>Loading Operation Details...</DialogTitle>
        <DialogContent>
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>
    );
  }

  if (error) {
    return (
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>Error Loading Operation</DialogTitle>
        <DialogContent>
          <Alert 
            severity="error" 
            action={
              <Button color="inherit" size="small" onClick={() => refetch()}>
                <RefreshIcon /> Retry
              </Button>
            }
          >
            Failed to load operation details: {(error as any)?.message || 'Unknown error'}
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>
    );
  }

  if (!operation) return null;

  const getStatusColor = (status: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    switch (status?.toLowerCase()) {
      case 'planned': return 'default';
      case 'intransit': return 'info';
      case 'loading': return 'warning';
      case 'loaded': return 'primary';
      case 'discharging': return 'warning';
      case 'completed': return 'success';
      case 'cancelled': return 'error';
      default: return 'default';
    }
  };

  const getStatusLabel = (status: string): string => {
    if (!status) return 'Unknown';
    return status
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  };

  const timelineEvents = [
    {
      title: 'Operation Planned',
      date: operation.createdAt,
      icon: <ScheduleIcon />,
      completed: true
    },
    {
      title: 'Vessel Nominated',
      date: operation.createdAt,
      icon: <ShipIcon />,
      completed: !!operation.vesselName
    },
    {
      title: 'Loading Started',
      date: operation.laycanStart,
      icon: <LoadingIcon />,
      completed: !!operation.laycanStart
    },
    {
      title: 'Discharge Completed',
      date: operation.dischargeDate,
      icon: <DischargeIcon />,
      completed: !!operation.dischargeDate
    }
  ];

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">
            Shipping Operation Details - {operation.shippingNumber}
          </Typography>
          <Chip 
            label={getStatusLabel(operation.status)} 
            color={getStatusColor(operation.status)}
            size="small"
          />
        </Box>
      </DialogTitle>
      
      <DialogContent>
        <Grid container spacing={3}>
          {/* Basic Information */}
          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Vessel Information
                </Typography>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Vessel Name
                  </Typography>
                  <Typography variant="body1" fontWeight="medium">
                    {operation.vesselName}
                  </Typography>
                </Box>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    IMO Number
                  </Typography>
                  <Typography variant="body1">
                    {operation.imoNumber || 'Not specified'}
                  </Typography>
                </Box>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Planned Quantity
                  </Typography>
                  <Typography variant="body1">
                    {operation.plannedQuantity?.toLocaleString()} {operation.plannedQuantityUnit}
                  </Typography>
                </Box>
                {operation.actualQuantity && (
                  <Box mb={1}>
                    <Typography variant="body2" color="text.secondary">
                      Actual Quantity
                    </Typography>
                    <Typography variant="body1" fontWeight="medium">
                      {operation.actualQuantity.toLocaleString()} {operation.actualQuantityUnit}
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Route Information */}
          <Grid item xs={12} md={6}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Route & Schedule
                </Typography>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Load Port
                  </Typography>
                  <Typography variant="body1" fontWeight="medium">
                    {operation.loadPort || 'TBD'}
                  </Typography>
                </Box>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Discharge Port
                  </Typography>
                  <Typography variant="body1" fontWeight="medium">
                    {operation.dischargePort || 'TBD'}
                  </Typography>
                </Box>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Laycan Start (ETA)
                  </Typography>
                  <Typography variant="body1">
                    {operation.laycanStart ? new Date(operation.laycanStart).toLocaleString() : 'TBD'}
                  </Typography>
                </Box>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Laycan End (ETA)
                  </Typography>
                  <Typography variant="body1">
                    {operation.laycanEnd ? new Date(operation.laycanEnd).toLocaleString() : 'TBD'}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* Timeline */}
          <Grid item xs={12}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Operation Timeline
                </Typography>
                <List>
                  {timelineEvents.map((event, index) => (
                    <ListItem key={index}>
                      <ListItemIcon>
                        <Box
                          sx={{
                            width: 40,
                            height: 40,
                            borderRadius: '50%',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            backgroundColor: event.completed ? 'primary.main' : 'grey.300',
                            color: event.completed ? 'primary.contrastText' : 'text.secondary'
                          }}
                        >
                          {event.icon}
                        </Box>
                      </ListItemIcon>
                      <ListItemText
                        primary={
                          <Typography variant="subtitle1" fontWeight="medium">
                            {event.title}
                          </Typography>
                        }
                        secondary={
                          <Typography color="text.secondary">
                            {event.date ? new Date(event.date).toLocaleString() : 'Pending'}
                          </Typography>
                        }
                      />
                      <Chip
                        label={event.completed ? 'Completed' : 'Pending'}
                        color={event.completed ? 'success' : 'default'}
                        size="small"
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>

          {/* Contract Information */}
          <Grid item xs={12}>
            <Card variant="outlined">
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Contract Information
                </Typography>
                <Box mb={1}>
                  <Typography variant="body2" color="text.secondary">
                    Contract Number
                  </Typography>
                  <Typography variant="body1" fontWeight="medium">
                    {operation.contractNumber || 'Not specified'}
                  </Typography>
                </Box>
                {operation.norDate && (
                  <Box mb={1}>
                    <Typography variant="body2" color="text.secondary">
                      Notice of Readiness Date
                    </Typography>
                    <Typography variant="body1">
                      {new Date(operation.norDate).toLocaleString()}
                    </Typography>
                  </Box>
                )}
                {operation.billOfLadingDate && (
                  <Box mb={1}>
                    <Typography variant="body2" color="text.secondary">
                      Bill of Lading Date
                    </Typography>
                    <Typography variant="body1">
                      {new Date(operation.billOfLadingDate).toLocaleString()}
                    </Typography>
                  </Box>
                )}
                {operation.notes && (
                  <Box mt={2}>
                    <Typography variant="body2" color="text.secondary">
                      Notes
                    </Typography>
                    <Typography variant="body1">
                      {operation.notes}
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Close</Button>
        {/* Create Settlement from shipping B/L data */}
        {operation && ['loaded', 'discharging', 'completed'].includes(operation.status?.toLowerCase()) && (
          <Button
            variant="outlined"
            color="success"
            startIcon={<SettlementIcon />}
            onClick={() => {
              onClose();
              navigate(`/settlements?contractId=${operation.contractId}`);
            }}
          >
            Create Settlement
          </Button>
        )}
        {onEdit && (
          <Button variant="contained" onClick={() => onEdit(operationId)}>
            Edit Operation
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
});