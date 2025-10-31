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
  Autocomplete,
} from '@mui/material';
import {
  Edit as EditIcon,
  Visibility as ViewIcon,
  PlayArrow as ActivateIcon,
  Add as AddIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { usePurchaseContracts, useTradingPartners, useProducts } from '@/hooks/useContracts';
import { useTags } from '@/hooks/useTags';
import {
  ContractStatus,
  QuantityUnit,
  ContractFilters,
  PurchaseContractListDto
} from '@/types/contracts';

interface ContractsListProps {
  onEdit: (contractId: string) => void;
  onView: (contractId: string) => void;
  onCreate: () => void;
  onActivate?: (contractId: string) => Promise<void>;
}

// Convert string status from API to numeric enum (backend returns strings due to JsonStringEnumConverter)
const normalizeStatus = (status: any): ContractStatus => {
  if (typeof status === 'string') {
    switch (status) {
      case 'Draft':
        return ContractStatus.Draft;
      case 'PendingApproval':
        return ContractStatus.PendingApproval;
      case 'Active':
        return ContractStatus.Active;
      case 'Completed':
        return ContractStatus.Completed;
      case 'Cancelled':
        return ContractStatus.Cancelled;
      default:
        return ContractStatus.Draft;
    }
  }
  return status;
};

const getStatusColor = (status: ContractStatus | string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  const normalizedStatus = normalizeStatus(status);
  switch (normalizedStatus) {
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

const getStatusLabel = (status: ContractStatus | string): string => {
  const normalizedStatus = normalizeStatus(status);
  switch (normalizedStatus) {
    case ContractStatus.Draft:
      return 'Draft';
    case ContractStatus.PendingApproval:
      return 'Pending Approval';
    case ContractStatus.Active:
      return 'Active';
    case ContractStatus.Completed:
      return 'Completed';
    case ContractStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
};

const getQuantityUnitLabel = (unit: QuantityUnit | string): string => {
  // Handle string values from backend JsonStringEnumConverter
  if (typeof unit === 'string') {
    return unit; // Return as-is since backend returns "MT", "BBL", "GAL", "LOTS"
  }

  // Handle numeric enum values
  switch (unit) {
    case QuantityUnit.MT:
      return 'MT';
    case QuantityUnit.BBL:
      return 'BBL';
    case QuantityUnit.GAL:
      return 'GAL';
    default:
      return 'Unknown';
  }
};

export const ContractsList: React.FC<ContractsListProps> = ({ onEdit, onView, onCreate, onActivate }) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [showFilters, setShowFilters] = useState(false);
  const [filters, setFilters] = useState<ContractFilters>({
    pageNumber: 1,
    pageSize: 25,
  });

  const { data: contractsData, isLoading, error } = usePurchaseContracts(filters);
  const { data: tradingPartners } = useTradingPartners();
  const { data: products } = useProducts();
  const { data: tags } = useTags();

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

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <Typography>Loading contracts...</Typography>
      </Box>
    );
  }

  if (error) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <Typography color="error">Error loading contracts: {error.message}</Typography>
      </Box>
    );
  }

  const contracts = contractsData?.items || [];
  const totalCount = contractsData?.totalCount || 0;

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Purchase Contracts
        </Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<FilterIcon />}
            onClick={() => setShowFilters(!showFilters)}
            sx={{ mr: 2 }}
          >
            Filters
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
              <Grid item xs={12} sm={6} md={3}>
                <Autocomplete
                  multiple
                  size="small"
                  options={tags?.filter(tag => tag.isActive) || []}
                  getOptionLabel={(option) => option.name}
                  value={tags?.filter(tag => filters.tagIds?.includes(tag.id)) || []}
                  onChange={(_, newValue) => {
                    const tagIds = newValue.map(tag => tag.id);
                    handleFilterChange('tagIds', tagIds.length > 0 ? tagIds : undefined);
                  }}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label="Tags"
                      placeholder="Filter by tags..."
                    />
                  )}
                  renderOption={(props, option) => (
                    <li {...props}>
                      <Chip
                        label={option.name}
                        size="small"
                        sx={{ 
                          backgroundColor: option.color,
                          color: 'white',
                          mr: 1
                        }}
                      />
                      {option.name} ({option.categoryDisplayName})
                    </li>
                  )}
                  renderTags={(value, getTagProps) =>
                    value.map((option, index) => (
                      <Chip
                        {...getTagProps({ index })}
                        key={option.id}
                        label={option.name}
                        size="small"
                        sx={{ 
                          backgroundColor: option.color,
                          color: 'white'
                        }}
                      />
                    ))
                  }
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
              <TableCell>System Contract #</TableCell>
              <TableCell>External Contract #</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Supplier</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Quantity</TableCell>
              <TableCell>Tags</TableCell>
              <TableCell>Laycan</TableCell>
              <TableCell>Created</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {contracts.map((contract: PurchaseContractListDto) => (
              <TableRow key={contract.id} hover>
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
                  <Box display="flex" flexWrap="wrap" gap={0.5}>
                    {/* Tags will be fetched and displayed here - placeholder for now */}
                    <Typography variant="caption" color="text.secondary">
                      No tags
                    </Typography>
                  </Box>
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
                  {(normalizeStatus(contract.status) === ContractStatus.Draft ||
                    normalizeStatus(contract.status) === ContractStatus.PendingApproval) && (
                    <Tooltip title="Edit Contract">
                      <IconButton size="small" onClick={() => onEdit(contract.id)}>
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                  )}
                  {(normalizeStatus(contract.status) === ContractStatus.Draft ||
                    normalizeStatus(contract.status) === ContractStatus.PendingApproval) && onActivate && (
                    <Tooltip title="Activate Contract">
                      <IconButton
                        size="small"
                        color="success"
                        onClick={() => onActivate(contract.id)}
                      >
                        <ActivateIcon />
                      </IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {contracts.length === 0 && (
              <TableRow>
                <TableCell colSpan={9} align="center">
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
    </Box>
  );
};