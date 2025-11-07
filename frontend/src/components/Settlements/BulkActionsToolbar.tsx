import React, { useState } from 'react';
import {
  Box,
  Button,
  ButtonGroup,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  CircularProgress,
  Stack,
} from '@mui/material';
import {
  CheckCircle as CheckIcon,
  Done as DoneIcon,
  FileDownload as ExportIcon,
  Close as ClearIcon,
} from '@mui/icons-material';
import { bulkSettlementApi } from '../../services/settlementApi';
import { BulkExportDialog, ExportFormat } from './BulkExportDialog';

export interface BulkActionsToolbarProps {
  selectedCount: number;
  selectedIds?: Set<string>;
  onClearSelection: () => void;
  onBulkApprove?: () => void;
  onBulkFinalize?: () => void;
  onRefresh?: () => void;
}

/**
 * BulkActionsToolbar Component
 * Displays action buttons for bulk settlement operations
 */
export const BulkActionsToolbar: React.FC<BulkActionsToolbarProps> = ({
  selectedCount,
  selectedIds,
  onClearSelection,
  onBulkApprove,
  onBulkFinalize,
  onRefresh,
}) => {
  const [showApproveDialog, setShowApproveDialog] = useState(false);
  const [showFinalizeDialog, setShowFinalizeDialog] = useState(false);
  const [showExportDialog, setShowExportDialog] = useState(false);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  if (selectedCount === 0) {
    return null;
  }

  const handleApprove = async () => {
    setLoading(true);
    try {
      if (!selectedIds || selectedIds.size === 0) {
        throw new Error('No settlements selected');
      }
      const ids = Array.from(selectedIds);
      const result = await bulkSettlementApi.bulkApprove(ids);
      setMessage({
        type: 'success',
        text: `${result.successCount} settlement(s) approved successfully${result.failureCount > 0 ? `, ${result.failureCount} failed` : ''}`,
      });
      onBulkApprove?.();
      setShowApproveDialog(false);
      onRefresh?.();
    } catch (error) {
      setMessage({
        type: 'error',
        text: `Failed to approve settlements: ${error instanceof Error ? error.message : 'Unknown error'}`,
      });
    } finally {
      setLoading(false);
    }
  };

  const handleFinalize = async () => {
    setLoading(true);
    try {
      if (!selectedIds || selectedIds.size === 0) {
        throw new Error('No settlements selected');
      }
      const ids = Array.from(selectedIds);
      const result = await bulkSettlementApi.bulkFinalize(ids);
      setMessage({
        type: 'success',
        text: `${result.successCount} settlement(s) finalized successfully${result.failureCount > 0 ? `, ${result.failureCount} failed` : ''}`,
      });
      onBulkFinalize?.();
      setShowFinalizeDialog(false);
      onRefresh?.();
    } catch (error) {
      setMessage({
        type: 'error',
        text: `Failed to finalize settlements: ${error instanceof Error ? error.message : 'Unknown error'}`,
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 2,
          p: 2,
          bgcolor: 'action.hover',
          borderRadius: 1,
          mb: 2,
        }}
      >
        {/* Selection Counter */}
        <Chip
          label={`${selectedCount} selected`}
          color="primary"
          variant="outlined"
          size="medium"
        />

        {/* Action Buttons */}
        <ButtonGroup variant="contained" size="small" sx={{ ml: 'auto' }}>
          <Button
            startIcon={<CheckIcon />}
            onClick={() => setShowApproveDialog(true)}
            disabled={loading || selectedCount === 0}
            sx={{
              backgroundColor: 'success.main',
              '&:hover': { backgroundColor: 'success.dark' },
            }}
          >
            Approve ({selectedCount})
          </Button>

          <Button
            startIcon={<DoneIcon />}
            onClick={() => setShowFinalizeDialog(true)}
            disabled={loading || selectedCount === 0}
            sx={{
              backgroundColor: 'primary.main',
              '&:hover': { backgroundColor: 'primary.dark' },
            }}
          >
            Finalize ({selectedCount})
          </Button>

          <Button
            startIcon={<ExportIcon />}
            onClick={() => setShowExportDialog(true)}
            disabled={loading || selectedCount === 0}
            sx={{
              backgroundColor: 'info.main',
              '&:hover': { backgroundColor: 'info.dark' },
            }}
          >
            Export
          </Button>

          <Button
            startIcon={<ClearIcon />}
            onClick={onClearSelection}
            disabled={loading}
            sx={{
              backgroundColor: 'grey.500',
              '&:hover': { backgroundColor: 'grey.600' },
            }}
          >
            Clear
          </Button>
        </ButtonGroup>
      </Box>

      {/* Status Messages */}
      {message && (
        <Alert
          severity={message.type}
          onClose={() => setMessage(null)}
          sx={{ mb: 2 }}
        >
          {message.text}
        </Alert>
      )}

      {/* Approve Dialog */}
      <Dialog open={showApproveDialog} onClose={() => setShowApproveDialog(false)}>
        <DialogTitle>Approve Settlements</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ minWidth: 400, pt: 2 }}>
            <Alert severity="info">
              You are about to approve <strong>{selectedCount}</strong> settlement(s).
              This action cannot be undone.
            </Alert>
            {loading && <CircularProgress />}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowApproveDialog(false)} disabled={loading}>
            Cancel
          </Button>
          <Button
            onClick={handleApprove}
            variant="contained"
            color="success"
            disabled={loading}
          >
            Approve
          </Button>
        </DialogActions>
      </Dialog>

      {/* Finalize Dialog */}
      <Dialog open={showFinalizeDialog} onClose={() => setShowFinalizeDialog(false)}>
        <DialogTitle>Finalize Settlements</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ minWidth: 400, pt: 2 }}>
            <Alert severity="warning">
              You are about to finalize <strong>{selectedCount}</strong> settlement(s).
              Once finalized, settlements cannot be modified.
            </Alert>
            {loading && <CircularProgress />}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowFinalizeDialog(false)} disabled={loading}>
            Cancel
          </Button>
          <Button
            onClick={handleFinalize}
            variant="contained"
            color="primary"
            disabled={loading}
          >
            Finalize
          </Button>
        </DialogActions>
      </Dialog>

      {/* Export Dialog */}
      {showExportDialog && (
        <BulkExportDialog
          selectedCount={selectedCount}
          onClose={() => setShowExportDialog(false)}
          onExport={async (format: ExportFormat) => {
            try {
              if (!selectedIds || selectedIds.size === 0) {
                throw new Error('No settlements selected');
              }
              const ids = Array.from(selectedIds);
              const blob = await bulkSettlementApi.bulkExport(ids, format);
              const timestamp = new Date().toISOString().split('T')[0];
              const filename = `settlements-export-${timestamp}.${format === 'excel' ? 'xlsx' : format}`;
              bulkSettlementApi.downloadExport(blob, filename);
              setMessage({ type: 'success', text: `Export completed successfully` });
              setShowExportDialog(false);
            } catch (error) {
              setMessage({
                type: 'error',
                text: `Export failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
              });
            }
          }}
        />
      )}
    </>
  );
};

export default BulkActionsToolbar;
