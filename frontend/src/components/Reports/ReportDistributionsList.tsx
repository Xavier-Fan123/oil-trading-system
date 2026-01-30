import React, { useState, useCallback } from 'react';
import {
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
  Chip,
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  Add as AddIcon,
  Check as CheckIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import reportingApi, { ReportDistribution } from '@/services/reportingApi';
import ReportDistributionForm from './ReportDistributionForm';
import { Alert } from '@mui/material';

const ReportDistributionsList: React.FC = () => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [openForm, setOpenForm] = useState(false);
  const [selectedDistribution, setSelectedDistribution] = useState<ReportDistribution | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const queryClient = useQueryClient();

  // Fetch distributions
  const { data, isLoading, isError } = useQuery({
    queryKey: ['reportDistributions', page, rowsPerPage],
    queryFn: () => reportingApi.listDistributions(page + 1, rowsPerPage),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => reportingApi.deleteDistribution(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reportDistributions'] });
      setSuccess('Distribution deleted successfully');
      setDeleteConfirmId(null);
      setTimeout(() => setSuccess(null), 3000);
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to delete distribution');
    },
  });

  const handleOpenForm = useCallback((dist?: ReportDistribution) => {
    setSelectedDistribution(dist || null);
    setOpenForm(true);
  }, []);

  const handleCloseForm = useCallback(() => {
    setOpenForm(false);
    setSelectedDistribution(null);
  }, []);

  const handleFormSuccess = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['reportDistributions'] });
    handleCloseForm();
    setSuccess(selectedDistribution ? 'Distribution updated' : 'Distribution created');
    setTimeout(() => setSuccess(null), 3000);
  }, [selectedDistribution, queryClient, handleCloseForm]);

  const handleDelete = useCallback((id: string) => {
    deleteMutation.mutate(id);
  }, [deleteMutation]);

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent sx={{ textAlign: 'center', py: 4 }}>
          Loading distributions...
        </CardContent>
      </Card>
    );
  }

  if (isError) {
    return (
      <Alert severity="error">
        Error loading distributions
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
          title="Report Distributions"
          action={
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => handleOpenForm()}
            >
              New Distribution
            </Button>
          }
        />
        <CardContent>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell>Name</TableCell>
                  <TableCell>Channel Type</TableCell>
                  <TableCell align="center">Enabled</TableCell>
                  <TableCell>Last Test</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data?.items && data.items.length > 0 ? (
                  data.items.map((dist) => (
                    <TableRow key={dist.id} hover>
                      <TableCell sx={{ fontWeight: 500 }}>{dist.channelName}</TableCell>
                      <TableCell>{dist.channelType}</TableCell>
                      <TableCell align="center">
                        {dist.isEnabled ? (
                          <CheckIcon sx={{ color: 'green' }} />
                        ) : (
                          <CloseIcon sx={{ color: 'red' }} />
                        )}
                      </TableCell>
                      <TableCell>
                        {dist.lastTestStatus && (
                          <Chip
                            label={dist.lastTestStatus}
                            size="small"
                            color={dist.lastTestStatus === 'Passed' ? 'success' : 'warning'}
                          />
                        )}
                      </TableCell>
                      <TableCell align="right">
                        <Tooltip title="Edit">
                          <IconButton
                            size="small"
                            color="primary"
                            onClick={() => handleOpenForm(dist)}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Delete">
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => setDeleteConfirmId(dist.id!)}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={5} align="center" sx={{ py: 3 }}>
                      No distributions found
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

      <ReportDistributionForm
        open={openForm}
        onClose={handleCloseForm}
        onSuccess={handleFormSuccess}
        distribution={selectedDistribution || undefined}
      />

      <Dialog open={!!deleteConfirmId} onClose={() => setDeleteConfirmId(null)}>
        <DialogTitle>Delete Distribution</DialogTitle>
        <DialogContent>
          Are you sure? This action cannot be undone.
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmId(null)}>Cancel</Button>
          <Button
            onClick={() => deleteConfirmId && handleDelete(deleteConfirmId)}
            color="error"
            variant="contained"
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ReportDistributionsList;
