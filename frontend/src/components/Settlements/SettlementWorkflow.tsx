import React from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Stepper,
  Step,
  StepLabel,
  Alert,
  CircularProgress,
  Stack,
  Chip,
  Divider,
  Typography,
} from '@mui/material';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import HourglassBottomIcon from '@mui/icons-material/HourglassBottom';
import settlementApi, { Settlement } from '../../services/settlementsApi';

export interface SettlementWorkflowProps {
  settlement: Settlement;
  contractType: 'purchase' | 'sales';
  onStatusChange?: (settlement: Settlement) => void;
  onError?: (error: Error) => void;
}

/**
 * Settlement Workflow Component
 * Displays the settlement lifecycle and manages state transitions
 * States: Draft → Calculated → Approved → Finalized
 */
export const SettlementWorkflow: React.FC<SettlementWorkflowProps> = ({
  settlement,
  contractType,
  onStatusChange,
  onError,
}) => {
  const queryClient = useQueryClient();

  const statusSteps = ['Draft', 'Calculated', 'Approved', 'Finalized'];
  const currentStep = statusSteps.indexOf(settlement.status);

  // Approve mutation
  const approveMutation = useMutation({
    mutationFn: async () => {
      if (contractType === 'purchase') {
        return settlementApi.approvePurchaseSettlement(settlement.id);
      } else {
        return settlementApi.approveSalesSettlement(settlement.id);
      }
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({
        queryKey: ['settlement', settlement.id, contractType],
      });
      onStatusChange?.(data);
    },
    onError: (error) => {
      onError?.(error instanceof Error ? error : new Error('Failed to approve settlement'));
    },
  });

  // Finalize mutation
  const finalizeMutation = useMutation({
    mutationFn: async () => {
      if (contractType === 'purchase') {
        return settlementApi.finalizePurchaseSettlement(settlement.id);
      } else {
        return settlementApi.finalizeSalesSettlement(settlement.id);
      }
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({
        queryKey: ['settlement', settlement.id, contractType],
      });
      onStatusChange?.(data);
    },
    onError: (error) => {
      onError?.(error instanceof Error ? error : new Error('Failed to finalize settlement'));
    },
  });

  const canApprove = settlement.status === 'Calculated';
  const canFinalize = settlement.status === 'Approved';

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Draft':
        return 'default';
      case 'Calculated':
        return 'info';
      case 'Approved':
        return 'warning';
      case 'Finalized':
        return 'success';
      default:
        return 'default';
    }
  };

  return (
    <Card>
      <CardHeader
        title="Settlement Workflow"
        subheader={`Settlement: ${settlement.settlementNumber}`}
        action={
          <Chip
            label={settlement.status}
            color={getStatusColor(settlement.status) as any}
            icon={settlement.status === 'Finalized' ? <CheckCircleIcon /> : <HourglassBottomIcon />}
          />
        }
      />
      <CardContent>
        <Stack spacing={3}>
          {approveMutation.isError && (
            <Alert severity="error">
              {approveMutation.error instanceof Error
                ? approveMutation.error.message
                : 'Failed to approve settlement'}
            </Alert>
          )}

          {finalizeMutation.isError && (
            <Alert severity="error">
              {finalizeMutation.error instanceof Error
                ? finalizeMutation.error.message
                : 'Failed to finalize settlement'}
            </Alert>
          )}

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 2, fontWeight: 'bold' }}>
              Lifecycle Progress
            </Typography>
            <Stepper activeStep={currentStep} sx={{ pt: 0 }}>
              {statusSteps.map((step) => (
                <Step key={step}>
                  <StepLabel>{step}</StepLabel>
                </Step>
              ))}
            </Stepper>
          </Box>

          <Divider />

          <Stack spacing={2}>
            <Box>
              <Typography variant="caption" display="block" sx={{ mb: 1 }}>
                Settlement Details
              </Typography>
              <Stack spacing={1} sx={{ bgcolor: '#f9f9f9', p: 2, borderRadius: 1 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem' }}>
                  <span>Number:</span>
                  <strong>{settlement.settlementNumber}</strong>
                </Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem' }}>
                  <span>Status:</span>
                  <strong>{settlement.status}</strong>
                </Box>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem' }}>
                  <span>Total Amount:</span>
                  <strong>
                    {settlement.currency} {settlement.totalAmount?.toFixed(2) || '0.00'}
                  </strong>
                </Box>
                {settlement.approvedBy && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem' }}>
                    <span>Approved By:</span>
                    <span>{settlement.approvedBy}</span>
                  </Box>
                )}
                {settlement.finalizedBy && (
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem' }}>
                    <span>Finalized By:</span>
                    <span>{settlement.finalizedBy}</span>
                  </Box>
                )}
              </Stack>
            </Box>
          </Stack>

          <Divider />

          {settlement.status !== 'Finalized' && (
            <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
              {canApprove && (
                <Button
                  variant="contained"
                  color="warning"
                  onClick={() => approveMutation.mutate()}
                  disabled={approveMutation.isPending}
                >
                  {approveMutation.isPending ? <CircularProgress size={24} /> : 'Approve Settlement'}
                </Button>
              )}

              {canFinalize && (
                <Button
                  variant="contained"
                  color="success"
                  onClick={() => finalizeMutation.mutate()}
                  disabled={finalizeMutation.isPending}
                >
                  {finalizeMutation.isPending ? <CircularProgress size={24} /> : 'Finalize Settlement'}
                </Button>
              )}

              {!canApprove && !canFinalize && (
                <Alert severity="info" sx={{ width: '100%' }}>
                  Settlement must be calculated before approval
                </Alert>
              )}
            </Box>
          )}

          {settlement.status === 'Finalized' && (
            <Alert severity="success" sx={{ width: '100%' }}>
              Settlement has been finalized and locked. No further modifications are allowed.
            </Alert>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

export default SettlementWorkflow;
