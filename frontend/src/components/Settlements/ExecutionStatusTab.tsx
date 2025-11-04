import React from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Alert,
  Grid,
  Paper,
  Typography,
  Chip,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Stack,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { settlementApi, ContractSettlementDto } from '@/services/settlementApi';
import { getSettlementStatusColor, ContractSettlementStatusLabels, ContractSettlementStatus } from '@/types/settlement';

interface ExecutionStatusTabProps {
  settlementId: string;
}

/**
 * Execution Status Tab Component
 * Displays settlement workflow status, quantities, and calculations
 */
export const ExecutionStatusTab: React.FC<ExecutionStatusTabProps> = ({ settlementId }) => {
  const {
    data: settlement,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['settlement', settlementId],
    queryFn: () => settlementApi.getById(settlementId),
    enabled: !!settlementId,
  });

  const getStatusPercentage = (status: string | number): number => {
    const statusNum = typeof status === 'string' ? parseInt(status) : status;
    // Draft(1) → DataEntered(2) → Calculated(3) → Reviewed(4) → Approved(5) → Finalized(6)
    const percentage = ((statusNum - 1) / 5) * 100;
    return Math.min(percentage, 100);
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load settlement status: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  if (!settlement) {
    return <Alert severity="warning">Settlement not found</Alert>;
  }

  const workflowSteps = [
    { step: 1, label: 'Draft', color: 'default' },
    { step: 2, label: 'Data Entered', color: 'info' },
    { step: 3, label: 'Calculated', color: 'primary' },
    { step: 4, label: 'Reviewed', color: 'secondary' },
    { step: 5, label: 'Approved', color: 'warning' },
    { step: 6, label: 'Finalized', color: 'success' },
  ] as const;

  const statusNum = typeof settlement.status === 'string' ? parseInt(settlement.status) : settlement.status;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {/* Workflow Progress */}
      <Card>
        <CardHeader title="Settlement Workflow Status" />
        <CardContent>
          <Box sx={{ mb: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
              <Typography variant="body2" sx={{ fontWeight: 500 }}>
                Progress: {ContractSettlementStatusLabels[statusNum as ContractSettlementStatus] || 'Unknown'} (Step {statusNum}/6)
              </Typography>
              <Chip
                label={ContractSettlementStatusLabels[statusNum as ContractSettlementStatus]}
                color={getSettlementStatusColor(settlement.status)}
                variant="filled"
              />
            </Box>
            <LinearProgress
              variant="determinate"
              value={getStatusPercentage(statusNum)}
              sx={{ height: 8, borderRadius: 1, mb: 2 }}
            />
          </Box>

          {/* Workflow Steps */}
          <Grid container spacing={1}>
            {workflowSteps.map((item) => (
              <Grid item xs={12} sm={6} md={4} key={item.step}>
                <Paper
                  sx={{
                    p: 2,
                    backgroundColor: statusNum >= item.step ? 'rgba(76, 175, 80, 0.1)' : '#f5f5f5',
                    border: `2px solid ${statusNum === item.step ? '#4caf50' : '#e0e0e0'}`,
                    position: 'relative',
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                    <Box
                      sx={{
                        width: 32,
                        height: 32,
                        borderRadius: '50%',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        backgroundColor: statusNum >= item.step ? '#4caf50' : '#e0e0e0',
                        color: 'white',
                        fontWeight: 600,
                      }}
                    >
                      {statusNum > item.step ? '✓' : item.step}
                    </Box>
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {item.label}
                      </Typography>
                      {statusNum === item.step && (
                        <Typography variant="caption" sx={{ color: '#4caf50', fontWeight: 600 }}>
                          Current
                        </Typography>
                      )}
                    </Box>
                  </Box>
                </Paper>
              </Grid>
            ))}
          </Grid>
        </CardContent>
      </Card>

      {/* Quantity Information */}
      <Card>
        <CardHeader title="Quantity Information" />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#f5f5f5' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Actual Quantity (MT)
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600 }}>
                  {settlement.actualQuantityMT?.toLocaleString() || '0'} MT
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#f5f5f5' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Actual Quantity (BBL)
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600 }}>
                  {settlement.actualQuantityBBL?.toLocaleString() || '0'} BBL
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#e3f2fd' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Calculation Quantity (MT)
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#1976d2' }}>
                  {settlement.calculationQuantityMT?.toLocaleString() || '0'} MT
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#e3f2fd' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Calculation Quantity (BBL)
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#1976d2' }}>
                  {settlement.calculationQuantityBBL?.toLocaleString() || '0'} BBL
                </Typography>
              </Paper>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Settlement Amounts */}
      <Card>
        <CardHeader title="Settlement Amounts" />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6} md={3}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#f5f5f5' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Benchmark Amount
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600 }}>
                  {settlement.settlementCurrency} {settlement.benchmarkAmount?.toLocaleString() || '0'}
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#f5f5f5' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Adjustment Amount
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600 }}>
                  {settlement.settlementCurrency} {settlement.adjustmentAmount?.toLocaleString() || '0'}
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#fff3e0' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Cargo Value
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#ff9800' }}>
                  {settlement.settlementCurrency} {settlement.cargoValue?.toLocaleString() || '0'}
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12} sm={6} md={3}>
              <Paper elevation={0} sx={{ p: 2, backgroundColor: '#fff3e0' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                  Total Charges
                </Typography>
                <Typography variant="h6" sx={{ fontWeight: 600, color: '#ff9800' }}>
                  {settlement.settlementCurrency} {settlement.totalCharges?.toLocaleString() || '0'}
                </Typography>
              </Paper>
            </Grid>

            <Grid item xs={12}>
              <Paper elevation={2} sx={{ p: 3, backgroundColor: '#e8f5e9', borderLeft: '4px solid #4caf50' }}>
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 1 }}>
                  Total Settlement Amount
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 700, color: '#4caf50' }}>
                  {settlement.settlementCurrency} {settlement.totalSettlementAmount?.toLocaleString() || '0'}
                </Typography>
              </Paper>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {/* Key Dates */}
      <Card>
        <CardHeader title="Key Dates" />
        <CardContent>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <Stack spacing={2}>
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Created
                  </Typography>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {settlement.createdDate ? format(new Date(settlement.createdDate), 'MMM dd, yyyy HH:mm') : '-'}
                  </Typography>
                  <Typography variant="caption" color="textSecondary">
                    by {settlement.createdBy}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Last Modified
                  </Typography>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {settlement.lastModifiedDate ? format(new Date(settlement.lastModifiedDate), 'MMM dd, yyyy HH:mm') : 'N/A'}
                  </Typography>
                  {settlement.lastModifiedBy && (
                    <Typography variant="caption" color="textSecondary">
                      by {settlement.lastModifiedBy}
                    </Typography>
                  )}
                </Box>
              </Stack>
            </Grid>

            <Grid item xs={12} sm={6}>
              {settlement.finalizedDate && (
                <Box>
                  <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                    Finalized
                  </Typography>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {format(new Date(settlement.finalizedDate), 'MMM dd, yyyy HH:mm')}
                  </Typography>
                  {settlement.finalizedBy && (
                    <Typography variant="caption" color="textSecondary">
                      by {settlement.finalizedBy}
                    </Typography>
                  )}
                </Box>
              )}
              {!settlement.finalizedDate && (
                <Alert severity="info">Settlement not yet finalized</Alert>
              )}
            </Grid>
          </Grid>
        </CardContent>
      </Card>
    </Box>
  );
};

export default ExecutionStatusTab;
