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
  DialogActions
} from '@mui/material';
import {
  Edit as EditIcon,
  Visibility as ViewIcon,
  MoreVert as MoreIcon,
  Add as AddIcon,
  FilterList as FilterIcon,
  Check as ApproveIcon,
  Close as RejectIcon,
  Delete as DeleteIcon
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useSalesContracts, useSalesContractsSummary, useApproveSalesContract, useRejectSalesContract, useDeleteSalesContract } from '@/hooks/useSalesContracts';
import {
  ContractStatus,
  SalesContractFilters
} from '@/types/salesContracts';

interface SalesContractsListProps {
  onEdit: (contractId: string) => void;
  onView: (contractId: string) => void;
  onCreate: () => void;
}

const getStatusColor = (status: ContractStatus): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (status) {
    case ContractStatus.Draft:
      return 'default';
    case ContractStatus.PendingApproval:
      return 'warning';
    case ContractStatus.Active:
      return 'success';
    case ContractStatus.Completed:
      return 'info';
    case ContractStatus.Cancelled:
      return 'error';
    default:
      return 'default';
  }
};

const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
};

const getQuantityUnitLabel = (unit: number | string): string => {
  // Handle string values from backend JsonStringEnumConverter
  if (typeof unit === 'string') {
    return unit; // Return as-is since backend returns "MT", "BBL", "GAL", "LOTS"
  }

  // Handle numeric enum values
  switch (unit) {
    case 1:
      return 'MT';
    case 2:
      return 'BBL';
    case 3:
      return 'GAL';
    case 4:
      return 'LOTS';
    default:
      return `Unknown (${unit})`;
  }
};

