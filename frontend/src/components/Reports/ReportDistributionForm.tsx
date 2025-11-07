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
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import reportingApi, { ReportDistribution } from '@/services/reportingApi';

interface ReportDistributionFormProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
  distribution?: ReportDistribution;
}

const ReportDistributionForm: React.FC<ReportDistributionFormProps> = ({
  open,
  onClose,
  onSuccess,
  distribution,
}) => {
  const isEditing = !!distribution?.id;

  const [formData, setFormData] = useState<ReportDistribution>({
    reportConfigId: '',
    channelName: '',
    channelType: '',
    isEnabled: true,
  });

  useEffect(() => {
    if (distribution) {
      setFormData(distribution);
    } else {
      setFormData({
        reportConfigId: '',
        channelName: '',
        channelType: '',
        isEnabled: true,
      });
    }
  }, [distribution, open]);

  const createMutation = useMutation({
    mutationFn: (data: ReportDistribution) => reportingApi.createDistribution(data),
    onSuccess,
  });

  const updateMutation = useMutation({
    mutationFn: (data: ReportDistribution) =>
      reportingApi.updateDistribution(distribution!.id!, data),
    onSuccess,
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>) => {
    const { name, value } = e.target as HTMLInputElement;

    if (name === 'isEnabled') {
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

  const handleSubmit = () => {
    if (!formData.channelName || !formData.channelType) {
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
        {isEditing ? 'Edit Distribution Channel' : 'Create New Distribution Channel'}
      </DialogTitle>

      <DialogContent sx={{ pt: 3 }}>
        <Stack spacing={2}>
          <TextField
            fullWidth
            label="Channel Name"
            name="channelName"
            value={formData.channelName}
            onChange={handleChange}
            required
            disabled={isLoading}
          />

          <FormControl fullWidth disabled={isLoading}>
            <InputLabel>Channel Type</InputLabel>
            <Select
              name="channelType"
              value={formData.channelType}
              onChange={handleSelectChange}
              label="Channel Type"
            >
              <MenuItem value="">Select type</MenuItem>
              <MenuItem value="Email">Email</MenuItem>
              <MenuItem value="SFTP">SFTP</MenuItem>
              <MenuItem value="Webhook">Webhook</MenuItem>
              <MenuItem value="FTP">FTP</MenuItem>
              <MenuItem value="S3">AWS S3</MenuItem>
              <MenuItem value="Azure">Azure Blob</MenuItem>
            </Select>
          </FormControl>

          <TextField
            fullWidth
            label="Configuration (JSON)"
            name="channelConfiguration"
            value={JSON.stringify(formData.channelConfiguration || {}, null, 2)}
            onChange={(e) => {
              try {
                setFormData({
                  ...formData,
                  channelConfiguration: JSON.parse(e.target.value),
                });
              } catch {
                // Invalid JSON, skip
              }
            }}
            multiline
            rows={3}
            disabled={isLoading}
            helperText="Enter configuration as valid JSON"
          />

          <FormControlLabel
            control={
              <Checkbox
                name="isEnabled"
                checked={formData.isEnabled}
                onChange={handleChange}
                disabled={isLoading}
              />
            }
            label="Enabled"
          />
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
          disabled={isLoading}
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ReportDistributionForm;
