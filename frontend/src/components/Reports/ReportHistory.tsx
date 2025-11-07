import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  LinearProgress,
  Alert,
  Pagination,
  Stack,
  TextField,
} from '@mui/material';
import {
  Download as DownloadIcon,
  Delete as DeleteIcon,
  Info as InfoIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { ReportExecutionHistory, ReportStatus, REPORT_STATUS_LABELS } from '@/types/advancedReporting';
import { advancedReportingApi } from '@/services/advancedReportingApi';

interface ReportHistoryProps {
  reportConfigId?: string;
  pageSize?: number;
}

const getStatusColor = (status: ReportStatus): 'default' | 'primary' | 'success' | 'error' | 'warning' => {
  switch (status) {
    case ReportStatus.Completed:
      return 'success';
    case ReportStatus.Running:
      return 'primary';
    case ReportStatus.Failed:
      return 'error';
    case ReportStatus.Scheduled:
      return 'warning';
    case ReportStatus.Archived:
      return 'default';
    default:
      return 'default';
  }
};

const formatFileSize = (bytes?: number): string => {
  if (!bytes) return 'Unknown';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
};

const formatDuration = (ms: number): string => {
  const seconds = Math.floor(ms / 1000);
  const minutes = Math.floor(seconds / 60);
  if (minutes > 0) {
    return `${minutes}m ${seconds % 60}s`;
  }
  return `${seconds}s`;
};

export const ReportHistory: React.FC<ReportHistoryProps> = ({
  reportConfigId,
  pageSize = 10,
}) => {
  const [history, setHistory] = useState<ReportExecutionHistory[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedExecution, setSelectedExecution] = useState<ReportExecutionHistory | null>(null);
  const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);
  const [downloadProgress, setDownloadProgress] = useState(0);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  useEffect(() => {
    loadHistory();
  }, [reportConfigId, page]);

  const loadHistory = async () => {
    setLoading(true);
    setError(null);
    try {
      if (!reportConfigId) return;

      const result = await advancedReportingApi.getExecutionHistoryPaged(
        reportConfigId,
        page,
        pageSize
      );
      setHistory(result.data);
      setTotalPages(result.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setLoading(false);
    }
  };

  const handleDownload = async (execution: ReportExecutionHistory) => {
    try {
      setDownloadingId(execution.id);
      setDownloadProgress(0);

      const blob = await advancedReportingApi.downloadReport(execution.id, (event) => {
        if (event.total) {
          const progress = Math.round((event.loaded / event.total) * 100);
          setDownloadProgress(progress);
        }
      });

      // Create download link
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = execution.fileName || `report-${execution.id}.zip`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to download report');
    } finally {
      setDownloadingId(null);
      setDownloadProgress(0);
    }
  };

  const handleRetry = async (execution: ReportExecutionHistory) => {
    try {
      setLoading(true);
      await advancedReportingApi.retryExecution(execution.id);
      setError(null);
      // Reload history
      loadHistory();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retry execution');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (executionId: string) => {
    if (!window.confirm('Delete this execution record?')) return;

    try {
      setLoading(true);
      await advancedReportingApi.deleteArchivedReport(executionId);
      setHistory((prev) => prev.filter((h) => h.id !== executionId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete record');
    } finally {
      setLoading(false);
    }
  };

  const handleShowDetails = (execution: ReportExecutionHistory) => {
    setSelectedExecution(execution);
    setDetailsDialogOpen(true);
  };

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">Execution History</Typography>
          <Button
            startIcon={<RefreshIcon />}
            onClick={() => {
              setPage(1);
              loadHistory();
            }}
            disabled={loading}
          >
            Refresh
          </Button>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {loading && <LinearProgress sx={{ mb: 2 }} />}

        {history.length > 0 ? (
          <>
            <TableContainer component={Paper}>
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ backgroundColor: 'background.default' }}>
                    <TableCell>Execution Date</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell align="center">Records</TableCell>
                    <TableCell align="right">Size</TableCell>
                    <TableCell align="right">Duration</TableCell>
                    <TableCell width={120}>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {history.map((execution) => (
                    <TableRow key={execution.id}>
                      <TableCell>
                        {new Date(execution.executionDate).toLocaleString()}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={REPORT_STATUS_LABELS[execution.status]}
                          color={getStatusColor(execution.status)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell align="center">{execution.recordsProcessed}</TableCell>
                      <TableCell align="right">
                        {formatFileSize(execution.fileSize)}
                      </TableCell>
                      <TableCell align="right">
                        {formatDuration(execution.executionDurationMs)}
                      </TableCell>
                      <TableCell>
                        <IconButton
                          size="small"
                          onClick={() => handleShowDetails(execution)}
                          title="View details"
                        >
                          <InfoIcon fontSize="small" />
                        </IconButton>
                        {execution.status === ReportStatus.Completed && (
                          <IconButton
                            size="small"
                            onClick={() => handleDownload(execution)}
                            disabled={downloadingId === execution.id}
                            title="Download report"
                          >
                            <DownloadIcon fontSize="small" />
                          </IconButton>
                        )}
                        {execution.status === ReportStatus.Failed && (
                          <Button
                            size="small"
                            onClick={() => handleRetry(execution)}
                            disabled={loading}
                          >
                            Retry
                          </Button>
                        )}
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleDelete(execution.id)}
                          disabled={loading}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>

            {/* Download Progress */}
            {downloadingId && (
              <Box sx={{ mt: 2 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
                  <Typography variant="caption" color="textSecondary">
                    Downloading... {downloadProgress}%
                  </Typography>
                </Box>
                <LinearProgress variant="determinate" value={downloadProgress} />
              </Box>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
              <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
                <Pagination
                  count={totalPages}
                  page={page}
                  onChange={(e, newPage) => setPage(newPage)}
                />
              </Box>
            )}
          </>
        ) : (
          <Typography color="textSecondary" sx={{ textAlign: 'center', py: 3 }}>
            {reportConfigId ? 'No execution history' : 'Select a report to view history'}
          </Typography>
        )}
      </CardContent>

      {/* Details Dialog */}
      <Dialog open={detailsDialogOpen} onClose={() => setDetailsDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Execution Details</DialogTitle>
        <DialogContent dividers>
          {selectedExecution && (
            <Stack spacing={2} sx={{ pt: 2 }}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Execution ID
                </Typography>
                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                  {selectedExecution.id}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="textSecondary">
                  Status
                </Typography>
                <Box sx={{ mt: 1 }}>
                  <Chip
                    label={REPORT_STATUS_LABELS[selectedExecution.status]}
                    color={getStatusColor(selectedExecution.status)}
                  />
                </Box>
              </Box>

              <Box>
                <Typography variant="caption" color="textSecondary">
                  Execution Date
                </Typography>
                <Typography variant="body2">
                  {new Date(selectedExecution.executionDate).toLocaleString()}
                </Typography>
              </Box>

              {selectedExecution.completionDate && (
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Completion Date
                  </Typography>
                  <Typography variant="body2">
                    {new Date(selectedExecution.completionDate).toLocaleString()}
                  </Typography>
                </Box>
              )}

              <Box>
                <Typography variant="caption" color="textSecondary">
                  Records Processed
                </Typography>
                <Typography variant="body2">
                  {selectedExecution.recordsProcessed.toLocaleString()}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="textSecondary">
                  Duration
                </Typography>
                <Typography variant="body2">
                  {formatDuration(selectedExecution.executionDurationMs)}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="textSecondary">
                  File Size
                </Typography>
                <Typography variant="body2">
                  {formatFileSize(selectedExecution.fileSize)}
                </Typography>
              </Box>

              {selectedExecution.errorMessage && (
                <Box>
                  <Typography variant="caption" color="error">
                    Error Message
                  </Typography>
                  <TextField
                    fullWidth
                    multiline
                    rows={3}
                    value={selectedExecution.errorMessage}
                    InputProps={{ readOnly: true }}
                    size="small"
                    sx={{ mt: 1 }}
                  />
                </Box>
              )}

              <Box>
                <Typography variant="caption" color="textSecondary">
                  Created By
                </Typography>
                <Typography variant="body2">
                  {selectedExecution.createdBy}
                </Typography>
              </Box>
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailsDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};

export default ReportHistory;
