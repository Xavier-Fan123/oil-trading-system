import React, { useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormLabel,
  RadioGroup,
  Radio,
  CircularProgress,
  Alert,
  Stack,
  Typography,
  Box,
  Checkbox,
  FormGroup,
} from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import { ContractExecutionReportFilter } from '@/types/reports';
import { contractExecutionReportApi } from '@/services/contractExecutionReportApi';

interface ReportExportDialogProps {
  open: boolean;
  onClose: () => void;
  filters?: ContractExecutionReportFilter;
}

type ExportFormat = 'csv' | 'excel' | 'pdf';

interface ExportOptions {
  format: ExportFormat;
  includeMetrics: boolean;
  includePricing: boolean;
  includeSettlement: boolean;
  includeDates: boolean;
}

export const ReportExportDialog: React.FC<ReportExportDialogProps> = ({
  open,
  onClose,
  filters,
}) => {
  const [exportOptions, setExportOptions] = useState<ExportOptions>({
    format: 'csv',
    includeMetrics: true,
    includePricing: true,
    includeSettlement: true,
    includeDates: true,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFormatChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setExportOptions({
      ...exportOptions,
      format: event.target.value as ExportFormat,
    });
  };

  const handleCheckboxChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setExportOptions({
      ...exportOptions,
      [event.target.name]: event.target.checked,
    });
  };

  const handleExport = async () => {
    try {
      setLoading(true);
      setError(null);

      let blob: Blob;
      let filename: string;

      const pageNum = filters?.pageNumber || 1;
      const pageSize = filters?.pageSize || 1000;
      const contractType = filters?.contractType;
      const executionStatus = filters?.executionStatus;
      const fromDate = filters?.fromDate;
      const toDate = filters?.toDate;
      const tradingPartnerId = filters?.tradingPartnerId;
      const productId = filters?.productId;
      const sortBy = filters?.sortBy;
      const sortDescending = filters?.sortDescending;

      const dateStr = new Date().toISOString().split('T')[0];

      if (exportOptions.format === 'csv') {
        blob = await contractExecutionReportApi.exportReportsToCsv(
          pageNum, pageSize, contractType, executionStatus, fromDate, toDate,
          tradingPartnerId, productId, sortBy, sortDescending
        );
        filename = `contract-execution-reports-${dateStr}.csv`;
      } else if (exportOptions.format === 'excel') {
        blob = await contractExecutionReportApi.exportReportsToExcel(
          pageNum, pageSize, contractType, executionStatus, fromDate, toDate,
          tradingPartnerId, productId, sortBy, sortDescending
        );
        filename = `contract-execution-reports-${dateStr}.xlsx`;
      } else {
        blob = await contractExecutionReportApi.exportReportsToPdf(
          pageNum, pageSize, contractType, executionStatus, fromDate, toDate,
          tradingPartnerId, productId, sortBy, sortDescending
        );
        filename = `contract-execution-reports-${dateStr}.pdf`;
      }

      // Create download link and trigger download
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(link);

      onClose();
    } catch (err: any) {
      setError(err.message || 'Failed to export reports');
      console.error('Error exporting reports:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Export Reports</DialogTitle>

      {error && (
        <Box sx={{ px: 3, pt: 2 }}>
          <Alert severity="error" onClose={() => setError(null)}>
            {error}
          </Alert>
        </Box>
      )}

      <DialogContent sx={{ pt: 3 }}>
        <Stack spacing={3}>
          {/* Export Format */}
          <FormControl component="fieldset">
            <FormLabel component="legend" sx={{ mb: 1 }}>
              Export Format
            </FormLabel>
            <RadioGroup
              value={exportOptions.format}
              onChange={handleFormatChange}
            >
              <FormControlLabel
                value="csv"
                control={<Radio />}
                label="CSV (Comma-Separated Values)"
              />
              <FormControlLabel
                value="excel"
                control={<Radio />}
                label="Excel (.xlsx)"
              />
              <FormControlLabel
                value="pdf"
                control={<Radio />}
                label="PDF (Formatted Report)"
              />
            </RadioGroup>
          </FormControl>

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1.5, fontWeight: 600 }}>
              Included Columns
            </Typography>
            <FormGroup>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={exportOptions.includeMetrics}
                    onChange={handleCheckboxChange}
                    name="includeMetrics"
                    disabled={loading}
                  />
                }
                label="Execution Metrics (%, Status, Quantity)"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={exportOptions.includePricing}
                    onChange={handleCheckboxChange}
                    name="includePricing"
                    disabled={loading}
                  />
                }
                label="Pricing Information (Benchmark, Final Price)"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={exportOptions.includeSettlement}
                    onChange={handleCheckboxChange}
                    name="includeSettlement"
                    disabled={loading}
                  />
                }
                label="Settlement Information (Amount, Status)"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={exportOptions.includeDates}
                    onChange={handleCheckboxChange}
                    name="includeDates"
                    disabled={loading}
                  />
                }
                label="Dates (Creation, Delivery, Laycan)"
              />
            </FormGroup>
          </Box>

          {/* Export Info */}
          <Alert severity="info" sx={{ mt: 2 }}>
            <Typography variant="body2">
              {exportOptions.format === 'csv' &&
                'CSV format is ideal for data analysis and importing into spreadsheet applications.'}
              {exportOptions.format === 'excel' &&
                'Excel format includes formatting, multiple sheets, and advanced features.'}
              {exportOptions.format === 'pdf' &&
                'PDF format provides a formatted, printable report with professional styling.'}
            </Typography>
          </Alert>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ p: 2 }}>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button
          onClick={handleExport}
          variant="contained"
          startIcon={loading ? <CircularProgress size={20} /> : <DownloadIcon />}
          disabled={loading}
        >
          {loading ? 'Exporting...' : 'Export'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
