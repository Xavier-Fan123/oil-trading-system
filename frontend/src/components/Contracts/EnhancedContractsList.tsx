import React, { useState, useMemo } from 'react';
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
  Checkbox,
  Menu,
  Divider,
  ListItemIcon,
  ListItemText,
  Badge,
  Fab,
  Zoom,
  Alert,
  LinearProgress,
} from '@mui/material';
import {
  Edit as EditIcon,
  Visibility as ViewIcon,
  PlayArrow as ActivateIcon,
  Add as AddIcon,
  FilterList as FilterIcon,
  Check,
  Close,
  Archive,
  Assignment,
  FileCopy,
  Gavel,
  Cancel,
  Send,
  Refresh,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { usePurchaseContracts, useTradingPartners, useProducts } from '@/hooks/useContracts';
import { useTags } from '@/hooks/useTags';
import {
  ContractStatus,
  QuantityUnit,
  ContractFilters,
  PurchaseContractListDto,
} from '@/types/contracts';
import { ContractBatchActions } from './ContractBatchActions';

interface EnhancedContractsListProps {
  onEdit: (contractId: string) => void;
  onView: (contractId: string) => void;
  onCreate: () => void;
}

const getStatusColor = (status: ContractStatus): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (status) {
    case ContractStatus.Draft: return 'default';
    case ContractStatus.PendingApproval: return 'warning';
    case ContractStatus.Active: return 'success';
    case ContractStatus.Completed: return 'info';
    case ContractStatus.Cancelled: return 'error';
    default: return 'default';
  }
};

const getStatusLabel = (status: ContractStatus): string => {
  switch (status) {
    case ContractStatus.Draft: return 'Draft';
    case ContractStatus.PendingApproval: return 'Pending Approval';
    case ContractStatus.Active: return 'Active';
    case ContractStatus.Completed: return 'Completed';
    case ContractStatus.Cancelled: return 'Cancelled';
    default: return 'Unknown';
  }
};

const getQuantityUnitLabel = (unit: QuantityUnit): string => {
  switch (unit) {
    case QuantityUnit.MT: return 'MT';
    case QuantityUnit.BBL: return 'BBL';
    case QuantityUnit.GAL: return 'GAL';
    default: return 'Unknown';
  }
};

