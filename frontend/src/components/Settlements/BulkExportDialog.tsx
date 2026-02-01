import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  RadioGroup,
  FormControlLabel,
  Radio,
  Stack,
  Typography,
  Alert,
  CircularProgress,
  Box,
  FormControl,
  FormLabel,
} from '@mui/material';
import {
  Download as DownloadIcon,
} from '@mui/icons-material';

export type ExportFormat = 'excel' | 'csv' | 'pdf';

export interface BulkExportDialogProps {
  selectedCount: number;
  onClose: () => void;
  onExport: (format: ExportFormat) => Promise<void>;
}

/**
 * BulkExportDialog Component
 * Dialog for selecting export format and options
 */
export const BulkExportDialog: React.FC<BulkExportDialogProps> = ({
  selectedCount,
  onClose,
  onExport,
}) => {
  const [format, setFormat] = useState<ExportFormat>('excel');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleExport = async () => {
    setLoading(true);
    setError(null);

    try {
      await onExport(format);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Export failed');
    } finally {
      setLoading(false);
    }
  };

  const formatDescriptions: Record<ExportFormat, { label: string; description: string }> = {
    excel: {
      label: 'Excel Workbook (.xlsx)',
      description: 'Professional Excel format with formatting, formulas, and multiple sheets',
    },
    csv: {
      label: 'Comma-Separated Values (.csv)',
      description: 'Plain text format for import into other systems',
    },
    pdf: {
      label: 'PDF Document (.pdf)',
      description: 'Professional PDF format for printing and archiving',
    },
  };

  return (
    <Dialog open={true} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Export Settlements</DialogTitle>
      <DialogContent>
        <Stack spacing={3} sx={{ pt: 2 }}>
          <Alert severity="info">
            Exporting <strong>{selectedCount}</strong> settlement(s) in {formatDescriptions[format].label} format
          </Alert>

          <FormControl component="fieldset">
            <FormLabel component="legend" sx={{ mb: 2 }}>
              Select Export Format
            </FormLabel>
            <RadioGroup
              value={format}
              onChange={(e) => setFormat(e.target.value as ExportFormat)}
            >
              {(Object.keys(formatDescriptions) as ExportFormat[]).map((fmt) => (
                <Box key={fmt}>
                  <FormControlLabel
                    value={fmt}
                    control={<Radio />}
                    label={
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                          {formatDescriptions[fmt].label}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatDescriptions[fmt].description}
                        </Typography>
                      </Box>
                    }
                    sx={{ mb: 1.5 }}
                  />
                </Box>
              ))}
            </RadioGroup>
          </FormControl>

          {/* Export Features */}
          <Box sx={{ bgcolor: 'grey.50', p: 2, borderRadius: 1 }}>
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Export Includes:
            </Typography>
            <ul style={{ margin: '0 0 0 20px', paddingLeft: 0 }}>
              <li>
                <Typography variant="caption">Settlement numbers and IDs</Typography>
              </li>
              <li>
                <Typography variant="caption">Status and workflow stage</Typography>
              </li>
              <li>
                <Typography variant="caption">Quantities (MT, BBL, GAL)</Typography>
              </li>
              <li>
                <Typography variant="caption">Pricing and calculation details</Typography>
              </li>
              <li>
                <Typography variant="caption">Settlement amounts and currency</Typography>
              </li>
              <li>
                <Typography variant="caption">Created/Modified dates and user info</Typography>
              </li>
            </ul>
          </Box>

          {error && <Alert severity="error">{error}</Alert>}

          {loading && <CircularProgress />}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button
          onClick={handleExport}
          variant="contained"
          startIcon={<DownloadIcon />}
          disabled={loading}
        >
          Export
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default BulkExportDialog;
