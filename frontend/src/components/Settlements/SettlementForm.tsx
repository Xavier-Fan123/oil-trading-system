import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Grid,
  TextField,
  CircularProgress,
  Alert,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
} from '@mui/material';
import { useMutation, useQuery } from '@tanstack/react-query';
import settlementApi, {
  CreatePurchaseSettlementRequest,
  CreateSalesSettlementRequest,
} from '../../services/settlementsApi';
import contractsApi from '../../services/contractsApi';
import salesContractsApi from '../../services/salesContractsApi';

export interface SettlementFormProps {
  contractType: 'purchase' | 'sales';
  contractId: string;
  onSuccess?: (settlementId: string) => void;
  onError?: (error: Error) => void;
}

/**
 * Settlement Creation Form Component
 * Handles creation of both purchase and sales settlements
 */
export const SettlementForm: React.FC<SettlementFormProps> = ({
  contractType,
  contractId,
  onSuccess,
  onError,
}) => {
  const [formData, setFormData] = useState({
    documentNumber: '',
    documentType: 'Invoice',
    documentDate: new Date().toISOString().split('T')[0],
    externalContractNumber: '',
  });

  // Fetch contract details to display
  const { data: contract, isLoading: contractLoading } = useQuery({
    queryKey: ['contract', contractId, contractType],
    queryFn: async () => {
      if (contractType === 'purchase') {
        return contractsApi.getPurchaseContract(contractId);
      } else {
        return salesContractsApi.getSalesContract(contractId);
      }
    },
  });

  // Create settlement mutation
  const createMutation = useMutation({
    mutationFn: async () => {
      if (contractType === 'purchase') {
        const request: CreatePurchaseSettlementRequest = {
          purchaseContractId: contractId,
          documentNumber: formData.documentNumber,
          documentType: formData.documentType,
          documentDate: `${formData.documentDate}T00:00:00Z`,
          externalContractNumber: formData.externalContractNumber || undefined,
        };
        return settlementApi.createPurchaseSettlement(request);
      } else {
        const request: CreateSalesSettlementRequest = {
          salesContractId: contractId,
          documentNumber: formData.documentNumber,
          documentType: formData.documentType,
          documentDate: `${formData.documentDate}T00:00:00Z`,
          externalContractNumber: formData.externalContractNumber || undefined,
        };
        return settlementApi.createSalesSettlement(request);
      }
    },
    onSuccess: (data) => {
      onSuccess?.(data.id);
    },
    onError: (error) => {
      onError?.(error instanceof Error ? error : new Error('Failed to create settlement'));
    },
  });

  const handleInputChange = (field: keyof typeof formData) => (
    e: React.ChangeEvent<HTMLInputElement | { name?: string; value: unknown }>
  ) => {
    setFormData((prev) => ({
      ...prev,
      [field]: e.target.value,
    }));
  };

  if (contractLoading) {
    return <CircularProgress />;
  }

  return (
    <Card>
      <CardHeader
        title={`Create ${contractType === 'purchase' ? 'Purchase' : 'Sales'} Settlement`}
        subheader={`Contract: ${contract?.contractNumber || 'N/A'}`}
      />
      <CardContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {createMutation.isError && (
            <Alert severity="error">
              {createMutation.error instanceof Error
                ? createMutation.error.message
                : 'Failed to create settlement'}
            </Alert>
          )}

          <TextField
            label="Document Number"
            value={formData.documentNumber}
            onChange={handleInputChange('documentNumber')}
            required
            fullWidth
            placeholder="e.g., INV-2025-001"
          />

          <FormControl fullWidth>
            <InputLabel>Document Type</InputLabel>
            <Select
              value={formData.documentType}
              onChange={handleInputChange('documentType')}
              label="Document Type"
            >
              <MenuItem value="Invoice">Invoice</MenuItem>
              <MenuItem value="BillOfLading">Bill of Lading</MenuItem>
              <MenuItem value="CertificateOfQuantity">Certificate of Quantity</MenuItem>
              <MenuItem value="Specification">Specification</MenuItem>
              <MenuItem value="Other">Other</MenuItem>
            </Select>
          </FormControl>

          <TextField
            label="Document Date"
            type="date"
            value={formData.documentDate}
            onChange={handleInputChange('documentDate')}
            InputLabelProps={{ shrink: true }}
            fullWidth
            required
          />

          <TextField
            label="External Contract Number (Optional)"
            value={formData.externalContractNumber}
            onChange={handleInputChange('externalContractNumber')}
            fullWidth
            placeholder="e.g., EXT-2025-001"
          />

          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
            <Button
              variant="contained"
              color="primary"
              onClick={() => createMutation.mutate()}
              disabled={createMutation.isPending || !formData.documentNumber}
            >
              {createMutation.isPending ? <CircularProgress size={24} /> : 'Create Settlement'}
            </Button>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
};

export default SettlementForm;
