import React, { useState, useEffect } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  TablePagination,
  Box,
  Chip,
  LinearProgress,
  Typography,
  CircularProgress,
  Alert,
  Tooltip,
} from '@mui/material';
import { format } from 'date-fns';
import { ContractExecutionReportDto, ContractExecutionReportFilter } from '@/types/reports';
import { contractExecutionReportApi } from '@/services/contractExecutionReportApi';

interface ContractExecutionReportTableProps {
  filters?: ContractExecutionReportFilter;
  onReportSelect?: (report: ContractExecutionReportDto) => void;
}

export const ContractExecutionReportTable: React.FC<ContractExecutionReportTableProps> = ({
  filters,
  onReportSelect,
}) => {
  const [reports, setReports] = useState<ContractExecutionReportDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [error, setError] = useState<string | null>(null);

  // Load reports when filters or pagination changes
  useEffect(() => {
    loadReports();
  }, [filters, pageNumber, pageSize]);

  const loadReports = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await contractExecutionReportApi.getContractReports(
        pageNumber,
        pageSize,
        filters?.contractType,
        filters?.executionStatus,
        filters?.fromDate,
        filters?.toDate,
        filters?.tradingPartnerId,
        filters?.productId,
        filters?.sortBy || 'ReportGeneratedDate',
        filters?.sortDescending !== false
      );

      setReports(result.items || []);
      setTotalCount(result.totalCount || 0);
    } catch (err: any) {
      setError(err.message || 'Failed to load reports');
      console.error('Error loading reports:', err);
    } finally {
      setLoading(false);
    }
  };

  const handlePageChange = (_event: unknown, newPage: number) => {
    setPageNumber(newPage + 1);
  };

  const handlePageSizeChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setPageSize(parseInt(event.target.value, 10));
    setPageNumber(1);
  };

  const getStatusColor = (status: string): 'success' | 'info' | 'warning' | 'error' => {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'OnTrack':
        return 'info';
      case 'Delayed':
        return 'warning';
      case 'Cancelled':
        return 'error';
      default:
        return 'info';
    }
  };

  const getPaymentStatusColor = (status: string): 'success' | 'warning' | 'error' | 'default' => {
    switch (status) {
      case 'Paid':
        return 'success';
      case 'PartiallyPaid':
        return 'warning';
      case 'NotPaid':
      case 'NotDue':
        return 'default';
      default:
        return 'default';
    }
  };

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  return (
    <Box>
      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="contract execution reports">
          <TableHead>
            <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
              <TableCell>Contract #</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Trading Partner</TableCell>
              <TableCell>Product</TableCell>
              <TableCell align="right">Execution %</TableCell>
              <TableCell>Execution Status</TableCell>
              <TableCell>Payment Status</TableCell>
              <TableCell align="right">Settlement Amount</TableCell>
              <TableCell>Report Date</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={9} align="center" sx={{ py: 3 }}>
                  <CircularProgress />
                </TableCell>
              </TableRow>
            ) : reports.length === 0 ? (
              <TableRow>
                <TableCell colSpan={9} align="center" sx={{ py: 3 }}>
                  <Typography color="textSecondary">No reports found</Typography>
                </TableCell>
              </TableRow>
            ) : (
              reports.map((report) => (
                <TableRow
                  key={report.id}
                  onClick={() => onReportSelect?.(report)}
                  sx={{
                    cursor: onReportSelect ? 'pointer' : 'default',
                    '&:hover': onReportSelect ? { backgroundColor: '#f9f9f9' } : {},
                  }}
                >
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {report.contractNumber}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={report.contractType}
                      size="small"
                      variant="outlined"
                      color={report.contractType === 'Purchase' ? 'primary' : 'secondary'}
                    />
                  </TableCell>
                  <TableCell>
                    <Tooltip title={report.tradingPartnerName}>
                      <Typography variant="body2" sx={{ maxWidth: 150, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {report.tradingPartnerName}
                      </Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell>
                    <Tooltip title={report.productName}>
                      <Typography variant="body2" sx={{ maxWidth: 100, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {report.productName}
                      </Typography>
                    </Tooltip>
                  </TableCell>
                  <TableCell align="right">
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 1 }}>
                      <Box sx={{ minWidth: 50, textAlign: 'right' }}>
                        <Typography variant="body2">{report.executionPercentage.toFixed(1)}%</Typography>
                      </Box>
                      <Box sx={{ width: 80 }}>
                        <LinearProgress
                          variant="determinate"
                          value={Math.min(report.executionPercentage, 100)}
                          sx={{
                            backgroundColor: '#e0e0e0',
                            '& .MuiLinearProgress-bar': {
                              backgroundColor:
                                report.executionPercentage >= 100
                                  ? '#4caf50'
                                  : report.executionPercentage >= 75
                                  ? '#8bc34a'
                                  : report.executionPercentage >= 50
                                  ? '#fdd835'
                                  : '#ff7043',
                            },
                          }}
                        />
                      </Box>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={report.executionStatus}
                      size="small"
                      color={getStatusColor(report.executionStatus)}
                      variant="filled"
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={report.paymentStatus}
                      size="small"
                      color={getPaymentStatusColor(report.paymentStatus)}
                      variant="filled"
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {report.currency} {report.totalSettledAmount.toLocaleString()}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="caption" color="textSecondary">
                      {format(new Date(report.reportGeneratedDate), 'MMM dd, yyyy')}
                    </Typography>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        rowsPerPageOptions={[5, 10, 25, 50]}
        component="div"
        count={totalCount}
        rowsPerPage={pageSize}
        page={pageNumber - 1}
        onPageChange={handlePageChange}
        onRowsPerPageChange={handlePageSizeChange}
      />
    </Box>
  );
};
