import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Stepper,
  Step,
  StepLabel,
  Typography,
  Alert,
  CircularProgress,
  TextField,
  Checkbox,
  FormControlLabel,
  Stack,
  Divider,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Grid,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  ArrowForward as NextIcon,
  Save as SaveIcon,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  ReportConfiguration,
  ReportType,
  ReportFormat,
  ReportColumn,
  REPORT_TYPE_LABELS,
  REPORT_FORMAT_LABELS,
} from '@/types/advancedReporting';
import { advancedReportingApi } from '@/services/advancedReportingApi';

interface ReportBuilderProps {
  existingConfig?: ReportConfiguration;
  onSave: (config: ReportConfiguration) => void;
  onCancel: () => void;
  isLoading?: boolean;
}

const REPORT_TYPES = Object.entries(REPORT_TYPE_LABELS).map(([key, label]) => ({
  value: key as ReportType,
  label,
}));

const REPORT_FORMATS = Object.entries(REPORT_FORMAT_LABELS).map(([key, label]) => ({
  value: key as ReportFormat,
  label,
}));

const DEFAULT_COLUMNS: ReportColumn[] = [
  {
    id: 'contractNumber',
    name: 'contractNumber',
    displayName: 'Contract #',
    dataType: 'string',
    visible: true,
    sortable: true,
    filterable: true,
  },
  {
    id: 'status',
    name: 'status',
    displayName: 'Status',
    dataType: 'string',
    visible: true,
    sortable: true,
    filterable: true,
  },
  {
    id: 'amount',
    name: 'amount',
    displayName: 'Amount',
    dataType: 'currency',
    visible: true,
    sortable: true,
    filterable: false,
  },
  {
    id: 'date',
    name: 'date',
    displayName: 'Date',
    dataType: 'date',
    visible: true,
    sortable: true,
    filterable: true,
  },
];

const steps = ['Basic Info', 'Filters', 'Columns', 'Format & Save'];

