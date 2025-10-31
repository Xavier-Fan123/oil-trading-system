import React, { useState } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Chip,
  IconButton,
  Typography,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Card,
  CardContent,
  Tooltip,
  Menu,
  ListItemText,
  ListItemIcon,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  LinearProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Visibility as ViewIcon,
  MoreVert as MoreIcon,
  FilterList as FilterIcon,
  LocalShipping as ShippingIcon,
  PlayArrow as StartIcon,
  CheckCircle as CompleteIcon,
  Cancel as CancelIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import {
  useShippingOperations,
  useStartLoading,
  useCompleteLoading,
  useCompleteDischarge,
  useCancelShippingOperation,
} from '@/hooks/useShipping';
import { ShippingFilters, ShippingStatus, SHIPPING_STATUS_OPTIONS } from '@/types/shipping';
import { shippingApi } from '@/services/shippingApi';

interface ShippingOperationsListProps {
  onEdit: (operationId: string) => void;
  onView: (operationId: string) => void;
  onCreate: () => void;
}

export const ShippingOperationsList: React.FC<ShippingOperationsListProps> = ({ 
  onEdit, 
  onView, 
  onCreate 
}) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [filters, setFilters] = useState<ShippingFilters>({});
  const [showFilters, setShowFilters] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedOperation, setSelectedOperation] = useState<string | null>(null);
  const [cancelDialog, setCancelDialog] = useState<{ open: boolean; operationId: string | null }>({ 
    open: false, 
    operationId: null 
  });
  const [cancelReason, setCancelReason] = useState('');

  // Fetch data
  const { data: operationsData, isLoading, error, refetch } = useShippingOperations({
    ...filters,
    page: page + 1,
    pageSize: rowsPerPage
  });

  // Mutations
  const startLoadingMutation = useStartLoading();
  const completeLoadingMutation = useCompleteLoading();
  const completeDischargeMutation = useCompleteDischarge();
  const cancelMutation = useCancelShippingOperation();

  const operations = operationsData?.items || [];
  const totalCount = operationsData?.totalCount || 0;

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleFilterChange = (key: keyof ShippingFilters, value: any) => {
    setFilters(prev => ({
      ...prev,
      [key]: value === '' ? undefined : value
    }));
    setPage(0);
  };

  const handleClearFilters = () => {
    setFilters({});
    setPage(0);
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, operationId: string) => {
    setAnchorEl(event.currentTarget);
    setSelectedOperation(operationId);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedOperation(null);
  };

  const handleStartLoading = (operationId: string) => {
    startLoadingMutation.mutate(operationId);
    handleMenuClose();
  };

  const handleCompleteLoading = (operationId: string) => {
    completeLoadingMutation.mutate(operationId);
    handleMenuClose();
  };

  const handleCompleteDischarge = (operationId: string) => {
    completeDischargeMutation.mutate(operationId);
    handleMenuClose();
  };

  const handleCancelClick = (operationId: string) => {
    setCancelDialog({ open: true, operationId });
    handleMenuClose();
  };

  const handleCancelSubmit = () => {
    if (cancelDialog.operationId && cancelReason.trim()) {
      cancelMutation.mutate({ 
        operationId: cancelDialog.operationId, 
        reason: cancelReason.trim() 
      });
      setCancelDialog({ open: false, operationId: null });
      setCancelReason('');
    }
  };


  const getStatusFromString = (statusStr: string): number => {
    const statusMap: { [key: string]: number } = {
      'Planned': 1,
      'In Transit': 2,
      'Loading': 3,
      'Loaded': 4,
      'Discharging': 5,
      'Completed': 6,
      'Cancelled': 7,
    };
    return statusMap[statusStr] || 1;
  };

  const canStartLoading = (status: string) => {
    const statusNum = getStatusFromString(status);
    return statusNum === ShippingStatus.Planned || statusNum === ShippingStatus.InTransit;
  };

  const canCompleteLoading = (status: string) => {
    const statusNum = getStatusFromString(status);
    return statusNum === ShippingStatus.Loading;
  };

  const canCompleteDischarge = (status: string) => {
    const statusNum = getStatusFromString(status);
    return statusNum === ShippingStatus.Loaded || statusNum === ShippingStatus.Discharging;
  };

  const canCancel = (status: string) => {
    const statusNum = getStatusFromString(status);
    return statusNum !== ShippingStatus.Completed && statusNum !== ShippingStatus.Cancelled;
  };

  if (error) {
    return (
      <Alert 
        severity="error" 
        action={
          <IconButton color="inherit" size="small" onClick={() => refetch()} aria-label="Retry loading shipping operations">
            <RefreshIcon />
          </IconButton>
        }
      >
        Error loading shipping operations: {error.message}
      </Alert>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="between" alignItems="center" mb={3}>
        <Typography variant="h5" component="h2">
          Shipping Operations
        </Typography>
        <Box display="flex" gap={1}>
          <Button
            variant="outlined"
            startIcon={<FilterIcon />}
            onClick={() => setShowFilters(!showFilters)}
          >
            Filters
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={onCreate}
          >
            New Shipping Operation
          </Button>
        </Box>
      </Box>

      {/* Summary Cards */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Total Operations
              </Typography>
              <Typography variant="h4">
                {totalCount}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                In Transit
              </Typography>
              <Typography variant="h4" color="info.main">
                {operations.filter(op => getStatusFromString(op.status) === ShippingStatus.InTransit).length}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Loading/Discharging
              </Typography>
              <Typography variant="h4" color="warning.main">
                {operations.filter(op => {
                  const status = getStatusFromString(op.status);
                  return status === ShippingStatus.Loading || status === ShippingStatus.Discharging;
                }).length}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="textSecondary" gutterBottom>
                Completed
              </Typography>
              <Typography variant="h4" color="success.main">
                {operations.filter(op => getStatusFromString(op.status) === ShippingStatus.Completed).length}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters */}
      {showFilters && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  size="small"
                  label="Shipping Number"
                  value={filters.shippingNumber || ''}
                  onChange={(e) => handleFilterChange('shippingNumber', e.target.value)}
                />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  size="small"
                  label="Vessel Name"
                  value={filters.vesselName || ''}
                  onChange={(e) => handleFilterChange('vesselName', e.target.value)}
                />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <FormControl fullWidth size="small">
                  <InputLabel>Status</InputLabel>
                  <Select
                    value={filters.status || ''}
                    label="Status"
                    onChange={(e) => handleFilterChange('status', e.target.value)}
                  >
                    <MenuItem value="">All</MenuItem>
                    {SHIPPING_STATUS_OPTIONS.map((status) => (
                      <MenuItem key={status.value} value={status.value}>
                        {status.label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Button variant="outlined" onClick={handleClearFilters} fullWidth>
                  Clear Filters
                </Button>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Loading indicator */}
      {isLoading && <LinearProgress sx={{ mb: 2 }} />}

      {/* Table */}
      <TableContainer component={Paper}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Shipping Number</TableCell>
              <TableCell>Contract</TableCell>
              <TableCell>Vessel</TableCell>
              <TableCell>Route</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>ETA</TableCell>
              <TableCell>Created</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {operations.map((operation) => (
              <TableRow key={operation.id} hover>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium">
                    {operation.shippingNumber}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {operation.contractNumber}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <ShippingIcon sx={{ mr: 1, color: 'text.secondary' }} />
                    <Typography variant="body2">
                      {operation.vesselName}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {operation.loadPort} → {operation.dischargePort}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2">
                    {operation.plannedQuantity.toLocaleString()} {operation.plannedQuantityUnit}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip
                    label={operation.status}
                    color={shippingApi.getStatusColor(getStatusFromString(operation.status)) as any}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {operation.loadPortETA 
                      ? format(new Date(operation.loadPortETA), 'MMM dd, yyyy')
                      : 'TBD'
                    }
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {format(new Date(operation.createdAt), 'MMM dd, yyyy')}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Tooltip title="View">
                    <IconButton size="small" onClick={() => onView(operation.id)}>
                      <ViewIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Edit">
                    <IconButton size="small" onClick={() => onEdit(operation.id)}>
                      <EditIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="More actions">
                    <IconButton size="small" onClick={(e) => handleMenuClick(e, operation.id)}>
                      <MoreIcon />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {operations.length === 0 && !isLoading && (
              <TableRow>
                <TableCell colSpan={9} align="center">
                  <Typography variant="body2" color="textSecondary" py={4}>
                    No shipping operations found
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <TablePagination
        rowsPerPageOptions={[10, 25, 50]}
        component="div"
        count={totalCount}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />

      {/* Context Menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        {selectedOperation && (
          <>
            {canStartLoading(operations.find(op => op.id === selectedOperation)?.status || '') && (
              <MenuItem onClick={() => selectedOperation && handleStartLoading(selectedOperation)}>
                <ListItemIcon>
                  <StartIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>Start Loading</ListItemText>
              </MenuItem>
            )}
            {canCompleteLoading(operations.find(op => op.id === selectedOperation)?.status || '') && (
              <MenuItem onClick={() => selectedOperation && handleCompleteLoading(selectedOperation)}>
                <ListItemIcon>
                  <CompleteIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>Complete Loading</ListItemText>
              </MenuItem>
            )}
            {canCompleteDischarge(operations.find(op => op.id === selectedOperation)?.status || '') && (
              <MenuItem onClick={() => selectedOperation && handleCompleteDischarge(selectedOperation)}>
                <ListItemIcon>
                  <CompleteIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>Complete Discharge</ListItemText>
              </MenuItem>
            )}
            {canCancel(operations.find(op => op.id === selectedOperation)?.status || '') && (
              <MenuItem onClick={() => selectedOperation && handleCancelClick(selectedOperation)}>
                <ListItemIcon>
                  <CancelIcon fontSize="small" />
                </ListItemIcon>
                <ListItemText>Cancel Operation</ListItemText>
              </MenuItem>
            )}
          </>
        )}
      </Menu>

      {/* Cancel Dialog */}
      <Dialog open={cancelDialog.open} onClose={() => setCancelDialog({ open: false, operationId: null })}>
        <DialogTitle>Cancel Shipping Operation</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Cancellation Reason"
            fullWidth
            multiline
            rows={3}
            variant="outlined"
            value={cancelReason}
            onChange={(e) => setCancelReason(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelDialog({ open: false, operationId: null })}>
            Cancel
          </Button>
          <Button onClick={handleCancelSubmit} variant="contained" color="error">
            Cancel Operation
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};