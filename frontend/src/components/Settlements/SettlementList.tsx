import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  IconButton,
  Tooltip,
  Alert,
  TableSortLabel,
  TextField,
  InputAdornment,
  CircularProgress,
  TablePagination,
  Grid,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  Search as SearchIcon,
  FileDownload as ExportIcon,
  FilterList as FilterIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import {
  ContractSettlementListDto,
  ContractSettlementStatus,
  ContractSettlementStatusLabels,
  getSettlementStatusColor
} from '@/types/settlement';
import { settlementApi } from '@/services/settlementApi';

interface SettlementListProps {
  settlements: ContractSettlementListDto[];
  searchTerm: string;
  onSettlementSelect: (settlementId: string) => void;
  onCreateNew: () => void;
  onBackToSearch: () => void;
  initialStatusFilter?: string;
}

type SortField = 'documentDate' | 'contractNumber' | 'externalContractNumber' | 'totalSettlementAmount' | 'status' | 'createdDate';
type SortDirection = 'asc' | 'desc';

// Status filter options
const STATUS_FILTERS = [
  { label: 'All', value: undefined },
  { label: 'Draft', value: ContractSettlementStatus.Draft },
  { label: 'Data Entered', value: ContractSettlementStatus.DataEntered },
  { label: 'Calculated', value: ContractSettlementStatus.Calculated },
  { label: 'Reviewed', value: ContractSettlementStatus.Reviewed },
  { label: 'Approved', value: ContractSettlementStatus.Approved },
  { label: 'Finalized', value: ContractSettlementStatus.Finalized },
];

