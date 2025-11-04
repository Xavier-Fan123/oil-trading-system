import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  TextField,
  Grid,
  Alert,
  CircularProgress,
  Stack,
  Typography,
  Divider,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { settlementApi } from '../../services/settlementApi';
import { ContractSettlementDto } from '../../types/settlement';

export interface SettlementCalculationFormProps {
  settlement: ContractSettlementDto;
  contractType: 'purchase' | 'sales';
  onSuccess?: (settlement: ContractSettlementDto) => void;
  onError?: (error: Error) => void;
}

/**
 * Settlement Calculation Form Component
 * Handles calculation of settlement amounts based on quantities and prices
 */
export const SettlementCalculationForm: React.FC<SettlementCalculationFormProps> = ({
  settlement,
  contractType,
  onSuccess,
  onError,
}) => {
  const [formData, setFormData] = useState({
    calculationQuantityMT: settlement.calculationQuantityMT || 0,
    calculationQuantityBBL: settlement.calculationQuantityBBL || 0,
    benchmarkAmount: settlement.benchmarkAmount || 0,
    adjustmentAmount: settlement.adjustmentAmount || 0,
    calculationNote: settlement.quantityCalculationNote || '',
  });

  const [calculatedTotal, setCalculatedTotal] = useState<number | null>(null);

  // Calculate total whenever quantities or prices change
  React.useEffect(() => {
    if (formData.benchmarkAmount > 0 && formData.calculationQuantityMT > 0) {
      const baseTotal = formData.calculationQuantityMT * formData.benchmarkAmount;
      const adjustmentTotal = formData.calculationQuantityBBL * (formData.adjustmentAmount || 0);
      setCalculatedTotal(baseTotal + adjustmentTotal);
    }
  }, [
    formData.benchmarkAmount,
    formData.calculationQuantityMT,
    formData.adjustmentAmount,
    formData.calculationQuantityBBL,
  ]);

  // Calculate mutation
  const calculateMutation = useMutation({
    mutationFn: async () => {
      if (contractType === 'purchase') {
        return settlementApi.calculatePurchaseSettlement(settlement.id, formData);
      } else {
        return settlementApi.calculateSalesSettlement(settlement.id, formData);
      }
    },
    onSuccess: (data) => {
      onSuccess?.(data);
    },
    onError: (error) => {
      onError?.(error instanceof Error ? error : new Error('Failed to calculate settlement'));
    },
  });

  const handleInputChange = (field: keyof typeof formData) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value =
      field === 'calculationNote'
        ? e.target.value
        : parseFloat(e.target.value) || 0;

    setFormData((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  // Disable if settlement is finalized
  if (settlement.status === 'Finalized') {
    return (
      <Alert severity="warning">
        This settlement has been finalized and cannot be modified.
      </Alert>
    );
  }

  return (
    <Card>
      <CardHeader
        title="Calculate Settlement"
        subheader={`Settlement: ${settlement.contractNumber}`}
      />
      <CardContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          {calculateMutation.isError && (
            <Alert severity="error">
              {calculateMutation.error instanceof Error
                ? calculateMutation.error.message
                : 'Failed to calculate settlement'}
            </Alert>
          )}

          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Quantity (MT)"
                type="number"
                value={formData.calculationQuantityMT}
                onChange={handleInputChange('calculationQuantityMT')}
                inputProps={{ step: '0.01' }}
                fullWidth
                required
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                label="Quantity (BBL)"
                type="number"
                value={formData.calculationQuantityBBL}
                onChange={handleInputChange('calculationQuantityBBL')}
                inputProps={{ step: '0.01' }}
                fullWidth
                required
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                label="Benchmark Amount (USD)"
                type="number"
                value={formData.benchmarkAmount}
                onChange={handleInputChange('benchmarkAmount')}
                inputProps={{ step: '0.01' }}
                fullWidth
                required
              />
            </Grid>

            <Grid item xs={12} sm={6}>
              <TextField
                label="Adjustment Amount (USD)"
                type="number"
                value={formData.adjustmentAmount}
                onChange={handleInputChange('adjustmentAmount')}
                inputProps={{ step: '0.01' }}
                fullWidth
              />
            </Grid>

            <Grid item xs={12}>
              <TextField
                label="Calculation Note"
                value={formData.calculationNote || ''}
                onChange={handleInputChange('calculationNote')}
                multiline
                rows={3}
                fullWidth
                placeholder="Enter any notes about this calculation..."
              />
            </Grid>
          </Grid>

          <Divider />

          {calculatedTotal !== null && (
            <Stack spacing={1} sx={{ bgcolor: '#f5f5f5', p: 2, borderRadius: 1 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography variant="body2">Benchmark Total:</Typography>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                  ${(formData.calculationQuantityMT * formData.benchmarkAmount).toFixed(2)}
                </Typography>
              </Box>

              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography variant="body2">Adjustment Total:</Typography>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                  ${(formData.calculationQuantityBBL * formData.adjustmentAmount).toFixed(2)}
                </Typography>
              </Box>

              <Divider />

              <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                  Total Settlement Amount:
                </Typography>
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                  ${calculatedTotal.toFixed(2)}
                </Typography>
              </Box>
            </Stack>
          )}

          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
            <Button
              variant="contained"
              color="primary"
              onClick={() => calculateMutation.mutate()}
              disabled={
                calculateMutation.isPending ||
                !formData.calculationQuantityMT ||
                !formData.benchmarkAmount
              }
            >
              {calculateMutation.isPending ? <CircularProgress size={24} /> : 'Calculate'}
            </Button>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
};

export default SettlementCalculationForm;
