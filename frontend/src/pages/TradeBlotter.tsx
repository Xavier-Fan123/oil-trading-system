import React, { useState, useEffect, useMemo, useCallback } from 'react';
import {
  Box,
  Typography,
  Card,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  TablePagination,
  Chip,
  TextField,
  InputAdornment,
  CircularProgress,
  Alert,
  ToggleButton,
  ToggleButtonGroup,
  Button,
} from '@mui/material';
import {
  Search as SearchIcon,
  FileDownload as ExportIcon,
  ViewModule as GroupIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useNavigate } from 'react-router-dom';
import { purchaseContractsApi } from '@/services/contractsApi';
import { salesContractsApi } from '@/services/salesContractsApi';
import { PurchaseContractListDto, ContractStatus } from '@/types/contracts';
import { SalesContractListDto } from '@/types/salesContracts';

interface BlotterRow {
  id: string;
  side: 'BUY' | 'SELL';
  contractNumber: string;
  externalContractNumber?: string;
  counterparty: string;
  product: string;
  quantity: number;
  unit: string;
  contractValue: number | null;
  currency: string;
  pricingStatus: string;
  laycanStart: Date | string;
  laycanEnd: Date | string;
  status: ContractStatus;
  createdAt: Date | string;
}

type SortField = 'side' | 'contractNumber' | 'counterparty' | 'product' | 'quantity' | 'contractValue' | 'laycanStart' | 'status';
type SortDirection = 'asc' | 'desc';
type SideFilter = 'all' | 'BUY' | 'SELL';

// Backend returns status as string ("Active", "Draft") due to JsonStringEnumConverter
// Must normalize before comparing against numeric enum values
const normalizeStatus = (status: any): ContractStatus => {
  if (typeof status === 'string') {
    switch (status) {
      case 'Draft': return ContractStatus.Draft;
      case 'PendingApproval': return ContractStatus.PendingApproval;
      case 'Active': return ContractStatus.Active;
      case 'Completed': return ContractStatus.Completed;
      case 'Cancelled': return ContractStatus.Cancelled;
      default: return ContractStatus.Draft;
    }
  }
  return status;
};

const getStatusColor = (status: ContractStatus | string): 'default' | 'primary' | 'success' | 'warning' | 'error' | 'info' => {
  const s = normalizeStatus(status);
  switch (s) {
    case ContractStatus.Draft: return 'default';
    case ContractStatus.PendingApproval: return 'warning';
    case ContractStatus.Active: return 'success';
    case ContractStatus.Completed: return 'info';
    case ContractStatus.Cancelled: return 'error';
    default: return 'default';
  }
};

const getStatusLabel = (status: ContractStatus | string): string => {
  if (typeof status === 'string') return status;
  switch (status) {
    case ContractStatus.Draft: return 'Draft';
    case ContractStatus.PendingApproval: return 'Pending';
    case ContractStatus.Active: return 'Active';
    case ContractStatus.Completed: return 'Completed';
    case ContractStatus.Cancelled: return 'Cancelled';
    default: return 'Unknown';
  }
};

const formatCurrency = (value: number, currency = 'USD'): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value);
};

