import React, { useState, useCallback } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
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
} from '@mui/material';
import {
  Download as DownloadIcon,
  RestoreFromTrash as RestoreIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import reportingApi, { ReportArchive } from '@/services/reportingApi';

const ReportArchivesList: React.FC = () => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const queryClient = useQueryClient();

  // Fetch archives
  const { data, isLoading, isError } = useQuery({
    queryKey: ['reportArchives', page, rowsPerPage],
    queryFn: () => reportingApi.listArchives(page + 1, rowsPerPage),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => reportingApi.deleteArchive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reportArchives'] });
      setSuccess('Archive deleted successfully');
      setDeleteConfirmId(null);
      setTimeout(() => setSuccess(null), 3000);
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to delete archive');
    },
  });

  // Download mutation
  const downloadMutation = useMutation({
    mutationFn: (id: string) => reportingApi.downloadArchive(id),
    onSuccess: (blob, id) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `archive-${id}.tar.gz`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      setSuccess('Archive downloaded successfully');
      setTimeout(() => setSuccess(null), 3000);
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to download archive');
    },
  });

  // Restore mutation
  const restoreMutation = useMutation({
    mutationFn: (id: string) => reportingApi.restoreArchive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reportArchives'] });
      queryClient.invalidateQueries({ queryKey: ['reportExecutions'] });
      setSuccess('Archive restored successfully');
      setTimeout(() => setSuccess(null), 3000);
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to restore archive');
    },
  });

  const handleDelete = useCallback((id: string) => {
    deleteMutation.mutate(id);
  }, [deleteMutation]);

  const handleDownload = useCallback((id: string) => {
    downloadMutation.mutate(id);
  }, [downloadMutation]);

  const handleRestore = useCallback((id: string) => {
    restoreMutation.mutate(id);
  }, [restoreMutation]);

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const formatDate = (date?: string) => {
    if (!date) return '-';
    return new Date(date).toLocaleDateString();
  };

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return '-';
    if (bytes < 1024) return `${bytes}B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)}KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)}MB`;
  };

  const isArchiveExpired = (expiryDate?: string) => {
    if (!expiryDate) return false;
    return new Date(expiryDate) < new Date();
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent sx={{ textAlign: 'center', py: 4 }}>
          Loading archives...
        </CardContent>
      </Card>
    );
  }

  if (isError) {
    return (
      <Alert severity="error">
        Error loading archives
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

      <Card>
        <CardHeader title="Report Archives" />
        <CardContent>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell>Archive Date</TableCell>
                  <TableCell>Expiry Date</TableCell>
                  <TableCell align="right">Size</TableCell>
                  <TableCell align="right">Retention Days</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data?.items && data.items.length > 0 ? (
                  data.items.map((archive) => {
                    const expired = isArchiveExpired(archive.expiryDate);
                    return (
                      <TableRow key={archive.id} hover sx={{
                        backgroundColor: expired ? '#ffebee' : 'inherit'
                      }}>
                        <TableCell>{formatDate(archive.archiveDate)}</TableCell>
                        <TableCell sx={{ color: expired ? 'error.main' : 'inherit' }}>
                          {formatDate(archive.expiryDate)}
                          {expired && ' (Expired)'}
                        </TableCell>
                        <TableCell align="right">
                          {formatFileSize(archive.fileSize)}
                        </TableCell>
                        <TableCell align="right">
                          {archive.retentionDays}
                        </TableCell>
                        <TableCell align="right">
                          {!expired && (
                            <>
                              <Tooltip title="Download">
                                <IconButton
                                  size="small"
                                  color="primary"
                                  onClick={() => handleDownload(archive.id!)}
                                  disabled={downloadMutation.isPending}
                                >
                                  <DownloadIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                              <Tooltip title="Restore">
                                <IconButton
                                  size="small"
                                  color="info"
                                  onClick={() => handleRestore(archive.id!)}
                                  disabled={restoreMutation.isPending}
                                >
                                  <RestoreIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            </>
                          )}
                          <Tooltip title="Delete">
                            <IconButton
                              size="small"
                              color="error"
                              onClick={() => setDeleteConfirmId(archive.id!)}
                            >
                              <DeleteIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    );
                  })
                ) : (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 3 }}>
                      No archives found
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

      <Dialog open={!!deleteConfirmId} onClose={() => setDeleteConfirmId(null)}>
        <DialogTitle>Delete Archive</DialogTitle>
        <DialogContent>
          Are you sure you want to permanently delete this archive? This action cannot be undone.
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmId(null)}>Cancel</Button>
          <Button
            onClick={() => deleteConfirmId && handleDelete(deleteConfirmId)}
            color="error"
            variant="contained"
            disabled={deleteMutation.isPending}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ReportArchivesList;