export const SettlementList: React.FC<SettlementListProps> = ({
  settlements: searchResults,
  searchTerm,
  onSettlementSelect,
  onCreateNew,
  onBackToSearch,
  initialStatusFilter
}) => {
  const [sortField, setSortField] = useState<SortField>('createdDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [filterTerm, setFilterTerm] = useState('');

  // Self-loading state
  const [allSettlements, setAllSettlements] = useState<ContractSettlementListDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [totalCount, setTotalCount] = useState(0);
  const [statusFilter, setStatusFilter] = useState<ContractSettlementStatus | undefined>(() => {
    if (initialStatusFilter) {
      const match = Object.entries(ContractSettlementStatusLabels).find(
        ([, label]) => label === initialStatusFilter
      );
      return match ? Number(match[0]) as ContractSettlementStatus : undefined;
    }
    return undefined;
  });

  // Determine if we're showing search results or self-loaded data
  const isSearchMode = searchResults.length > 0 && searchTerm !== '';

  // Load all settlements when not in search mode
  const loadSettlements = useCallback(async () => {
    if (isSearchMode) return;

    setLoading(true);
    setError(null);
    try {
      const result = await settlementApi.getSettlements({
        pageNumber: page + 1,
        pageSize: rowsPerPage,
        status: statusFilter,
      });
      setAllSettlements(result.data || []);
      setTotalCount(result.totalCount || 0);
    } catch (err) {
      console.error('Failed to load settlements:', err);
      setError('Failed to load settlements. The backend may be unavailable.');
      setAllSettlements([]);
    } finally {
      setLoading(false);
    }
  }, [isSearchMode, page, rowsPerPage, statusFilter]);

  useEffect(() => {
    loadSettlements();
  }, [loadSettlements]);

  // Use search results or self-loaded data
  const settlements = isSearchMode ? searchResults : allSettlements;

  // Status summary counts
  const statusCounts = React.useMemo(() => {
    const counts: Record<string, number> = {};
    settlements.forEach(s => {
      const key = s.displayStatus || s.status || 'Unknown';
      counts[key] = (counts[key] || 0) + 1;
    });
    return counts;
  }, [settlements]);

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('desc');
    }
  };

  const handleStatusFilter = (status: ContractSettlementStatus | undefined) => {
    setStatusFilter(status);
    setPage(0);
  };

  const sortedAndFilteredSettlements = React.useMemo(() => {
    let filtered = settlements;

    // Apply local filter
    if (filterTerm.trim()) {
      const term = filterTerm.toLowerCase();
      filtered = settlements.filter(settlement =>
        (settlement.externalContractNumber || '').toLowerCase().includes(term) ||
        (settlement.contractNumber || '').toLowerCase().includes(term) ||
        (settlement.documentNumber || '').toLowerCase().includes(term) ||
        (settlement.status || '').toLowerCase().includes(term) ||
        (settlement.createdBy || '').toLowerCase().includes(term)
      );
    }

    // Apply sorting
    return [...filtered].sort((a, b) => {
      let aValue: any;
      let bValue: any;

      switch (sortField) {
        case 'documentDate':
          aValue = new Date(a.documentDate);
          bValue = new Date(b.documentDate);
          break;
        case 'contractNumber':
          aValue = a.contractNumber;
          bValue = b.contractNumber;
          break;
        case 'externalContractNumber':
          aValue = a.externalContractNumber;
          bValue = b.externalContractNumber;
          break;
        case 'totalSettlementAmount':
          aValue = a.totalSettlementAmount;
          bValue = b.totalSettlementAmount;
          break;
        case 'status':
          aValue = a.status;
          bValue = b.status;
          break;
        case 'createdDate':
          aValue = new Date(a.createdDate);
          bValue = new Date(b.createdDate);
          break;
        default:
          aValue = a.createdDate;
          bValue = b.createdDate;
      }

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
  }, [settlements, sortField, sortDirection, filterTerm]);

  const handleExport = () => {
    const headers = [
      'Contract Number', 'External Contract Number', 'Document Number',
      'Document Type', 'Document Date', 'Quantity (MT)', 'Quantity (BBL)',
      'Settlement Amount', 'Currency', 'Status', 'Created Date', 'Created By', 'Charges Count'
    ];

    const csvContent = [
      headers.join(','),
      ...sortedAndFilteredSettlements.map(settlement =>
        [
          `"${settlement.contractNumber}"`,
          `"${settlement.externalContractNumber}"`,
          `"${settlement.documentNumber || ''}"`,
          `"${settlement.documentType}"`,
          `"${format(new Date(settlement.documentDate), 'yyyy-MM-dd')}"`,
          settlement.actualQuantityMT.toString(),
          settlement.actualQuantityBBL.toString(),
          settlement.totalSettlementAmount.toString(),
          `"${settlement.settlementCurrency}"`,
          `"${settlement.displayStatus}"`,
          `"${format(new Date(settlement.createdDate), 'yyyy-MM-dd HH:mm')}"`,
          `"${settlement.createdBy}"`,
          settlement.chargesCount.toString()
        ].join(',')
      )
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `settlements_${format(new Date(), 'yyyy-MM-dd')}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD',
    }).format(amount);
  };

  const formatQuantity = (quantity: number, unit: string = '') => {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(quantity) + (unit ? ` ${unit}` : '');
  };

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1">
            {isSearchMode ? 'Search Results' : 'Settlements'}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {isSearchMode
              ? `Found ${sortedAndFilteredSettlements.length} settlements for: "${searchTerm}"`
              : `${totalCount > 0 ? totalCount : settlements.length} total settlements`
            }
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<SearchIcon />}
            onClick={onBackToSearch}
            size="small"
          >
            Advanced Search
          </Button>
          <Tooltip title="Export to CSV">
            <IconButton onClick={handleExport} disabled={settlements.length === 0}>
              <ExportIcon />
            </IconButton>
          </Tooltip>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={onCreateNew}
          >
            Create Settlement
          </Button>
        </Box>
      </Box>

      {/* Status Summary Cards */}
      {!isSearchMode && settlements.length > 0 && (
        <Grid container spacing={2} sx={{ mb: 3 }}>
          {Object.entries(statusCounts).map(([status, count]) => (
            <Grid item key={status}>
              <Card
                sx={{
                  minWidth: 120,
                  cursor: 'pointer',
                  border: statusFilter !== undefined && ContractSettlementStatusLabels[statusFilter] === status
                    ? '2px solid'
                    : '1px solid transparent',
                  borderColor: statusFilter !== undefined && ContractSettlementStatusLabels[statusFilter] === status
                    ? 'primary.main'
                    : 'transparent',
                  '&:hover': { borderColor: 'primary.light' }
                }}
                onClick={() => {
                  const matchEntry = Object.entries(ContractSettlementStatusLabels).find(([, label]) => label === status);
                  if (matchEntry) {
                    const val = Number(matchEntry[0]) as ContractSettlementStatus;
                    handleStatusFilter(statusFilter === val ? undefined : val);
                  }
                }}
              >
                <CardContent sx={{ py: 1.5, px: 2, '&:last-child': { pb: 1.5 } }}>
                  <Typography variant="caption" color="text.secondary">{status}</Typography>
                  <Typography variant="h5" fontWeight="bold">{count}</Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Status Filter Chips + Local Text Filter */}
      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 1.5, display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
          <FilterIcon color="action" fontSize="small" />
          {STATUS_FILTERS.map(sf => (
            <Chip
              key={sf.label}
              label={sf.label}
              variant={statusFilter === sf.value ? 'filled' : 'outlined'}
              color={statusFilter === sf.value ? 'primary' : 'default'}
              size="small"
              onClick={() => handleStatusFilter(sf.value)}
            />
          ))}
          <Box sx={{ flexGrow: 1 }} />
          <TextField
            size="small"
            placeholder="Filter by text..."
            value={filterTerm}
            onChange={(e) => setFilterTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 200 }}
          />
        </CardContent>
      </Card>

      {/* Error */}
      {error && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Loading State */}
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      )}

      {/* Empty State */}
      {!loading && settlements.length === 0 && (
        <Card>
          <CardContent sx={{ textAlign: 'center', py: 6 }}>
            <Typography variant="h6" color="text.secondary" gutterBottom>
              No settlements found
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              {statusFilter !== undefined
                ? `No settlements with status "${ContractSettlementStatusLabels[statusFilter]}". Try clearing the filter.`
                : 'Create your first settlement to get started.'
              }
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
              <Button variant="contained" startIcon={<AddIcon />} onClick={onCreateNew}>
                Create Settlement
              </Button>
              {statusFilter !== undefined && (
                <Button variant="outlined" onClick={() => handleStatusFilter(undefined)}>
                  Clear Filter
                </Button>
              )}
            </Box>
          </CardContent>
        </Card>
      )}

      {/* Results Table */}
      {!loading && settlements.length > 0 && (
        <>
          <Card>
            <TableContainer>
              <Table stickyHeader size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>
                      <TableSortLabel
                        active={sortField === 'externalContractNumber'}
                        direction={sortField === 'externalContractNumber' ? sortDirection : 'asc'}
                        onClick={() => handleSort('externalContractNumber')}
                      >
                        External Contract #
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortField === 'contractNumber'}
                        direction={sortField === 'contractNumber' ? sortDirection : 'asc'}
                        onClick={() => handleSort('contractNumber')}
                      >
                        Contract #
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>Document</TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortField === 'documentDate'}
                        direction={sortField === 'documentDate' ? sortDirection : 'asc'}
                        onClick={() => handleSort('documentDate')}
                      >
                        Doc Date
                      </TableSortLabel>
                    </TableCell>
                    <TableCell align="right">Qty (MT)</TableCell>
                    <TableCell align="right">
                      <TableSortLabel
                        active={sortField === 'totalSettlementAmount'}
                        direction={sortField === 'totalSettlementAmount' ? sortDirection : 'asc'}
                        onClick={() => handleSort('totalSettlementAmount')}
                      >
                        Amount
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortField === 'status'}
                        direction={sortField === 'status' ? sortDirection : 'asc'}
                        onClick={() => handleSort('status')}
                      >
                        Status
                      </TableSortLabel>
                    </TableCell>
                    <TableCell>
                      <TableSortLabel
                        active={sortField === 'createdDate'}
                        direction={sortField === 'createdDate' ? sortDirection : 'asc'}
                        onClick={() => handleSort('createdDate')}
                      >
                        Created
                      </TableSortLabel>
                    </TableCell>
                    <TableCell align="center">Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {sortedAndFilteredSettlements.map((settlement) => (
                    <TableRow
                      key={settlement.id}
                      hover
                      sx={{ cursor: 'pointer' }}
                      onClick={() => onSettlementSelect(settlement.id)}
                    >
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {settlement.externalContractNumber || '-'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {settlement.contractNumber}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" fontWeight="medium">
                          {settlement.documentNumber || 'N/A'}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {settlement.documentType}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {settlement.documentDate ? format(new Date(settlement.documentDate), 'MMM dd, yyyy') : '-'}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography variant="body2">
                          {formatQuantity(settlement.actualQuantityMT, 'MT')}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" fontWeight="medium">
                          {formatCurrency(settlement.totalSettlementAmount, settlement.settlementCurrency)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={settlement.displayStatus || settlement.status}
                          color={getSettlementStatusColor(settlement.status as unknown as ContractSettlementStatus)}
                          size="small"
                          variant={settlement.isFinalized ? 'filled' : 'outlined'}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {settlement.createdDate ? format(new Date(settlement.createdDate), 'MMM dd') : '-'}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {settlement.createdBy}
                        </Typography>
                      </TableCell>
                      <TableCell align="center" onClick={(e) => e.stopPropagation()}>
                        <Tooltip title="View Details">
                          <IconButton size="small" onClick={() => onSettlementSelect(settlement.id)}>
                            <ViewIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            {!isSearchMode && (
              <TablePagination
                component="div"
                count={totalCount}
                page={page}
                onPageChange={(_, newPage) => setPage(newPage)}
                rowsPerPage={rowsPerPage}
                onRowsPerPageChange={(e) => {
                  setRowsPerPage(parseInt(e.target.value, 10));
                  setPage(0);
                }}
                rowsPerPageOptions={[10, 25, 50, 100]}
              />
            )}
          </Card>

          {/* Summary Bar */}
          <Card sx={{ mt: 2 }}>
            <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
              <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                <Box>
                  <Typography variant="caption" color="text.secondary">Showing</Typography>
                  <Typography variant="subtitle2">{sortedAndFilteredSettlements.length} settlements</Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">Total Value</Typography>
                  <Typography variant="subtitle2">
                    {formatCurrency(
                      sortedAndFilteredSettlements.reduce((sum, s) => sum + s.totalSettlementAmount, 0),
                      sortedAndFilteredSettlements[0]?.settlementCurrency || 'USD'
                    )}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">Finalized</Typography>
                  <Typography variant="subtitle2">
                    {sortedAndFilteredSettlements.filter(s => s.isFinalized).length}
                  </Typography>
                </Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">Total Qty (MT)</Typography>
                  <Typography variant="subtitle2">
                    {formatQuantity(sortedAndFilteredSettlements.reduce((sum, s) => sum + s.actualQuantityMT, 0), 'MT')}
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </>
      )}
    </Box>
  );
};
