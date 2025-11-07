import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardActions,
  TextField,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControlLabel,
  Switch,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Typography,
  Alert,
  CircularProgress,
  Divider,
  Grid,
  ListItemIcon,
  ListItemText,
  Menu,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  Email as EmailIcon,
  Storage as SftpIcon,
  Webhook as WebhookIcon,
  MoreVert as MoreVertIcon,
  CheckCircle as SuccessIcon,
  Cancel as FailIcon,
} from '@mui/icons-material';
import { advancedReportingApi } from '@/services/advancedReportingApi';
import { ReportDistribution, DistributionChannel } from '@/types/advancedReporting';

interface ReportDistributionProps {
  reportConfigId: string;
  onCancel: () => void;
}

interface DistributionChannelFormData {
  name: string;
  type: 'Email' | 'SFTP' | 'Webhook';
  enabled: boolean;
  config: {
    recipients?: string;
    subject?: string;
    body?: string;
    sftpHost?: string;
    sftpPort?: number;
    sftpUsername?: string;
    sftpPassword?: string;
    sftpPath?: string;
    webhookUrl?: string;
    webhookHeaders?: string;
    retryAttempts?: number;
    retryDelayMinutes?: number;
  };
}

const defaultChannelConfig: DistributionChannelFormData = {
  name: '',
  type: 'Email',
  enabled: true,
  config: {
    recipients: '',
    subject: 'Report: {reportName}',
    body: 'Please find the attached report.',
    retryAttempts: 3,
    retryDelayMinutes: 5,
  },
};

