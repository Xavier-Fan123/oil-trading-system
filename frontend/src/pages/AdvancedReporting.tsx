import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Container,
  Dialog,
  Divider,
  Grid,
  LinearProgress,
  Alert,
  Card,
  CardContent,
  Typography,
  Tabs,
  Tab,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  MoreVert as MoreVertIcon,
  PlayArrow as RunIcon,
} from '@mui/icons-material';
import { ReportConfiguration, ReportStatus } from '@/types/advancedReporting';
import { advancedReportingApi } from '@/services/advancedReportingApi';
import { ReportBuilder } from '@/components/Reports/ReportBuilder';
import { ReportScheduler } from '@/components/Reports/ReportScheduler';
import { ReportHistory } from '@/components/Reports/ReportHistory';
import { ReportDistribution } from '@/components/Reports/ReportDistribution';

type ViewMode = 'list' | 'create' | 'edit' | 'manage';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div role="tabpanel" hidden={value !== index}>
    {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
  </div>
);

export const AdvancedReportingPage: React.FC = () => {
  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [reports, setReports] = useState<ReportConfiguration[]>([]);
  const [selectedReport, setSelectedReport] = useState<ReportConfiguration | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tabValue, setTabValue] = useState(0);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [selectedMenuReport, setSelectedMenuReport] = useState<ReportConfiguration | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [reportToDelete, setReportToDelete] = useState<string | null>(null);

  useEffect(() => {
    if (viewMode === 'list') {
      loadReports();
    }
  }, [viewMode]);

  const loadReports = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await advancedReportingApi.listReportConfigs(1, 50);
      setReports(result.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load reports');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateReport = async (config: ReportConfiguration) => {
    setLoading(true);
    setError(null);
    try {
      const created = await advancedReportingApi.createReportConfig(config);
      setReports((prev) => [...prev, created]);
      setViewMode('list');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create report');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateReport = async (config: ReportConfiguration) => {
    if (!config.id) return;

    setLoading(true);
    setError(null);
    try {
      const updated = await advancedReportingApi.updateReportConfig(config.id, config);
      setReports((prev) =>
        prev.map((r) => (r.id === config.id ? updated : r))
      );
      setViewMode('list');
      setSelectedReport(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update report');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteReport = async () => {
    if (!reportToDelete) return;

    setLoading(true);
    setError(null);
    try {
      await advancedReportingApi.deleteReportConfig(reportToDelete);
      setReports((prev) => prev.filter((r) => r.id !== reportToDelete));
      setDeleteConfirmOpen(false);
      setReportToDelete(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete report');
    } finally {
      setLoading(false);
    }
  };

  const handleRunReport = async (reportId: string) => {
    setLoading(true);
    setError(null);
    try {
      await advancedReportingApi.executeReport(reportId);
      // Show success message
      setViewMode('manage');
      setSelectedReport(reports.find((r) => r.id === reportId) || null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to run report');
    } finally {
      setLoading(false);
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, report: ReportConfiguration) => {
    setMenuAnchor(event.currentTarget);
    setSelectedMenuReport(report);
  };

  const handleMenuClose = () => {
    setMenuAnchor(null);
    setSelectedMenuReport(null);
  };

  const handleDeleteClick = (reportId: string) => {
    setReportToDelete(reportId);
    setDeleteConfirmOpen(true);
    handleMenuClose();
  };

  // List View
  if (viewMode === 'list') {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h4">Advanced Reporting</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              setSelectedReport(null);
              setViewMode('create');
            }}
          >
            Create Report
          </Button>
        </Box>

        {/* Error Message */}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {/* Loading */}
        {loading && <LinearProgress sx={{ mb: 2 }} />}

        {/* Reports Table */}
        {reports.length > 0 ? (
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: 'background.default' }}>
                  <TableCell sx={{ fontWeight: 600 }}>Report Name</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>Type</TableCell>
                  <TableCell sx={{ fontWeight: 600 }} align="center">
                    Last Run
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600 }} align="right">
                    Actions
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {reports.map((report) => (
                  <TableRow key={report.id} hover>
                    <TableCell>
                      <Typography
                        variant="body2"
                        sx={{ fontWeight: 600, cursor: 'pointer', color: 'primary.main' }}
                        onClick={() => {
                          setSelectedReport(report);
                          setViewMode('manage');
                        }}
                      >
                        {report.name}
                      </Typography>
                      <Typography variant="caption" color="textSecondary">
                        {report.description}
                      </Typography>
                    </TableCell>
                    <TableCell>{report.reportType}</TableCell>
                    <TableCell align="center">
                      <Typography variant="caption">-</Typography>
                    </TableCell>
                    <TableCell align="right">
                      <IconButton
                        size="small"
                        onClick={() => handleRunReport(report.id!)}
                        title="Run report"
                        disabled={loading}
                      >
                        <RunIcon fontSize="small" />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuOpen(e, report)}
                      >
                        <MoreVertIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 4 }}>
              <Typography color="textSecondary" gutterBottom>
                No reports created yet
              </Typography>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => setViewMode('create')}
                sx={{ mt: 2 }}
              >
                Create First Report
              </Button>
            </CardContent>
          </Card>
        )}

        {/* Context Menu */}
        <Menu
          anchorEl={menuAnchor}
          open={Boolean(menuAnchor)}
          onClose={handleMenuClose}
        >
          <MenuItem onClick={() => {
            if (selectedMenuReport) {
              setSelectedReport(selectedMenuReport);
              setViewMode('edit');
            }
            handleMenuClose();
          }}>
            <ListItemIcon><EditIcon fontSize="small" /></ListItemIcon>
            <ListItemText>Edit</ListItemText>
          </MenuItem>
          <MenuItem onClick={() => {
            if (selectedMenuReport) {
              setSelectedReport(selectedMenuReport);
              setViewMode('manage');
            }
            handleMenuClose();
          }}>
            <ListItemIcon><RunIcon fontSize="small" /></ListItemIcon>
            <ListItemText>Manage</ListItemText>
          </MenuItem>
          <Divider />
          <MenuItem
            onClick={() => {
              if (selectedMenuReport) {
                handleDeleteClick(selectedMenuReport.id!);
              }
            }}
            sx={{ color: 'error.main' }}
          >
            <ListItemIcon><DeleteIcon fontSize="small" color="error" /></ListItemIcon>
            <ListItemText>Delete</ListItemText>
          </MenuItem>
        </Menu>

        {/* Delete Confirmation Dialog */}
        <Dialog open={deleteConfirmOpen} onClose={() => setDeleteConfirmOpen(false)}>
          <Box sx={{ p: 2 }}>
            <Typography variant="h6">Delete Report</Typography>
            <Typography sx={{ mt: 2 }}>
              Are you sure you want to delete this report? This action cannot be undone.
            </Typography>
            <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1, mt: 3 }}>
              <Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
              <Button
                variant="contained"
                color="error"
                onClick={handleDeleteReport}
              >
                Delete
              </Button>
            </Box>
          </Box>
        </Dialog>
      </Container>
    );
  }

  // Create View
  if (viewMode === 'create') {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Button onClick={() => setViewMode('list')} sx={{ mb: 2 }}>
            ← Back to Reports
          </Button>
          <ReportBuilder
            onSave={handleCreateReport}
            onCancel={() => setViewMode('list')}
            isLoading={loading}
          />
        </Box>
      </Container>
    );
  }

  // Edit View
  if (viewMode === 'edit' && selectedReport) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Button onClick={() => setViewMode('list')} sx={{ mb: 2 }}>
            ← Back to Reports
          </Button>
          <ReportBuilder
            existingConfig={selectedReport}
            onSave={handleUpdateReport}
            onCancel={() => setViewMode('list')}
            isLoading={loading}
          />
        </Box>
      </Container>
    );
  }

  // Manage View (Scheduling, History, etc.)
  if (viewMode === 'manage' && selectedReport) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Button onClick={() => setViewMode('list')} sx={{ mb: 2 }}>
            ← Back to Reports
          </Button>
          <Typography variant="h5">{selectedReport.name}</Typography>
          <Typography color="textSecondary">{selectedReport.description}</Typography>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Paper>
          <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
            <Tab label="Scheduling" />
            <Tab label="Execution History" />
            <Tab label="Distribution" />
          </Tabs>

          <TabPanel value={tabValue} index={0}>
            <ReportScheduler
              reportConfigId={selectedReport.id!}
              onCancel={() => setViewMode('list')}
            />
          </TabPanel>

          <TabPanel value={tabValue} index={1}>
            <ReportHistory
              reportConfigId={selectedReport.id!}
            />
          </TabPanel>

          <TabPanel value={tabValue} index={2}>
            <ReportDistribution
              reportConfigId={selectedReport.id!}
              onCancel={() => setViewMode('list')}
            />
          </TabPanel>
        </Paper>
      </Container>
    );
  }

  return null;
};

export default AdvancedReportingPage;