export const EnhancedContractsList: React.FC<EnhancedContractsListProps> = ({ onEdit, onView, onCreate }) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [showFilters, setShowFilters] = useState(false);
  const [filters, setFilters] = useState<ContractFilters>({
    pageNumber: 1,
    pageSize: 25,
  });

  // Batch operations state
  const [selectedContracts, setSelectedContracts] = useState<string[]>([]);
  const [batchActionAnchor, setBatchActionAnchor] = useState<null | HTMLElement>(null);
  const [batchActionDialog, setBatchActionDialog] = useState<{
    open: boolean;
    action: string;
  }>({ open: false, action: '' });

  const { data: contractsData, isLoading, error, refetch } = usePurchaseContracts(filters);
  const { data: tradingPartners } = useTradingPartners();
  const { data: products } = useProducts();
  const { data: _tags } = useTags();

  const contracts = contractsData?.items || [];
  const totalCount = contractsData?.totalCount || 0;

  // Get selected contract objects
  const selectedContractObjects = useMemo(() => {
    return contracts.filter(contract => selectedContracts.includes(contract.id));
  }, [contracts, selectedContracts]);

  // Batch action handlers
  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      setSelectedContracts(contracts.map(contract => contract.id));
    } else {
      setSelectedContracts([]);
    }
  };

  const handleSelectContract = (contractId: string) => {
    setSelectedContracts(prev => 
      prev.includes(contractId)
        ? prev.filter(id => id !== contractId)
        : [...prev, contractId]
    );
  };

  const handleBatchAction = (action: string) => {
    setBatchActionDialog({ open: true, action });
    setBatchActionAnchor(null);
  };

  const handleBatchActionConfirm = async (action: string, params?: any) => {
    try {
      console.log('Executing batch action:', action, 'on contracts:', selectedContracts, 'with params:', params);
      
      // Here you would call the appropriate API endpoints
      switch (action) {
        case 'submit-approval':
          // await submitContractsForApproval(selectedContracts, params);
          break;
        case 'approve':
          // await approveContracts(selectedContracts, params);
          break;
        case 'reject':
          // await rejectContracts(selectedContracts, params);
          break;
        case 'cancel':
          // await cancelContracts(selectedContracts, params);
          break;
        case 'assign-trader':
          // await assignTrader(selectedContracts, params);
          break;
        case 'update-status':
          // await updateContractStatus(selectedContracts, params);
          break;
        case 'archive':
          // await archiveContracts(selectedContracts, params);
          break;
        case 'export':
          // await exportContracts(selectedContracts, params);
          break;
      }
      
      // Refresh data and clear selection
      await refetch();
      setSelectedContracts([]);
      
    } catch (error: any) {
      throw new Error(error.message || 'Batch action failed');
    }
  };

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
    setFilters(prev => ({
      ...prev,
      pageNumber: newPage + 1,
    }));
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    const newRowsPerPage = parseInt(event.target.value, 10);
    setRowsPerPage(newRowsPerPage);
    setPage(0);
    setFilters(prev => ({
      ...prev,
      pageNumber: 1,
      pageSize: newRowsPerPage,
    }));
  };

  const handleFilterChange = (field: keyof ContractFilters, value: any) => {
    setFilters(prev => ({
      ...prev,
      [field]: value,
      pageNumber: 1, // Reset to first page when filtering
    }));
    setPage(0);
  };

  const getBatchActions = () => {
    const actions = [
      { key: 'submit-approval', label: 'Submit for Approval', icon: <Send />, color: 'primary' },
      { key: 'approve', label: 'Approve', icon: <Check />, color: 'success' },
      { key: 'reject', label: 'Reject', icon: <Close />, color: 'error' },
      { key: 'cancel', label: 'Cancel', icon: <Cancel />, color: 'warning' },
      { key: 'assign-trader', label: 'Assign Trader', icon: <Assignment />, color: 'info' },
      { key: 'update-status', label: 'Update Status', icon: <Gavel />, color: 'default' },
      { key: 'archive', label: 'Archive', icon: <Archive />, color: 'default' },
      { key: 'export', label: 'Export', icon: <FileCopy />, color: 'default' },
    ];

    // Filter actions based on selected contracts' status
    const selectedStatuses = selectedContractObjects.map(c => c.status);
    const uniqueStatuses = [...new Set(selectedStatuses)];

    return actions.filter(action => {
      switch (action.key) {
        case 'submit-approval':
          return uniqueStatuses.every(status => status === ContractStatus.Draft);
        case 'approve':
        case 'reject':
          return uniqueStatuses.every(status => status === ContractStatus.PendingApproval);
        case 'cancel':
          return uniqueStatuses.every(status => 
            status !== ContractStatus.Completed && status !== ContractStatus.Cancelled
          );
        default:
          return true;
      }
    });
  };

  if (isLoading) {
    return (
      <Box>
        <LinearProgress />
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
          <Typography>Loading contracts...</Typography>
        </Box>
      </Box>
    );
  }

  if (error) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <Alert severity="error">
          Error loading contracts: {error.message}
          <Button onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const isIndeterminate = selectedContracts.length > 0 && selectedContracts.length < contracts.length;
  const isAllSelected = contracts.length > 0 && selectedContracts.length === contracts.length;

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Purchase Contracts
        </Typography>
        <Box display="flex" gap={2} alignItems="center">
          <Badge badgeContent={selectedContracts.length} color="primary" invisible={selectedContracts.length === 0}>
            <Button
              variant="outlined"
              startIcon={<Gavel />}
              onClick={(e) => setBatchActionAnchor(e.currentTarget)}
              disabled={selectedContracts.length === 0}
            >
              Batch Actions
            </Button>
          </Badge>
          <Button
            variant="outlined"
            startIcon={<FilterIcon />}
            onClick={() => setShowFilters(!showFilters)}
          >
            Filters
          </Button>
          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={() => refetch()}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={onCreate}
          >
            New Contract
          </Button>
        </Box>
      </Box>

      {/* Batch Actions Menu */}
      <Menu
        anchorEl={batchActionAnchor}
        open={Boolean(batchActionAnchor)}
        onClose={() => setBatchActionAnchor(null)}
        PaperProps={{ sx: { minWidth: 200 } }}
      >
        <Typography variant="subtitle2" sx={{ px: 2, py: 1, color: 'text.secondary' }}>
          Actions for {selectedContracts.length} contract(s)
        </Typography>
        <Divider />
        {getBatchActions().map((action) => (
          <MenuItem key={action.key} onClick={() => handleBatchAction(action.key)}>
            <ListItemIcon>{action.icon}</ListItemIcon>
            <ListItemText>{action.label}</ListItemText>
          </MenuItem>
        ))}
      </Menu>

      {/* Selection Info */}
      {selectedContracts.length > 0 && (
        <Alert 
          severity="info" 
          sx={{ mb: 2 }}
          action={
            <Button size="small" onClick={() => setSelectedContracts([])}>
              Clear Selection
            </Button>
          }
        >
          {selectedContracts.length} contract(s) selected
        </Alert>
      )}

      {/* Filters */}
      {showFilters && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <FormControl fullWidth size="small">
                  <InputLabel>Status</InputLabel>
                  <Select
                    value={filters.status ?? ''}
                    label="Status"
                    onChange={(e) => handleFilterChange('status', e.target.value || undefined)}
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
                <FormControl fullWidth size="small">
                  <InputLabel>Supplier</InputLabel>
                  <Select
                    value={filters.supplierId ?? ''}
                    label="Supplier"
                    onChange={(e) => handleFilterChange('supplierId', e.target.value || undefined)}
                  >
                    <MenuItem value="">All</MenuItem>
                    {tradingPartners?.map(partner => (
                      <MenuItem key={partner.id} value={partner.id}>
                        {partner.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <FormControl fullWidth size="small">
                  <InputLabel>Product</InputLabel>
                  <Select
                    value={filters.productId ?? ''}
                    label="Product"
                    onChange={(e) => handleFilterChange('productId', e.target.value || undefined)}
                  >
                    <MenuItem value="">All</MenuItem>
                    {products?.map(product => (
                      <MenuItem key={product.id} value={product.id}>
                        {product.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <TextField
                  fullWidth
                  size="small"
                  label="Laycan Start From"
                  type="date"
                  value={filters.laycanStart ? format(filters.laycanStart, 'yyyy-MM-dd') : ''}
                  onChange={(e) => handleFilterChange('laycanStart', e.target.value ? new Date(e.target.value) : undefined)}
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Contracts Table */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox">
                <Checkbox
                  indeterminate={isIndeterminate}
                  checked={isAllSelected}
                  onChange={handleSelectAll}
                />
              </TableCell>
              <TableCell>System Contract #</TableCell>
              <TableCell>External Contract #</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Supplier</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell>Laycan</TableCell>
              <TableCell>Created</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {contracts.map((contract: PurchaseContractListDto) => (
              <TableRow 
                key={contract.id} 
                hover 
                selected={selectedContracts.includes(contract.id)}
              >
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={selectedContracts.includes(contract.id)}
                    onChange={() => handleSelectContract(contract.id)}
                  />
                </TableCell>
                <TableCell>
                  <Typography variant="body2" fontWeight="medium">
                    {contract.contractNumber}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2" color={contract.externalContractNumber ? "text.primary" : "text.secondary"}>
                    {contract.externalContractNumber || "—"}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Chip
                    label={getStatusLabel(contract.status)}
                    color={getStatusColor(contract.status)}
                    size="small"
                  />
                </TableCell>
                <TableCell>{contract.supplierName}</TableCell>
                <TableCell>{contract.productName}</TableCell>
                <TableCell align="right">
                  <Typography variant="body2">
                    {contract.quantity.toLocaleString()} {getQuantityUnitLabel(contract.quantityUnit)}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {format(new Date(contract.laycanStart), 'MMM dd')} - {format(new Date(contract.laycanEnd), 'MMM dd, yyyy')}
                  </Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2">
                    {format(new Date(contract.createdAt), 'MMM dd, yyyy')}
                  </Typography>
                </TableCell>
                <TableCell align="center">
                  <Tooltip title="View Details">
                    <IconButton size="small" onClick={() => onView(contract.id)}>
                      <ViewIcon />
                    </IconButton>
                  </Tooltip>
                  {(contract.status === ContractStatus.Draft || contract.status === ContractStatus.PendingApproval) && (
                    <Tooltip title="Edit Contract">
                      <IconButton size="small" onClick={() => onEdit(contract.id)}>
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                  )}
                  {contract.status === ContractStatus.PendingApproval && (
                    <Tooltip title="Activate Contract">
                      <IconButton size="small" color="success">
                        <ActivateIcon />
                      </IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {contracts.length === 0 && (
              <TableRow>
                <TableCell colSpan={10} align="center">
                  <Typography variant="body1" color="textSecondary" py={4}>
                    No contracts found
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      <TablePagination
        rowsPerPageOptions={[10, 25, 50, 100]}
        component="div"
        count={totalCount}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />

      {/* Batch Actions Dialog */}
      <ContractBatchActions
        open={batchActionDialog.open}
        action={batchActionDialog.action}
        selectedContracts={selectedContractObjects}
        onClose={() => setBatchActionDialog({ open: false, action: '' })}
        onConfirm={handleBatchActionConfirm}
      />

      {/* Floating Action Button for Quick Actions */}
      <Zoom in={selectedContracts.length > 0}>
        <Fab
          color="primary"
          sx={{ position: 'fixed', bottom: 16, right: 16 }}
          onClick={(e) => setBatchActionAnchor(e.currentTarget)}
        >
          <Badge badgeContent={selectedContracts.length} color="secondary">
            <Gavel />
          </Badge>
        </Fab>
      </Zoom>
    </Box>
  );
};