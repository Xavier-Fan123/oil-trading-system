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
} from '@mui/material';
import {
  Edit as EditIcon,
  Delete as DeleteIcon,
  Add as AddIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import reportingApi, { ReportConfiguration } from '@/services/reportingApi';
import ReportConfigurationForm from './ReportConfigurationForm';
import { Alert } from '@mui/material';

const ReportConfigurationsList: React.FC = () => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [openForm, setOpenForm] = useState(false);
  const [selectedConfig, setSelectedConfig] = useState<ReportConfiguration | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const queryClient = useQueryClient();

  // Fetch configurations
  const { data, isLoading, isError } = useQuery({
    queryKey: ['reportConfigurations', page, rowsPerPage],
    queryFn: () => reportingApi.listConfigurations(page + 1, rowsPerPage),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => reportingApi.deleteConfiguration(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reportConfigurations'] });
      setSuccess('Configuration deleted successfully');
      setDeleteConfirmId(null);
      setTimeout(() => setSuccess(null), 3000);
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to delete configuration');
    },
  });

  const handleOpenForm = useCallback((config?: ReportConfiguration) => {
    setSelectedConfig(config || null);
    setOpenForm(true);
  }, []);

  const handleCloseForm = useCallback(() => {
    setOpenForm(false);
    setSelectedConfig(null);
  }, []);

  const handleFormSuccess = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['reportConfigurations'] });
    handleCloseForm();
    setSuccess(selectedConfig ? 'Configuration updated successfully' : 'Configuration created successfully');
    setTimeout(() => setSuccess(null), 3000);
  }, [selectedConfig, queryClient, handleCloseForm]);

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
          Loading configurations...
        </CardContent>
      </Card>
    );
  }

  if (isError) {
    return (
      <Alert severity="error">
        Error loading configurations
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
          title="Report Configurations"
          action={
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => handleOpenForm()}
            >
              New Configuration
            </Button>
          }
        />
        <CardContent>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                  <TableCell>Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Format</TableCell>
                  <TableCell align="center">Active</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data?.items && data.items.length > 0 ? (
                  data.items.map((config) => (
                    <TableRow key={config.id} hover>
                      <TableCell sx={{ fontWeight: 500 }}>{config.name}</TableCell>
                      <TableCell>{config.reportType}</TableCell>
                      <TableCell>{config.exportFormat}</TableCell>
                      <TableCell align="center">
                        {config.isActive ? '✓' : '✗'}
                      </TableCell>
                      <TableCell align="right">
                        <Tooltip title="View details">
                          <IconButton
                            size="small"
                            color="primary"
                            onClick={() => handleOpenForm(config)}
                          >
                            <VisibilityIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Edit">
                          <IconButton
                            size="small"
                            color="primary"
                            onClick={() => handleOpenForm(config)}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Delete">
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => setDeleteConfirmId(config.id!)}
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
                      No configurations found
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

      {/* Form Dialog */}
      <ReportConfigurationForm
        open={openForm}
        onClose={handleCloseForm}
        onSuccess={handleFormSuccess}
        config={selectedConfig || undefined}
      />

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={!!deleteConfirmId}
        onClose={() => setDeleteConfirmId(null)}
      >
        <DialogTitle>Delete Configuration</DialogTitle>
        <DialogContent>
          Are you sure you want to delete this report configuration? This action cannot be undone.
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

export default ReportConfigurationsList;
