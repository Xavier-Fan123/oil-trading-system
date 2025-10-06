import React, { useState } from 'react';
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
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Add as AddIcon,
  Visibility as ViewIcon,
  Edit as EditIcon,
  Search as SearchIcon,
  FileDownload as ExportIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { 
  ContractSettlementListDto,
  ContractSettlementStatus,
  getSettlementStatusColor 
} from '@/types/settlement';

interface SettlementListProps {
  settlements: ContractSettlementListDto[];
  searchTerm: string;
  onSettlementSelect: (settlementId: string) => void;
  onCreateNew: () => void;
  onBackToSearch: () => void;
}

type SortField = 'documentDate' | 'contractNumber' | 'externalContractNumber' | 'totalSettlementAmount' | 'status' | 'createdDate';
type SortDirection = 'asc' | 'desc';

export const SettlementList: React.FC<SettlementListProps> = ({
  settlements,
  searchTerm,
  onSettlementSelect,
  onCreateNew,
  onBackToSearch
}) => {
  const [sortField, setSortField] = useState<SortField>('documentDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [filterTerm, setFilterTerm] = useState('');

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('desc');
    }
  };

  const sortedAndFilteredSettlements = React.useMemo(() => {
    let filtered = settlements;

    // Apply local filter
    if (filterTerm.trim()) {
      const term = filterTerm.toLowerCase();
      filtered = settlements.filter(settlement =>
        settlement.externalContractNumber.toLowerCase().includes(term) ||
        settlement.contractNumber.toLowerCase().includes(term) ||
        settlement.documentNumber?.toLowerCase().includes(term) ||
        settlement.status.toLowerCase().includes(term) ||
        settlement.createdBy.toLowerCase().includes(term)
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
          aValue = a.documentDate;
          bValue = b.documentDate;
      }

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
  }, [settlements, sortField, sortDirection, filterTerm]);

  const handleExport = () => {
    // Create CSV content
    const headers = [
      'Contract Number',
      'External Contract Number',
      'Document Number',
      'Document Type',
      'Document Date',
      'Quantity (MT)',
      'Quantity (BBL)',
      'Settlement Amount',
      'Currency',
      'Status',
      'Created Date',
      'Created By',
      'Charges Count'
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

    // Download CSV
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

  if (settlements.length === 0) {
    return (
      <Box>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
          <Button
            startIcon={<ArrowBackIcon />}
            onClick={onBackToSearch}
            sx={{ mr: 2 }}
          >
            Back to Search
          </Button>
          <Typography variant="h4" component="h1">
            Settlement Search Results
          </Typography>
        </Box>

        <Alert severity="info">
          No settlements found for search term: <strong>{searchTerm}</strong>
          <Box sx={{ mt: 2 }}>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={onCreateNew}
              sx={{ mr: 1 }}
            >
              Create New Settlement
            </Button>
            <Button
              variant="outlined"
              onClick={onBackToSearch}
            >
              Try Another Search
            </Button>
          </Box>
        </Alert>
      </Box>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center' }}>
          <Button
            startIcon={<ArrowBackIcon />}
            onClick={onBackToSearch}
            sx={{ mr: 2 }}
          >
            Back to Search
          </Button>
          <Box>
            <Typography variant="h4" component="h1">
              Settlement Search Results
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Found {sortedAndFilteredSettlements.length} of {settlements.length} settlements for: <strong>{searchTerm}</strong>
            </Typography>
          </Box>
        </Box>
        
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Export to CSV">
            <IconButton onClick={handleExport}>
              <ExportIcon />
            </IconButton>
          </Tooltip>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={onCreateNew}
          >
            Create New
          </Button>
        </Box>
      </Box>

      {/* Filter Bar */}
      <Card sx={{ mb: 3 }}>
        <CardContent sx={{ py: 2 }}>
          <TextField
            size="small"
            placeholder="Filter results..."
            value={filterTerm}
            onChange={(e) => setFilterTerm(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 300 }}
          />
        </CardContent>
      </Card>

      {/* Results Table */}
      <Card>
        <TableContainer>
          <Table stickyHeader>
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
                <TableCell>Document Info</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'documentDate'}
                    direction={sortField === 'documentDate' ? sortDirection : 'asc'}
                    onClick={() => handleSort('documentDate')}
                  >
                    Document Date
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">Quantities</TableCell>
                <TableCell align="right">
                  <TableSortLabel
                    active={sortField === 'totalSettlementAmount'}
                    direction={sortField === 'totalSettlementAmount' ? sortDirection : 'asc'}
                    onClick={() => handleSort('totalSettlementAmount')}
                  >
                    Settlement Amount
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
                <TableCell>Created</TableCell>
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
                      {settlement.externalContractNumber}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {settlement.contractNumber}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Box>
                      <Typography variant="body2" fontWeight="medium">
                        {settlement.documentNumber || 'N/A'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {settlement.documentType}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {format(new Date(settlement.documentDate), 'MMM dd, yyyy')}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Box>
                      <Typography variant="body2">
                        {formatQuantity(settlement.actualQuantityMT, 'MT')}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {formatQuantity(settlement.actualQuantityBBL, 'BBL')}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell align="right">
                    <Box>
                      <Typography variant="body2" fontWeight="medium">
                        {formatCurrency(settlement.totalSettlementAmount, settlement.settlementCurrency)}
                      </Typography>
                      {settlement.chargesCount > 0 && (
                        <Typography variant="caption" color="text.secondary">
                          {settlement.chargesCount} charges
                        </Typography>
                      )}
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={settlement.displayStatus}
                      color={getSettlementStatusColor(settlement.status as unknown as ContractSettlementStatus)}
                      size="small"
                      variant={settlement.isFinalized ? 'filled' : 'outlined'}
                    />
                  </TableCell>
                  <TableCell>
                    <Box>
                      <Typography variant="body2">
                        {format(new Date(settlement.createdDate), 'MMM dd')}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        by {settlement.createdBy}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell align="center" onClick={(e) => e.stopPropagation()}>
                    <Box sx={{ display: 'flex', gap: 0.5 }}>
                      <Tooltip title="View Details">
                        <IconButton
                          size="small"
                          onClick={() => onSettlementSelect(settlement.id)}
                        >
                          <ViewIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      {(settlement as any).canBeModified && (
                        <Tooltip title="Edit Settlement">
                          <IconButton
                            size="small"
                            onClick={() => onSettlementSelect(settlement.id)}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      {/* Summary */}
      {sortedAndFilteredSettlements.length > 0 && (
        <Card sx={{ mt: 2 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Summary
            </Typography>
            <Box sx={{ display: 'flex', gap: 4 }}>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Total Settlements
                </Typography>
                <Typography variant="h6">
                  {sortedAndFilteredSettlements.length}
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Total Value
                </Typography>
                <Typography variant="h6">
                  {formatCurrency(
                    sortedAndFilteredSettlements.reduce((sum, s) => sum + s.totalSettlementAmount, 0),
                    sortedAndFilteredSettlements[0]?.settlementCurrency || 'USD'
                  )}
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Finalized
                </Typography>
                <Typography variant="h6">
                  {sortedAndFilteredSettlements.filter(s => s.isFinalized).length}
                </Typography>
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">
                  Total Charges
                </Typography>
                <Typography variant="h6">
                  {sortedAndFilteredSettlements.reduce((sum, s) => sum + s.chargesCount, 0)}
                </Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};