export const TradeBlotter: React.FC = () => {
  const navigate = useNavigate();
  const [rows, setRows] = useState<BlotterRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterText, setFilterText] = useState('');
  const [sideFilter, setSideFilter] = useState<SideFilter>('all');
  const [sortField, setSortField] = useState<SortField>('laycanStart');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(50);
  const [groupByProduct, setGroupByProduct] = useState(false);

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      setError(null);
      try {
        const [purchaseResult, salesResult] = await Promise.all([
          purchaseContractsApi.getAll({ pageNumber: 1, pageSize: 500 }),
          salesContractsApi.getAll({ pageNumber: 1, pageSize: 500 }),
        ]);

        const buyRows: BlotterRow[] = (purchaseResult.items || []).map((c: PurchaseContractListDto) => ({
          id: c.id,
          side: 'BUY' as const,
          contractNumber: c.contractNumber,
          externalContractNumber: c.externalContractNumber,
          counterparty: c.supplierName,
          product: c.productName,
          quantity: c.quantity,
          unit: typeof c.quantityUnit === 'string' ? c.quantityUnit : c.quantityUnit === 1 ? 'MT' : 'BBL',
          contractValue: c.contractValue ?? null,
          currency: c.contractValueCurrency || 'USD',
          pricingStatus: c.pricingStatus || 'Unpriced',
          laycanStart: c.laycanStart,
          laycanEnd: c.laycanEnd,
          status: c.status,
          createdAt: c.createdAt,
        }));

        const sellRows: BlotterRow[] = (salesResult.items || []).map((c: SalesContractListDto) => ({
          id: c.id,
          side: 'SELL' as const,
          contractNumber: c.contractNumber,
          externalContractNumber: c.externalContractNumber,
          counterparty: c.customerName,
          product: c.productName,
          quantity: c.quantity,
          unit: typeof c.quantityUnit === 'string' ? c.quantityUnit : c.quantityUnit === 1 ? 'MT' : 'BBL',
          contractValue: c.contractValue ?? null,
          currency: 'USD',
          pricingStatus: c.contractValue ? 'Priced' : 'Unpriced',
          laycanStart: c.laycanStart,
          laycanEnd: c.laycanEnd,
          status: c.status,
          createdAt: c.createdAt,
        }));

        setRows([...buyRows, ...sellRows]);
      } catch (err) {
        console.error('Failed to load trade blotter data:', err);
        setError('Failed to load contracts. The backend may be unavailable.');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  const filteredAndSorted = useMemo(() => {
    let result = rows;

    if (sideFilter !== 'all') {
      result = result.filter(r => r.side === sideFilter);
    }

    if (filterText.trim()) {
      const term = filterText.toLowerCase();
      result = result.filter(r =>
        r.contractNumber.toLowerCase().includes(term) ||
        (r.externalContractNumber || '').toLowerCase().includes(term) ||
        r.counterparty.toLowerCase().includes(term) ||
        r.product.toLowerCase().includes(term)
      );
    }

    return [...result].sort((a, b) => {
      let aVal: any, bVal: any;
      switch (sortField) {
        case 'side': aVal = a.side; bVal = b.side; break;
        case 'contractNumber': aVal = a.contractNumber; bVal = b.contractNumber; break;
        case 'counterparty': aVal = a.counterparty; bVal = b.counterparty; break;
        case 'product': aVal = a.product; bVal = b.product; break;
        case 'quantity': aVal = a.quantity; bVal = b.quantity; break;
        case 'contractValue': aVal = a.contractValue || 0; bVal = b.contractValue || 0; break;
        case 'laycanStart': aVal = new Date(a.laycanStart); bVal = new Date(b.laycanStart); break;
        case 'status': aVal = a.status; bVal = b.status; break;
        default: aVal = a.laycanStart; bVal = b.laycanStart;
      }
      if (aVal < bVal) return sortDirection === 'asc' ? -1 : 1;
      if (aVal > bVal) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
  }, [rows, sideFilter, filterText, sortField, sortDirection]);

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('desc');
    }
  };

  // Product group subtotals
  const productGroups = useMemo(() => {
    if (!groupByProduct) return null;
    const groups: Record<string, { buyQty: number; sellQty: number; buyValue: number; sellValue: number; unit: string }> = {};
    filteredAndSorted.forEach(r => {
      if (!groups[r.product]) {
        groups[r.product] = { buyQty: 0, sellQty: 0, buyValue: 0, sellValue: 0, unit: r.unit };
      }
      if (r.side === 'BUY') {
        groups[r.product].buyQty += r.quantity;
        groups[r.product].buyValue += r.contractValue || 0;
      } else {
        groups[r.product].sellQty += r.quantity;
        groups[r.product].sellValue += r.contractValue || 0;
      }
    });
    return groups;
  }, [filteredAndSorted, groupByProduct]);

  const paginatedRows = groupByProduct
    ? filteredAndSorted // show all when grouped (pagination less useful)
    : filteredAndSorted.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);

  const buyCount = rows.filter(r => r.side === 'BUY').length;
  const sellCount = rows.filter(r => r.side === 'SELL').length;
  const totalValue = filteredAndSorted.reduce((sum, r) => sum + (r.contractValue || 0), 0);

  const handleRowClick = (row: BlotterRow) => {
    navigate(row.side === 'BUY' ? `/contracts/${row.id}` : `/sales-contracts/${row.id}`);
  };

  const handleExportCSV = useCallback(() => {
    const headers = ['Side', 'Contract #', 'External #', 'Counterparty', 'Product', 'Quantity', 'Unit', 'Value', 'Currency', 'Pricing', 'Laycan Start', 'Laycan End', 'Status'];
    const csvRows = filteredAndSorted.map(r => [
      r.side,
      r.contractNumber,
      r.externalContractNumber || '',
      r.counterparty,
      r.product,
      r.quantity,
      r.unit,
      r.contractValue || '',
      r.currency,
      r.pricingStatus,
      r.laycanStart ? format(new Date(r.laycanStart), 'yyyy-MM-dd') : '',
      r.laycanEnd ? format(new Date(r.laycanEnd), 'yyyy-MM-dd') : '',
      getStatusLabel(r.status),
    ]);
    const csv = [headers, ...csvRows].map(row => row.map(cell => `"${cell}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `trade-blotter-${format(new Date(), 'yyyy-MM-dd')}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }, [filteredAndSorted]);

  // Build rows grouped by product
  const renderGroupedRows = () => {
    if (!productGroups) return null;
    const sortedProducts = Object.keys(productGroups).sort();
    return sortedProducts.map(product => {
      const group = productGroups[product];
      const productRows = filteredAndSorted.filter(r => r.product === product);
      const netQty = group.buyQty - group.sellQty;
      return (
        <React.Fragment key={`group-${product}`}>
          {/* Group header */}
          <TableRow sx={{ backgroundColor: 'action.hover' }}>
            <TableCell colSpan={10} sx={{ py: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Typography variant="subtitle2" fontWeight="bold">{product}</Typography>
                <Chip label={`BUY: ${group.buyQty.toLocaleString()} ${group.unit}`} size="small" sx={{ backgroundColor: '#e3f2fd', color: '#1565c0', fontWeight: 'bold' }} />
                <Chip label={`SELL: ${group.sellQty.toLocaleString()} ${group.unit}`} size="small" sx={{ backgroundColor: '#fff3e0', color: '#e65100', fontWeight: 'bold' }} />
                <Chip
                  label={`NET: ${netQty >= 0 ? '+' : ''}${netQty.toLocaleString()} ${group.unit} (${netQty >= 0 ? 'Long' : 'Short'})`}
                  size="small"
                  sx={{ backgroundColor: netQty >= 0 ? '#e8f5e9' : '#ffebee', color: netQty >= 0 ? '#2e7d32' : '#c62828', fontWeight: 'bold' }}
                />
                {(group.buyValue > 0 || group.sellValue > 0) && (
                  <Typography variant="caption" color="text.secondary">
                    Value: {formatCurrency(group.buyValue + group.sellValue)}
                  </Typography>
                )}
              </Box>
            </TableCell>
          </TableRow>
          {/* Product rows */}
          {productRows.map(row => renderRow(row))}
        </React.Fragment>
      );
    });
  };

  const renderRow = (row: BlotterRow) => (
    <TableRow
      key={`${row.side}-${row.id}`}
      hover
      sx={{
        cursor: 'pointer',
        borderLeft: row.side === 'BUY' ? '3px solid #2196f3' : '3px solid #ff9800',
      }}
      onClick={() => handleRowClick(row)}
    >
      <TableCell>
        <Chip
          label={row.side}
          size="small"
          sx={{
            backgroundColor: row.side === 'BUY' ? '#2196f3' : '#ff9800',
            color: '#fff',
            fontWeight: 'bold',
            fontSize: '0.7rem',
          }}
        />
      </TableCell>
      <TableCell>
        <Typography variant="body2" fontWeight="medium">{row.contractNumber}</Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2" color="text.secondary">{row.externalContractNumber || '-'}</Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2">{row.counterparty}</Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2">{row.product}</Typography>
      </TableCell>
      <TableCell align="right">
        <Typography variant="body2">
          {row.quantity.toLocaleString()} {row.unit}
        </Typography>
      </TableCell>
      <TableCell align="right">
        {row.contractValue ? (
          <Typography variant="body2" fontWeight="medium">
            {formatCurrency(row.contractValue, row.currency)}
          </Typography>
        ) : (
          <Chip label={row.pricingStatus} size="small" variant="outlined" sx={{ fontSize: '0.65rem', height: 18 }} />
        )}
      </TableCell>
      <TableCell>
        <Typography variant="body2">
          {row.laycanStart ? format(new Date(row.laycanStart), 'MMM dd') : '-'}
          {row.laycanEnd ? ` - ${format(new Date(row.laycanEnd), 'MMM dd, yyyy')}` : ''}
        </Typography>
      </TableCell>
      <TableCell>
        <Chip
          label={getStatusLabel(row.status)}
          color={getStatusColor(row.status)}
          size="small"
          variant="outlined"
        />
      </TableCell>
    </TableRow>
  );

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
        <Typography variant="h4" gutterBottom>Trade Blotter</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            size="small"
            variant={groupByProduct ? 'contained' : 'outlined'}
            startIcon={<GroupIcon />}
            onClick={() => setGroupByProduct(!groupByProduct)}
          >
            Group
          </Button>
          <Button
            size="small"
            variant="outlined"
            startIcon={<ExportIcon />}
            onClick={handleExportCSV}
            disabled={filteredAndSorted.length === 0}
          >
            Export CSV
          </Button>
        </Box>
      </Box>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {buyCount} Buy | {sellCount} Sell | {rows.length} Total | Value: {formatCurrency(totalValue)}
      </Typography>

      {/* Filters */}
      <Box sx={{ display: 'flex', gap: 2, mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
        <ToggleButtonGroup
          value={sideFilter}
          exclusive
          onChange={(_, val) => val && setSideFilter(val)}
          size="small"
        >
          <ToggleButton value="all">All ({rows.length})</ToggleButton>
          <ToggleButton value="BUY" sx={{ color: '#2196f3' }}>Buy ({buyCount})</ToggleButton>
          <ToggleButton value="SELL" sx={{ color: '#ff9800' }}>Sell ({sellCount})</ToggleButton>
        </ToggleButtonGroup>

        <TextField
          size="small"
          placeholder="Filter by contract, counterparty, product..."
          value={filterText}
          onChange={(e) => setFilterText(e.target.value)}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment>
            ),
          }}
          sx={{ minWidth: 300 }}
        />
      </Box>

      {error && <Alert severity="warning" sx={{ mb: 2 }}>{error}</Alert>}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}><CircularProgress /></Box>
      ) : (
        <Card>
          <TableContainer>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>
                    <TableSortLabel active={sortField === 'side'} direction={sortField === 'side' ? sortDirection : 'asc'} onClick={() => handleSort('side')}>
                      Side
                    </TableSortLabel>
                  </TableCell>
                  <TableCell>
                    <TableSortLabel active={sortField === 'contractNumber'} direction={sortField === 'contractNumber' ? sortDirection : 'asc'} onClick={() => handleSort('contractNumber')}>
                      Contract #
                    </TableSortLabel>
                  </TableCell>
                  <TableCell>External #</TableCell>
                  <TableCell>
                    <TableSortLabel active={sortField === 'counterparty'} direction={sortField === 'counterparty' ? sortDirection : 'asc'} onClick={() => handleSort('counterparty')}>
                      Counterparty
                    </TableSortLabel>
                  </TableCell>
                  <TableCell>
                    <TableSortLabel active={sortField === 'product'} direction={sortField === 'product' ? sortDirection : 'asc'} onClick={() => handleSort('product')}>
                      Product
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right">
                    <TableSortLabel active={sortField === 'quantity'} direction={sortField === 'quantity' ? sortDirection : 'asc'} onClick={() => handleSort('quantity')}>
                      Quantity
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right">
                    <TableSortLabel active={sortField === 'contractValue'} direction={sortField === 'contractValue' ? sortDirection : 'asc'} onClick={() => handleSort('contractValue')}>
                      Value
                    </TableSortLabel>
                  </TableCell>
                  <TableCell>
                    <TableSortLabel active={sortField === 'laycanStart'} direction={sortField === 'laycanStart' ? sortDirection : 'asc'} onClick={() => handleSort('laycanStart')}>
                      Laycan
                    </TableSortLabel>
                  </TableCell>
                  <TableCell>
                    <TableSortLabel active={sortField === 'status'} direction={sortField === 'status' ? sortDirection : 'asc'} onClick={() => handleSort('status')}>
                      Status
                    </TableSortLabel>
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {groupByProduct ? (
                  renderGroupedRows()
                ) : (
                  paginatedRows.map(row => renderRow(row))
                )}
                {filteredAndSorted.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} align="center">
                      <Typography variant="body2" color="text.secondary" sx={{ py: 4 }}>
                        No contracts found
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
          {!groupByProduct && (
            <TablePagination
              component="div"
              count={filteredAndSorted.length}
              page={page}
              onPageChange={(_, p) => setPage(p)}
              rowsPerPage={rowsPerPage}
              onRowsPerPageChange={(e) => { setRowsPerPage(parseInt(e.target.value, 10)); setPage(0); }}
              rowsPerPageOptions={[25, 50, 100]}
            />
          )}
        </Card>
      )}
    </Box>
  );
};

export default TradeBlotter;