export const SalesContractsList: React.FC<SalesContractsListProps> = ({ 
  onEdit, 
  onView, 
  onCreate 
}) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [filters, setFilters] = useState<SalesContractFilters>({});
  const [showFilters, setShowFilters] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedContract, setSelectedContract] = useState<string | null>(null);
  const [rejectDialog, setRejectDialog] = useState<{ open: boolean; contractId: string | null }>({ 
    open: false, 
    contractId: null 
  });
  const [rejectReason, setRejectReason] = useState('');

  // Fetch data
  const { data: contractsData, isLoading, error } = useSalesContracts({
    ...filters,
    pageNumber: page + 1,
    pageSize: rowsPerPage
  });

  const { data: summary } = useSalesContractsSummary();

  // Mutations
  const approveMutation = useApproveSalesContract();
  const rejectMutation = useRejectSalesContract();
  const deleteMutation = useDeleteSalesContract();

  const contracts = contractsData?.items || [];
  const totalCount = contractsData?.totalCount || 0;

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleFilterChange = (key: keyof SalesContractFilters, value: any) => {
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

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>, contractId: string) => {
    setAnchorEl(event.currentTarget);
    setSelectedContract(contractId);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedContract(null);
  };

  const handleApprove = (contractId: string) => {
    approveMutation.mutate(contractId);
    handleMenuClose();
  };

  const handleRejectClick = (contractId: string) => {
    setRejectDialog({ open: true, contractId });
    handleMenuClose();
  };

  const handleRejectSubmit = () => {
    if (rejectDialog.contractId && rejectReason.trim()) {
      rejectMutation.mutate({ id: rejectDialog.contractId, reason: rejectReason.trim() });
      setRejectDialog({ open: false, contractId: null });
      setRejectReason('');
    }
  };

  const handleDelete = (contractId: string) => {
    if (confirm('Are you sure you want to delete this contract?')) {
      deleteMutation.mutate(contractId);
    }
    handleMenuClose();
  };

  if (error) {
    return (
      <Box>
        <Typography color="error">
          Error loading sales contracts: {error.message}
        </Typography>
      </Box>
    );
  }

  return (
    <Box>
      {/* Summary Cards */}
      {summary && (
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Total Contracts
                </Typography>
                <Typography variant="h5" component="div">
                  {summary.totalContracts}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Total Value
                </Typography>
                <Typography variant="h5" component="div">
                  {formatCurrency(summary.totalValue)}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Estimated Profit
                </Typography>
                <Typography variant="h5" component="div" color="success.main">
                  {formatCurrency(summary.estimatedProfit)}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>
                  Avg Margin
                </Typography>
                <Typography variant="h5" component="div">
                  {((summary.estimatedProfit / summary.totalValue) * 100).toFixed(1)}%
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Header */}
      <Box display="flex" justifyContent="between" alignItems="center" mb={3}>
        <Typography variant="h5" component="h2">
          Sales Contracts
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
            New Sales Contract
          </Button>
        </Box>
      </Box>

      {/* Filters */}
      {showFilters && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <FormControl fullWidth size="small">
                  <InputLabel>Status</InputLabel>
                  <Select
                    value={filters.status || ''}
                    label="Status"
                    onChange={(e) => handleFilterChange('status', e.target.value)}
                  >
                    <MenuItem value="">All</MenuItem>
                    <MenuItem value={ContractStatus.Draft}>Draft</MenuItem>
                    <MenuItem value={ContractStatus.PendingApproval}>Pending Approval</MenuItem>
                    <MenuItem value={ContractStatus.Active}>Active</MenuItem>
                    <MenuItem value={ContractStatus.Completed}>Completed</MenuItem>
                    <MenuItem value={ContractStatus.Cancelled}>Cancelled</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  size="small"
                  label="Min Value"
                  type="number"
                  value={filters.minValue || ''}
                  onChange={(e) => handleFilterChange('minValue', parseFloat(e.target.value))}
                />
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  size="small"
                  label="Max Value"
                  type="number"
                  value={filters.maxValue || ''}
                  onChange={(e) => handleFilterChange('maxValue', parseFloat(e.target.value))}
                />
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

      {/* Table */}
      <TableContainer component={Paper}>
        <Table stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Contract Number</TableCell>
              <TableCell>Customer</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell align="right">Total Value</TableCell>
              <TableCell align="right">Est. Profit</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Delivery</TableCell>
              <TableCell>Created</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {contracts.map((contract) => (
              <TableRow key={contract.id} hover>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium" color={contract.externalContractNumber ? "text.primary" : "text.secondary"}>
                    {contract.externalContractNumber || contract.contractNumber || "—"}
                  </Typography>
                </TableCell>
                <TableCell>{contract.customerName}</TableCell>
                <TableCell>{contract.productName}</TableCell>
                <TableCell align="right">
                  {contract.quantity.toLocaleString()} {getQuantityUnitLabel(contract.quantityUnit)}
                </TableCell>
                <TableCell align="right">
                  {contract.contractValue ? formatCurrency(contract.contractValue) : '-'}
                </TableCell>
                <TableCell align="right">
                  <Typography color="success.main">
                    {contract.estimatedProfit ? formatCurrency(contract.estimatedProfit) : '-'}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip
                    label={ContractStatus[contract.status]}
                    color={getStatusColor(contract.status)}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  {contract.laycanStart && contract.laycanEnd ?
                    `${format(new Date(contract.laycanStart), 'MMM dd')} - ${format(new Date(contract.laycanEnd), 'MMM dd')}`
                    : '-'
                  }
                </TableCell>
                <TableCell>
                  {format(new Date(contract.createdAt), 'MMM dd, yyyy')}
                </TableCell>
                <TableCell align="center">
                  <Tooltip title="View">
                    <IconButton size="small" onClick={() => onView(contract.id)}>
                      <ViewIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Edit">
                    <IconButton size="small" onClick={() => onEdit(contract.id)}>
                      <EditIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="More actions">
                    <IconButton size="small" onClick={(e) => handleMenuClick(e, contract.id)}>
                      <MoreIcon />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {contracts.length === 0 && !isLoading && (
              <TableRow>
                <TableCell colSpan={10} align="center">
                  <Typography variant="body2" color="textSecondary" py={4}>
                    No sales contracts found
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
        <MenuItem onClick={() => selectedContract && handleApprove(selectedContract)}>
          <ListItemIcon>
            <ApproveIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Approve</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => selectedContract && handleRejectClick(selectedContract)}>
          <ListItemIcon>
            <RejectIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Reject</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => selectedContract && handleDelete(selectedContract)}>
          <ListItemIcon>
            <DeleteIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Delete</ListItemText>
        </MenuItem>
      </Menu>

      {/* Reject Dialog */}
      <Dialog open={rejectDialog.open} onClose={() => setRejectDialog({ open: false, contractId: null })}>
        <DialogTitle>Reject Contract</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Rejection Reason"
            fullWidth
            multiline
            rows={3}
            variant="outlined"
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRejectDialog({ open: false, contractId: null })}>
            Cancel
          </Button>
          <Button onClick={handleRejectSubmit} variant="contained" color="error">
            Reject Contract
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};