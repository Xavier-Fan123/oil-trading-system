import React, { useState, useCallback } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  Tooltip,
  Chip,
} from '@mui/material';
import {
  Download as DownloadIcon,
  Refresh as RefreshIcon,
  PlayArrow as PlayArrowIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import reportingApi from '@/services/reportingApi';
import ReportExecutionForm from './ReportExecutionForm';
import { Alert } from '@mui/material';

const ReportExecutionsList: React.FC = () => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [openForm, setOpenForm] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const queryClient = useQueryClient();

  // Fetch executions
  const { data, isLoading, isError } = useQuery({
    queryKey: ['reportExecutions', page, rowsPerPage],
    queryFn: () => reportingApi.listExecutions(page + 1, rowsPerPage),
  });

  // Download mutation
  const downloadMutation = useMutation({
    mutationFn: (id: string) => reportingApi.downloadExecution(id),
    onSuccess: (blob, id) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `report-${id}.csv`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      setSuccess('Report downloaded successfully');
      setTimeout(() => setSuccess(null), 3000);
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to download report');
    },
  });

  const handleOpenForm = useCallback(() => {
    setOpenForm(true);
  }, []);

  const handleCloseForm = useCallback(() => {
    setOpenForm(false);
  }, []);

  const handleFormSuccess = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['reportExecutions'] });
    handleCloseForm();
    setSuccess('Report executed successfully');
    setTimeout(() => setSuccess(null), 3000);
  }, [queryClient, handleCloseForm]);

  const handleDownload = useCallback((id: string) => {
    downloadMutation.mutate(id);
  }, [downloadMutation]);

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const getStatusColor = (status: string): 'success' | 'warning' | 'error' | 'info' => {
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'success';
      case 'running':
        return 'info';
      case 'failed':
        return 'error';
      default:
        return 'warning';
    }
  };

  const formatDate = (date?: string) => {
    if (!date) return '-';
    return new Date(date).toLocaleString();
  };

  const formatDuration = (ms?: number) => {
    if (!ms) return '-';
    return `${(ms / 1000).toFixed(2)}s`;
  };

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return '-';
    if (bytes < 1024) return `${bytes}B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)}KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)}MB`;
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent sx={{ textAlign: 'center', py: 4 }}>
          Loading executions...
        </CardContent>
      </Card>
    );
  }

  if (isError) {
    return (
      <Alert severity="error">
        Error loading executions
      </Alert>
    );
  }

  return (
    <Box sx={{ width: '100%' }}>
      {error && (
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
      {success && (
        <Alert severity="success" onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      )}

      <Card sx={{ mb: 2 }}>
        <CardHeader
          title="Report Executions"
          action={
            <Button
              variant="contained"
              startIcon={<PlayArrowIcon />}
              onClick={handleOpenForm}
            >
              Execute Report
            </Button>
          }
        />
        <CardContent>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell>Report</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Started</TableCell>
                  <TableCell align="right">Duration</TableCell>
                  <TableCell align="right">Records</TableCell>
                  <TableCell align="right">Size</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data?.items && data.items.length > 0 ? (
                  data.items.map((execution) => (
                    <TableRow key={execution.id} hover>
                      <TableCell sx={{ fontWeight: 500 }}>
                        {execution.reportConfigId}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={execution.status}
                          color={getStatusColor(execution.status)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>{formatDate(execution.executionStartTime)}</TableCell>
                      <TableCell align="right">
                        {formatDuration(execution.durationMilliseconds)}
                      </TableCell>
                      <TableCell align="right">
                        {execution.recordsProcessed?.toLocaleString() || '-'}
                      </TableCell>
                      <TableCell align="right">
                        {formatFileSize(execution.fileSizeBytes)}
                      </TableCell>
                      <TableCell align="right">
                        {execution.status === 'Completed' && (
                          <Tooltip title="Download">
                            <IconButton
                              size="small"
                              color="primary"
                              onClick={() => handleDownload(execution.id!)}
                              disabled={downloadMutation.isPending}
                            >
                              <DownloadIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                        {execution.status === 'Failed' && (
                          <Tooltip title="Retry">
                            <IconButton
                              size="small"
                              color="warning"
                              onClick={handleOpenForm}
                            >
                              <RefreshIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={7} align="center" sx={{ py: 3 }}>
                      No executions found
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>

          {data && (
            <TablePagination
              rowsPerPageOptions={[5, 10, 25, 50]}
              component="div"
              count={data.totalCount}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={handleChangePage}
              onRowsPerPageChange={handleChangeRowsPerPage}
            />
          )}
        </CardContent>
      </Card>

      {/* Execution Form Dialog */}
      <ReportExecutionForm
        open={openForm}
        onClose={handleCloseForm}
        onSuccess={handleFormSuccess}
      />
    </Box>
  );
};

export default ReportExecutionsList;