export const ReportBuilder: React.FC<ReportBuilderProps> = ({
  existingConfig,
  onSave,
  onCancel,
  isLoading = false,
}) => {
  const [activeStep, setActiveStep] = useState(0);
  const [config, setConfig] = useState<ReportConfiguration>(
    existingConfig || {
      name: '',
      description: '',
      reportType: ReportType.ContractExecution,
      filters: {},
      columns: DEFAULT_COLUMNS,
      format: ReportFormat.Excel,
      includeMetadata: true,
    }
  );
  const [error, setError] = useState<string | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  // Validation
  const validateStep = (step: number): boolean => {
    switch (step) {
      case 0: // Basic Info
        return !!(config.name.trim() && config.reportType);
      case 1: // Filters
        return true; // Optional
      case 2: // Columns
        return config.columns.some((c) => c.visible);
      case 3: // Format & Save
        return !!config.format;
      default:
        return true;
    }
  };

  const handleNext = () => {
    if (validateStep(activeStep)) {
      setActiveStep((prev) => prev + 1);
      setError(null);
    } else {
      setError('Please complete all required fields in this step');
    }
  };

  const handleBack = () => {
    setActiveStep((prev) => Math.max(0, prev - 1));
    setError(null);
  };

  const handleSave = async () => {
    if (!validateStep(3)) {
      setError('Please complete all required fields');
      return;
    }

    try {
      onSave(config);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save report');
    }
  };

  const handlePreview = async () => {
    setPreviewLoading(true);
    setError(null);
    try {
      await advancedReportingApi.previewReport(config);
      // Show success toast or navigate to preview
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to preview report');
    } finally {
      setPreviewLoading(false);
    }
  };

  const toggleColumnVisibility = (columnId: string) => {
    setConfig((prev) => ({
      ...prev,
      columns: prev.columns.map((col) =>
        col.id === columnId ? { ...col, visible: !col.visible } : col
      ),
    }));
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h5" gutterBottom>
          {existingConfig ? 'Edit Report' : 'Create New Report'}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <Stepper activeStep={activeStep} sx={{ my: 3 }}>
          {steps.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        <Box sx={{ minHeight: 300, mb: 3 }}>
          {/* Step 0: Basic Info */}
          {activeStep === 0 && (
            <Stack spacing={2}>
              <TextField
                fullWidth
                label="Report Name"
                value={config.name}
                onChange={(e) => setConfig((prev) => ({ ...prev, name: e.target.value }))}
                placeholder="e.g., Monthly Contract Report"
              />

              <TextField
                fullWidth
                label="Description"
                value={config.description || ''}
                onChange={(e) =>
                  setConfig((prev) => ({ ...prev, description: e.target.value }))
                }
                placeholder="Describe what this report shows"
                multiline
                rows={3}
              />

              <FormControl fullWidth>
                <InputLabel>Report Type</InputLabel>
                <Select
                  value={config.reportType}
                  onChange={(e) =>
                    setConfig((prev) => ({
                      ...prev,
                      reportType: e.target.value as ReportType,
                    }))
                  }
                  label="Report Type"
                >
                  {REPORT_TYPES.map((type) => (
                    <MenuItem key={type.value} value={type.value}>
                      {type.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
          )}

          {/* Step 1: Filters */}
          {activeStep === 1 && (
            <Stack spacing={2}>
              <Typography variant="subtitle2" gutterBottom>
                Optional Filters
              </Typography>

              <LocalizationProvider dateAdapter={AdapterDateFns}>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <DatePicker
                      label="Start Date"
                      value={config.filters?.dateRange?.startDate || null}
                      onChange={(date) => {
                        if (date) {
                          setConfig((prev) => ({
                            ...prev,
                            filters: {
                              ...prev.filters,
                              dateRange: {
                                startDate: date,
                                endDate: prev.filters?.dateRange?.endDate || new Date(),
                              },
                            },
                          }));
                        }
                      }}
                      slotProps={{ textField: { fullWidth: true } }}
                    />
                  </Grid>

                  <Grid item xs={12} sm={6}>
                    <DatePicker
                      label="End Date"
                      value={config.filters?.dateRange?.endDate || null}
                      onChange={(date) => {
                        if (date) {
                          setConfig((prev) => ({
                            ...prev,
                            filters: {
                              ...prev.filters,
                              dateRange: {
                                startDate: prev.filters?.dateRange?.startDate || new Date(),
                                endDate: date,
                              },
                            },
                          }));
                        }
                      }}
                      slotProps={{ textField: { fullWidth: true } }}
                    />
                  </Grid>
                </Grid>
              </LocalizationProvider>

              <TextField
                fullWidth
                label="Contract Type"
                placeholder="e.g., Purchase, Sales"
                value={config.filters?.contractType || ''}
                onChange={(e) =>
                  setConfig((prev) => ({
                    ...prev,
                    filters: { ...prev.filters, contractType: e.target.value as "Purchase" | "Sales" },
                  }))
                }
              />

              <TextField
                fullWidth
                label="Execution Status"
                placeholder="e.g., OnTrack, Completed, Delayed"
                value={config.filters?.executionStatus || ''}
                onChange={(e) =>
                  setConfig((prev) => ({
                    ...prev,
                    filters: { ...prev.filters, executionStatus: e.target.value },
                  }))
                }
              />

              <Typography variant="caption" color="textSecondary">
                Leave filters empty to include all data
              </Typography>
            </Stack>
          )}

          {/* Step 2: Columns */}
          {activeStep === 2 && (
            <Stack spacing={2}>
              <Typography variant="subtitle2" gutterBottom>
                Select Columns to Include
              </Typography>

              <Paper variant="outlined">
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow sx={{ backgroundColor: 'background.default' }}>
                        <TableCell width={50}>Show</TableCell>
                        <TableCell>Column Name</TableCell>
                        <TableCell width={120}>Data Type</TableCell>
                        <TableCell width={80}>Sortable</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {config.columns.map((column) => (
                        <TableRow key={column.id}>
                          <TableCell>
                            <Checkbox
                              checked={column.visible}
                              onChange={() => toggleColumnVisibility(column.id)}
                            />
                          </TableCell>
                          <TableCell>{column.displayName}</TableCell>
                          <TableCell>
                            <Typography variant="caption">{column.dataType}</Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="caption">
                              {column.sortable ? 'Yes' : 'No'}
                            </Typography>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Paper>

              <Typography variant="caption" color="textSecondary">
                At least one column must be selected
              </Typography>
            </Stack>
          )}

          {/* Step 3: Format & Save */}
          {activeStep === 3 && (
            <Stack spacing={2}>
              <FormControl fullWidth>
                <InputLabel>Export Format</InputLabel>
                <Select
                  value={config.format}
                  onChange={(e) =>
                    setConfig((prev) => ({
                      ...prev,
                      format: e.target.value as ReportFormat,
                    }))
                  }
                  label="Export Format"
                >
                  {REPORT_FORMATS.map((format) => (
                    <MenuItem key={format.value} value={format.value}>
                      {format.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>

              <FormControlLabel
                control={
                  <Checkbox
                    checked={config.includeMetadata}
                    onChange={(e) =>
                      setConfig((prev) => ({
                        ...prev,
                        includeMetadata: e.target.checked,
                      }))
                    }
                  />
                }
                label="Include metadata (generated date, filters, etc.)"
              />

              <Divider sx={{ my: 2 }} />

              <Paper sx={{ p: 2, backgroundColor: 'info.light' }}>
                <Typography variant="subtitle2" gutterBottom>
                  Report Summary
                </Typography>
                <Stack spacing={1}>
                  <Typography variant="body2">
                    <strong>Name:</strong> {config.name || '(Not set)'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Type:</strong> {REPORT_TYPE_LABELS[config.reportType]}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Columns:</strong> {config.columns.filter((c) => c.visible).length}{' '}
                    visible
                  </Typography>
                  <Typography variant="body2">
                    <strong>Format:</strong> {REPORT_FORMAT_LABELS[config.format]}
                  </Typography>
                </Stack>
              </Paper>

              <Button onClick={handlePreview} disabled={previewLoading}>
                {previewLoading && <CircularProgress size={20} sx={{ mr: 1 }} />}
                Preview Report
              </Button>
            </Stack>
          )}
        </Box>

        {/* Navigation Buttons */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1 }}>
          <Button onClick={onCancel}>Cancel</Button>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              onClick={handleBack}
              disabled={activeStep === 0}
              startIcon={<BackIcon />}
            >
              Back
            </Button>

            {activeStep === steps.length - 1 ? (
              <Button
                variant="contained"
                onClick={handleSave}
                startIcon={<SaveIcon />}
                disabled={isLoading}
              >
                {isLoading ? <CircularProgress size={20} /> : 'Save Report'}
              </Button>
            ) : (
              <Button
                variant="contained"
                onClick={handleNext}
                endIcon={<NextIcon />}
              >
                Next
              </Button>
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
};

export default ReportBuilder;
