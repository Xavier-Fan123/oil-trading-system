import React, { useState, useCallback } from 'react';
import {
  Box,
  Container,
  Button,
  Stack,
  CircularProgress,
  Alert,
} from '@mui/material';
import { ContractExecutionReportTable } from '@/components/Reports/ContractExecutionReportTable';
import { ContractExecutionReportFilter } from '@/components/Reports/ContractExecutionReportFilter';
import { ContractExecutionReportSummary } from '@/components/Reports/ContractExecutionReportSummary';
import { ContractExecutionReportDetails } from '@/components/Reports/ContractExecutionReportDetails';
import { ReportExportDialog } from '@/components/Reports/ReportExportDialog';
import { ContractExecutionReportFilter as FilterType, ContractExecutionReportDto } from '@/types/reports';
import { contractExecutionReportApi } from '@/services/contractExecutionReportApi';
import FileDownloadIcon from '@mui/icons-material/FileDownload';

type ViewMode = 'list' | 'details';

export const Reports: React.FC = () => {
  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [filters, setFilters] = useState<FilterType>({
    pageNumber: 1,
    pageSize: 10,
    sortBy: 'ReportGeneratedDate',
    sortDescending: true,
  });

  const [reports, setReports] = useState<ContractExecutionReportDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [selectedReportId, setSelectedReportId] = useState<string | null>(null);
  const [selectedReportIsPurchase, setSelectedReportIsPurchase] = useState(true);

  const [exportDialogOpen, setExportDialogOpen] = useState(false);

  // Load reports with current filters
  const loadReports = useCallback(async (currentFilters: FilterType) => {
    try {
      setLoading(true);
      setError(null);
      const result = await contractExecutionReportApi.getContractReports(
        currentFilters.pageNumber || 1,
        currentFilters.pageSize || 10,
        currentFilters.contractType,
        currentFilters.executionStatus,
        currentFilters.fromDate,
        currentFilters.toDate,
        currentFilters.tradingPartnerId,
        currentFilters.productId,
        currentFilters.sortBy || 'ReportGeneratedDate',
        currentFilters.sortDescending !== false
      );

      setReports(result.items || []);
    } catch (err: any) {
      setError(err.message || 'Failed to load reports');
      console.error('Error loading reports:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  // Handle filter changes
  const handleFilterChange = (newFilters: FilterType) => {
    setFilters(newFilters);
    loadReports(newFilters);
  };

  // Handle report selection
  const handleReportSelect = (report: ContractExecutionReportDto) => {
    setSelectedReportId(report.contractId);
    setSelectedReportIsPurchase(report.contractType === 'Purchase');
    setViewMode('details');
  };

  // Handle back from details
  const handleBackToList = () => {
    setViewMode('list');
    setSelectedReportId(null);
  };

  // Initial load effect
  React.useEffect(() => {
    loadReports(filters);
  }, []);

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* Header with Export Button */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 4 }}>
        <div>
          <h1 style={{ margin: 0, marginBottom: 4 }}>Contract Execution Reports</h1>
          <p style={{ margin: 0, color: '#666', fontSize: '14px' }}>
            Track contract execution status, performance metrics, and settlement information
          </p>
        </div>
        <Button
          variant="contained"
          startIcon={<FileDownloadIcon />}
          onClick={() => setExportDialogOpen(true)}
          disabled={reports.length === 0}
        >
          Export
        </Button>
      </Stack>

      {error && (
        <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {viewMode === 'list' ? (
        <>
          {/* Filters */}
          <ContractExecutionReportFilter
            onFilterChange={handleFilterChange}
            isLoading={loading}
          />

          {/* Summary Statistics */}
          {!loading && reports.length > 0 && (
            <ContractExecutionReportSummary
              reports={reports}
              isLoading={loading}
              filters={filters}
            />
          )}

          {/* Reports Table */}
          {loading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
              <CircularProgress />
            </Box>
          ) : reports.length > 0 ? (
            <ContractExecutionReportTable
              filters={filters}
              onReportSelect={handleReportSelect}
            />
          ) : (
            <Alert severity="info">
              No reports found. Try adjusting your filters or create new contracts.
            </Alert>
          )}
        </>
      ) : (
        /* Details View */
        selectedReportId && (
          <ContractExecutionReportDetails
            contractId={selectedReportId}
            isPurchaseContract={selectedReportIsPurchase}
            onBack={handleBackToList}
          />
        )
      )}

      {/* Export Dialog */}
      <ReportExportDialog
        open={exportDialogOpen}
        onClose={() => setExportDialogOpen(false)}
        filters={filters}
      />
    </Container>
  );
};

export default Reports;