export const ReportDistribution: React.FC<ReportDistributionProps> = ({
  reportConfigId,
  onCancel,
}) => {
  const [distributions, setDistributions] = useState<ReportDistribution[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [openDialog, setOpenDialog] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [testLoading, setTestLoading] = useState(false);
  const [testingChannelId, setTestingChannelId] = useState<string | null>(null);
  const [testResult, setTestResult] = useState<{
    channelId: string;
    success: boolean;
    message: string;
  } | null>(null);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [selectedChannelId, setSelectedChannelId] = useState<string | null>(null);

  const [channelForm, setChannelForm] = useState<DistributionChannelFormData>(
    defaultChannelConfig
  );

  useEffect(() => {
    loadDistributions();
  }, [reportConfigId]);

  const loadDistributions = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await advancedReportingApi.getDistributions(reportConfigId);
      setDistributions(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load distributions');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenDialog = (distribution?: ReportDistribution) => {
    if (distribution) {
      setEditingId(distribution.id);
      setChannelForm({
        name: distribution.channelName,
        type: distribution.channelType as 'Email' | 'SFTP' | 'Webhook',
        enabled: distribution.isEnabled,
        config: JSON.parse(distribution.channelConfiguration || '{}'),
      });
    } else {
      setEditingId(null);
      setChannelForm(defaultChannelConfig);
    }
    setOpenDialog(true);
  };

  const handleCloseDialog = () => {
    setOpenDialog(false);
    setEditingId(null);
    setChannelForm(defaultChannelConfig);
  };

  const handleSaveChannel = async () => {
    if (!channelForm.name.trim()) {
      setError('Channel name is required');
      return;
    }

    // Validate based on type
    if (channelForm.type === 'Email' && !channelForm.config.recipients?.trim()) {
      setError('Email recipients are required');
      return;
    }

    if (channelForm.type === 'SFTP') {
      if (!channelForm.config.sftpHost?.trim() || !channelForm.config.sftpUsername?.trim()) {
        setError('SFTP host and username are required');
        return;
      }
    }

    if (channelForm.type === 'Webhook' && !channelForm.config.webhookUrl?.trim()) {
      setError('Webhook URL is required');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      if (editingId) {
        await advancedReportingApi.updateDistribution(reportConfigId, editingId, {
          channelName: channelForm.name,
          channelType: channelForm.type,
          channelConfiguration: JSON.stringify(channelForm.config),
          isEnabled: channelForm.enabled,
        });
      } else {
        await advancedReportingApi.createDistribution(reportConfigId, {
          channelName: channelForm.name,
          channelType: channelForm.type,
          channelConfiguration: JSON.stringify(channelForm.config),
          isEnabled: channelForm.enabled,
        });
      }
      handleCloseDialog();
      await loadDistributions();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save distribution');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteChannel = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this distribution channel?')) {
      setLoading(true);
      setError(null);
      try {
        await advancedReportingApi.deleteDistribution(reportConfigId, id);
        await loadDistributions();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to delete distribution');
      } finally {
        setLoading(false);
        handleMenuClose();
      }
    }
  };

  const handleTestChannel = async (id: string) => {
    setTestLoading(true);
    setTestingChannelId(id);
    setError(null);

    try {
      await advancedReportingApi.testDistribution(reportConfigId, id);
      setTestResult({
        channelId: id,
        success: true,
        message: 'Distribution channel test successful!',
      });
    } catch (err) {
      setTestResult({
        channelId: id,
        success: false,
        message: err instanceof Error ? err.message : 'Distribution channel test failed',
      });
    } finally {
      setTestLoading(false);
      setTestingChannelId(null);
      handleMenuClose();
    }
  };

  const handleToggleChannel = async (id: string, enabled: boolean) => {
    setLoading(true);
    setError(null);
    try {
      const distribution = distributions.find((d) => d.id === id);
      if (distribution) {
        await advancedReportingApi.updateDistribution(reportConfigId, id, {
          channelName: distribution.channelName,
          channelType: distribution.channelType,
          channelConfiguration: distribution.channelConfiguration,
          isEnabled: !enabled,
        });
        await loadDistributions();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update distribution');
    } finally {
      setLoading(false);
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, channelId: string) => {
    setMenuAnchor(event.currentTarget);
    setSelectedChannelId(channelId);
  };

  const handleMenuClose = () => {
    setMenuAnchor(null);
    setSelectedChannelId(null);
  };

  const getChannelIcon = (type: string) => {
    switch (type) {
      case 'Email':
        return <EmailIcon fontSize="small" />;
      case 'SFTP':
        return <SftpIcon fontSize="small" />;
      case 'Webhook':
        return <WebhookIcon fontSize="small" />;
      default:
        return null;
    }
  };

  return (
    <Box>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {testResult && (
        <Alert
          severity={testResult.success ? 'success' : 'error'}
          sx={{ mb: 2 }}
          onClose={() => setTestResult(null)}
        >
          {testResult.message}
        </Alert>
      )}

      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Distribution Channels</Typography>
        <Button
          startIcon={<AddIcon />}
          variant="contained"
          onClick={() => handleOpenDialog()}
          disabled={loading}
          size="small"
        >
          Add Channel
        </Button>
      </Box>

      {/* Loading */}
      {loading && <CircularProgress sx={{ display: 'block', mx: 'auto', my: 2 }} />}

      {/* Channels Table */}
      {distributions.length > 0 ? (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ backgroundColor: 'background.default' }}>
                <TableCell sx={{ fontWeight: 600 }}>Channel Name</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>Type</TableCell>
                <TableCell sx={{ fontWeight: 600 }} align="center">
                  Status
                </TableCell>
                <TableCell sx={{ fontWeight: 600 }}>Configuration</TableCell>
                <TableCell sx={{ fontWeight: 600 }} align="right">
                  Actions
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {distributions.map((distribution) => {
                const config = JSON.parse(distribution.channelConfiguration || '{}');
                return (
                  <TableRow key={distribution.id} hover>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        {getChannelIcon(distribution.channelType)}
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {distribution.channelName}
                        </Typography>
                      </Box>
                    </TableCell>
                    <TableCell>{distribution.channelType}</TableCell>
                    <TableCell align="center">
                      <Chip
                        label={distribution.isEnabled ? 'Enabled' : 'Disabled'}
                        size="small"
                        color={distribution.isEnabled ? 'primary' : 'default'}
                        variant={distribution.isEnabled ? 'filled' : 'outlined'}
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption" color="textSecondary" noWrap>
                        {distribution.channelType === 'Email' &&
                          `Recipients: ${config.recipients || 'N/A'}`}
                        {distribution.channelType === 'SFTP' &&
                          `Host: ${config.sftpHost || 'N/A'}`}
                        {distribution.channelType === 'Webhook' &&
                          `URL: ${config.webhookUrl || 'N/A'}`}
                      </Typography>
                    </TableCell>
                    <TableCell align="right">
                      <FormControlLabel
                        control={
                          <Switch
                            size="small"
                            checked={distribution.isEnabled}
                            onChange={() => handleToggleChannel(distribution.id, distribution.isEnabled)}
                            disabled={testingChannelId === distribution.id}
                          />
                        }
                        label=""
                      />
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuOpen(e, distribution.id)}
                        disabled={testingChannelId === distribution.id}
                      >
                        <MoreVertIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      ) : (
        <Card variant="outlined">
          <CardContent sx={{ textAlign: 'center', py: 4 }}>
            <Typography color="textSecondary" gutterBottom>
              No distribution channels configured
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mt: 1 }}>
              Add email, SFTP, or webhook channels to deliver reports automatically
            </Typography>
            <Button
              startIcon={<AddIcon />}
              variant="outlined"
              onClick={() => handleOpenDialog()}
              sx={{ mt: 2 }}
              disabled={loading}
            >
              Add First Channel
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
        <MenuItem
          onClick={() => {
            if (selectedChannelId) {
              const distribution = distributions.find((d) => d.id === selectedChannelId);
              if (distribution) handleOpenDialog(distribution);
            }
            handleMenuClose();
          }}
        >
          <ListItemIcon>
            <EditIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText>Edit</ListItemText>
        </MenuItem>
        <MenuItem
          onClick={() => {
            if (selectedChannelId) handleTestChannel(selectedChannelId);
          }}
          disabled={testingChannelId !== null}
        >
          <ListItemIcon>
            {testingChannelId === selectedChannelId ? (
              <CircularProgress size={20} />
            ) : (
              <EmailIcon fontSize="small" />
            )}
          </ListItemIcon>
          <ListItemText>Test Channel</ListItemText>
        </MenuItem>
        <Divider />
        <MenuItem
          onClick={() => {
            if (selectedChannelId) handleDeleteChannel(selectedChannelId);
          }}
          sx={{ color: 'error.main' }}
        >
          <ListItemIcon>
            <DeleteIcon fontSize="small" color="error" />
          </ListItemIcon>
          <ListItemText>Delete</ListItemText>
        </MenuItem>
      </Menu>

      {/* Channel Configuration Dialog */}
      <Dialog
        open={openDialog}
        onClose={handleCloseDialog}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          {editingId ? 'Edit Distribution Channel' : 'Add Distribution Channel'}
        </DialogTitle>
        <DialogContent dividers>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            {/* Channel Name */}
            <TextField
              fullWidth
              label="Channel Name"
              value={channelForm.name}
              onChange={(e) => setChannelForm({ ...channelForm, name: e.target.value })}
              placeholder="e.g., Daily Email Report"
            />

            {/* Channel Type */}
            <FormControl fullWidth>
              <InputLabel>Channel Type</InputLabel>
              <Select
                value={channelForm.type}
                onChange={(e) =>
                  setChannelForm({
                    ...channelForm,
                    type: e.target.value as 'Email' | 'SFTP' | 'Webhook',
                    config: defaultChannelConfig.config,
                  })
                }
                label="Channel Type"
              >
                <MenuItem value="Email">Email</MenuItem>
                <MenuItem value="SFTP">SFTP Server</MenuItem>
                <MenuItem value="Webhook">Webhook</MenuItem>
              </Select>
            </FormControl>

            {/* Enabled Toggle */}
            <FormControlLabel
              control={
                <Switch
                  checked={channelForm.enabled}
                  onChange={(e) => setChannelForm({ ...channelForm, enabled: e.target.checked })}
                />
              }
              label="Enabled"
            />

            <Divider sx={{ my: 1 }} />

            {/* Email Configuration */}
            {channelForm.type === 'Email' && (
              <>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Email Configuration
                </Typography>

                <TextField
                  fullWidth
                  label="Recipients (comma-separated)"
                  value={channelForm.config.recipients || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, recipients: e.target.value },
                    })
                  }
                  placeholder="user1@example.com, user2@example.com"
                  multiline
                  rows={2}
                />

                <TextField
                  fullWidth
                  label="Subject"
                  value={channelForm.config.subject || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, subject: e.target.value },
                    })
                  }
                  placeholder="Report: {reportName}"
                />

                <TextField
                  fullWidth
                  label="Email Body"
                  value={channelForm.config.body || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, body: e.target.value },
                    })
                  }
                  placeholder="Please find the attached report."
                  multiline
                  rows={3}
                />

                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Retry Attempts"
                      value={channelForm.config.retryAttempts || 3}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: {
                            ...channelForm.config,
                            retryAttempts: parseInt(e.target.value),
                          },
                        })
                      }
                      inputProps={{ min: 1, max: 10 }}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Retry Delay (minutes)"
                      value={channelForm.config.retryDelayMinutes || 5}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: {
                            ...channelForm.config,
                            retryDelayMinutes: parseInt(e.target.value),
                          },
                        })
                      }
                      inputProps={{ min: 1, max: 1440 }}
                    />
                  </Grid>
                </Grid>
              </>
            )}

            {/* SFTP Configuration */}
            {channelForm.type === 'SFTP' && (
              <>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  SFTP Server Configuration
                </Typography>

                <TextField
                  fullWidth
                  label="SFTP Host"
                  value={channelForm.config.sftpHost || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, sftpHost: e.target.value },
                    })
                  }
                  placeholder="sftp.example.com"
                />

                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Port"
                      value={channelForm.config.sftpPort || 22}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: { ...channelForm.config, sftpPort: parseInt(e.target.value) },
                        })
                      }
                      inputProps={{ min: 1, max: 65535 }}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      label="Username"
                      value={channelForm.config.sftpUsername || ''}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: { ...channelForm.config, sftpUsername: e.target.value },
                        })
                      }
                    />
                  </Grid>
                </Grid>

                <TextField
                  fullWidth
                  type="password"
                  label="Password"
                  value={channelForm.config.sftpPassword || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, sftpPassword: e.target.value },
                    })
                  }
                />

                <TextField
                  fullWidth
                  label="Remote Path"
                  value={channelForm.config.sftpPath || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, sftpPath: e.target.value },
                    })
                  }
                  placeholder="/reports/"
                />

                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Retry Attempts"
                      value={channelForm.config.retryAttempts || 3}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: {
                            ...channelForm.config,
                            retryAttempts: parseInt(e.target.value),
                          },
                        })
                      }
                      inputProps={{ min: 1, max: 10 }}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Retry Delay (minutes)"
                      value={channelForm.config.retryDelayMinutes || 5}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: {
                            ...channelForm.config,
                            retryDelayMinutes: parseInt(e.target.value),
                          },
                        })
                      }
                      inputProps={{ min: 1, max: 1440 }}
                    />
                  </Grid>
                </Grid>
              </>
            )}

            {/* Webhook Configuration */}
            {channelForm.type === 'Webhook' && (
              <>
                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                  Webhook Configuration
                </Typography>

                <TextField
                  fullWidth
                  label="Webhook URL"
                  value={channelForm.config.webhookUrl || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, webhookUrl: e.target.value },
                    })
                  }
                  placeholder="https://example.com/webhook"
                />

                <TextField
                  fullWidth
                  label="Custom Headers (JSON)"
                  value={channelForm.config.webhookHeaders || ''}
                  onChange={(e) =>
                    setChannelForm({
                      ...channelForm,
                      config: { ...channelForm.config, webhookHeaders: e.target.value },
                    })
                  }
                  placeholder='{"Authorization": "Bearer token"}'
                  multiline
                  rows={2}
                />

                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Retry Attempts"
                      value={channelForm.config.retryAttempts || 3}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: {
                            ...channelForm.config,
                            retryAttempts: parseInt(e.target.value),
                          },
                        })
                      }
                      inputProps={{ min: 1, max: 10 }}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <TextField
                      fullWidth
                      type="number"
                      label="Retry Delay (minutes)"
                      value={channelForm.config.retryDelayMinutes || 5}
                      onChange={(e) =>
                        setChannelForm({
                          ...channelForm,
                          config: {
                            ...channelForm.config,
                            retryDelayMinutes: parseInt(e.target.value),
                          },
                        })
                      }
                      inputProps={{ min: 1, max: 1440 }}
                    />
                  </Grid>
                </Grid>
              </>
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog} disabled={loading}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleSaveChannel}
            disabled={loading}
          >
            {loading ? <CircularProgress size={24} /> : editingId ? 'Update' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ReportDistribution;
