import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  FormControlLabel,
  Checkbox,
  Box,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Stack,
  Chip,
  Grid,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import reportingApi, { ReportConfiguration } from '@/services/reportingApi';

interface ReportConfigurationFormProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  config?: ReportConfiguration;
}

const ReportConfigurationForm: React.FC<ReportConfigurationFormProps> = ({
  open,
  onClose,
  onSuccess,
  config,
}) => {
  const isEditing = !!config?.id;

  const [formData, setFormData] = useState<ReportConfiguration>({
    name: '',
    description: '',
    reportType: '',
    exportFormat: 'CSV',
    includeMetadata: false,
    isActive: true,
    filters: {},
    columns: [],
  });

  const [columnInput, setColumnInput] = useState('');

  useEffect(() => {
    if (config) {
      setFormData(config);
    } else {
      setFormData({
        name: '',
        description: '',
        reportType: '',
        exportFormat: 'CSV',
        includeMetadata: false,
        isActive: true,
        filters: {},
        columns: [],
      });
    }
    setColumnInput('');
  }, [config, open]);

  // Create mutation
  const createMutation = useMutation({
    mutationFn: (data: ReportConfiguration) => reportingApi.createConfiguration(data),
    onSuccess,
  });

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: (data: ReportConfiguration) =>
      reportingApi.updateConfiguration(config!.id!, data),
    onSuccess,
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value } = e.target as HTMLInputElement;

    if (name === 'includeMetadata' || name === 'isActive') {
      setFormData({
        ...formData,
        [name]: (e.target as HTMLInputElement).checked,
      });
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
  };

  const handleSelectChange = (event: any) => {
    setFormData({
      ...formData,
      [event.target.name]: event.target.value,
    });
  };

  const handleAddColumn = () => {
    if (columnInput.trim()) {
      setFormData({
        ...formData,
        columns: [...(formData.columns || []), columnInput.trim()],
      });
      setColumnInput('');
    }
  };

  const handleRemoveColumn = (column: string) => {
    setFormData({
      ...formData,
      columns: (formData.columns || []).filter((c) => c !== column),
    });
  };

  const handleSubmit = () => {
    if (!formData.name || !formData.reportType) {
      alert('Please fill in all required fields');
      return;
    }

    if (isEditing) {
      updateMutation.mutate(formData);
    } else {
      createMutation.mutate(formData);
    }
  };

  const isLoading = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {isEditing ? 'Edit Report Configuration' : 'Create New Report Configuration'}
      </DialogTitle>

      <DialogContent sx={{ pt: 3 }}>
        <Stack spacing={2}>
          {/* Name */}
          <TextField
            fullWidth
            label="Configuration Name"
            name="name"
            value={formData.name}
            onChange={handleChange}
            required
            error={!formData.name}
            helperText={!formData.name ? 'Name is required' : ''}
          />

          {/* Description */}
          <TextField
            fullWidth
            label="Description"
            name="description"
            value={formData.description || ''}
            onChange={handleChange}
            multiline
            rows={2}
          />

          {/* Report Type */}
          <TextField
            fullWidth
            label="Report Type"
            name="reportType"
            value={formData.reportType}
            onChange={handleChange}
            required
            error={!formData.reportType}
            helperText={!formData.reportType ? 'Report Type is required' : 'e.g., Sales, Executive, Analytics'}
          />

          {/* Export Format */}
          <FormControl fullWidth>
            <InputLabel>Export Format</InputLabel>
            <Select
              name="exportFormat"
              value={formData.exportFormat}
              onChange={handleSelectChange}
              label="Export Format"
            >
              <MenuItem value="CSV">CSV</MenuItem>
              <MenuItem value="Excel">Excel</MenuItem>
              <MenuItem value="PDF">PDF</MenuItem>
              <MenuItem value="JSON">JSON</MenuItem>
            </Select>
          </FormControl>

          {/* Columns */}
          <Box>
            <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
              <TextField
                fullWidth
                label="Add Column"
                value={columnInput}
                onChange={(e) => setColumnInput(e.target.value)}
                onKeyPress={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    handleAddColumn();
                  }
                }}
                size="small"
              />
              <Button
                variant="outlined"
                onClick={handleAddColumn}
                sx={{ minWidth: 80 }}
              >
                Add
              </Button>
            </Stack>

            {formData.columns && formData.columns.length > 0 && (
              <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                {formData.columns.map((column) => (
                  <Chip
                    key={column}
                    label={column}
                    onDelete={() => handleRemoveColumn(column)}
                    color="primary"
                    variant="outlined"
                  />
                ))}
              </Stack>
            )}
          </Box>

          {/* Checkboxes */}
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <FormControlLabel
                control={
                  <Checkbox
                    name="includeMetadata"
                    checked={formData.includeMetadata}
                    onChange={handleChange}
                  />
                }
                label="Include Metadata"
              />
            </Grid>
            <Grid item xs={6}>
              <FormControlLabel
                control={
                  <Checkbox
                    name="isActive"
                    checked={formData.isActive}
                    onChange={handleChange}
                  />
                }
                label="Active"
              />
            </Grid>
          </Grid>
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          color="primary"
          disabled={isLoading}
        >
          {isLoading
            ? 'Saving...'
            : isEditing
            ? 'Update'
            : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ReportConfigurationForm;
