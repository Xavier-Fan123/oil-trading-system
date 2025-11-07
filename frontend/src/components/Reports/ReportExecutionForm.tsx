import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Stack,
  CircularProgress,
  LinearProgress,
} from '@mui/material';
import { useMutation, useQuery } from '@tanstack/react-query';
import reportingApi, { ReportConfiguration } from '@/services/reportingApi';

interface ReportExecutionFormProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

const ReportExecutionForm: React.FC<ReportExecutionFormProps> = ({
  open,
  onClose,
  onSuccess,
}) => {
  const [formData, setFormData] = useState({
    reportConfigurationId: '',
    outputFormat: 'CSV',
    isScheduled: false,
    parameters: {} as Record<string, unknown>,
  });

  const [parameterInput, setParameterInput] = useState('');
  const [parameterValue, setParameterValue] = useState('');

  // Fetch configurations for dropdown
  const { data: configs, isLoading: configsLoading } = useQuery({
    queryKey: ['reportConfigurations', 1],
    queryFn: () => reportingApi.listConfigurations(1, 100),
  });

  // Execute mutation
  const executeMutation = useMutation({
    mutationFn: (data: typeof formData) => reportingApi.executeReport(data),
    onSuccess: () => {
      resetForm();
      onSuccess();
    },
  });

  useEffect(() => {
    if (!open) {
      resetForm();
    }
  }, [open]);

  const resetForm = () => {
    setFormData({
      reportConfigurationId: '',
      outputFormat: 'CSV',
      isScheduled: false,
      parameters: {},
    });
    setParameterInput('');
    setParameterValue('');
  };

  const handleConfigChange = (event: any) => {
    setFormData({
      ...formData,
      reportConfigurationId: event.target.value,
    });
  };

  const handleFormatChange = (event: any) => {
    setFormData({
      ...formData,
      outputFormat: event.target.value,
    });
  };

  const handleAddParameter = () => {
    if (parameterInput.trim() && parameterValue.trim()) {
      setFormData({
        ...formData,
        parameters: {
          ...formData.parameters,
          [parameterInput]: parameterValue,
        },
      });
      setParameterInput('');
      setParameterValue('');
    }
  };

  const handleRemoveParameter = (key: string) => {
    const newParams = { ...formData.parameters };
    delete newParams[key];
    setFormData({
      ...formData,
      parameters: newParams,
    });
  };

  const handleSubmit = () => {
    if (!formData.reportConfigurationId) {
      alert('Please select a report configuration');
      return;
    }

    executeMutation.mutate(formData);
  };

  const isLoading = executeMutation.isPending || configsLoading;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Execute Report</DialogTitle>

      <DialogContent sx={{ pt: 3 }}>
        {executeMutation.isPending && (
          <Box sx={{ mb: 2 }}>
            <LinearProgress />
            <Box sx={{ textAlign: 'center', mt: 1, fontSize: '0.875rem', color: '#666' }}>
              Report is being executed... This may take a moment.
            </Box>
          </Box>
        )}

        <Stack spacing={2}>
          {/* Report Configuration */}
          <FormControl fullWidth disabled={isLoading}>
            <InputLabel>Report Configuration</InputLabel>
            <Select
              value={formData.reportConfigurationId}
              onChange={handleConfigChange}
              label="Report Configuration"
            >
              <MenuItem value="">
                <em>Select a configuration</em>
              </MenuItem>
              {configs?.items?.map((config) => (
                <MenuItem key={config.id} value={config.id}>
                  {config.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Output Format */}
          <FormControl fullWidth disabled={isLoading}>
            <InputLabel>Output Format</InputLabel>
            <Select
              value={formData.outputFormat}
              onChange={handleFormatChange}
              label="Output Format"
            >
              <MenuItem value="CSV">CSV</MenuItem>
              <MenuItem value="Excel">Excel</MenuItem>
              <MenuItem value="PDF">PDF</MenuItem>
              <MenuItem value="JSON">JSON</MenuItem>
            </Select>
          </FormControl>

          {/* Parameters */}
          <Box>
            <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
              <TextField
                fullWidth
                label="Parameter Name"
                value={parameterInput}
                onChange={(e) => setParameterInput(e.target.value)}
                size="small"
                disabled={isLoading}
              />
              <TextField
                fullWidth
                label="Parameter Value"
                value={parameterValue}
                onChange={(e) => setParameterValue(e.target.value)}
                size="small"
                disabled={isLoading}
                onKeyPress={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    handleAddParameter();
                  }
                }}
              />
              <Button
                variant="outlined"
                onClick={handleAddParameter}
                disabled={isLoading}
                sx={{ minWidth: 80 }}
              >
                Add
              </Button>
            </Stack>

            {Object.entries(formData.parameters).length > 0 && (
              <Box sx={{
                p: 1.5,
                backgroundColor: '#f5f5f5',
                borderRadius: 1,
                maxHeight: 150,
                overflowY: 'auto',
              }}>
                {Object.entries(formData.parameters).map(([key, value]) => (
                  <Box
                    key={key}
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      py: 0.5,
                      fontSize: '0.875rem',
                    }}
                  >
                    <span>
                      <strong>{key}:</strong> {String(value)}
                    </span>
                    <Button
                      size="small"
                      color="error"
                      onClick={() => handleRemoveParameter(key)}
                      disabled={isLoading}
                    >
                      Remove
                    </Button>
                  </Box>
                ))}
              </Box>
            )}
          </Box>
        </Stack>
      </DialogContent>

      <DialogActions>
        <Button onClick={onClose} disabled={isLoading}>
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          color="primary"
          disabled={isLoading || !formData.reportConfigurationId}
        >
          {executeMutation.isPending ? (
            <>
              <CircularProgress size={20} sx={{ mr: 1 }} />
              Executing...
            </>
          ) : (
            'Execute'
          )}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ReportExecutionForm;
