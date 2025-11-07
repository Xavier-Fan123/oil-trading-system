import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  TextField,
  CircularProgress,
  Alert,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Typography,
  Divider,
} from '@mui/material';
import { useMutation, useQuery } from '@tanstack/react-query';
import { settlementApi, CreateSettlementDto } from '../../services/settlementApi';
import { purchaseContractsApi } from '../../services/contractsApi';
import { salesContractsApi } from '../../services/salesContractsApi';
import { PaymentTerms, PaymentMethod, PaymentTermsLabels, PaymentMethodLabels } from '../../types/settlement';

/**
 * Calculate expected payment date based on payment terms
 */
function calculateExpectedPaymentDate(terms: PaymentTerms | string): Date {
  const today = new Date();
  const termsNum = typeof terms === 'string' ? parseInt(terms) : terms;

  switch (termsNum) {
    case PaymentTerms.Immediate:
      return today;
    case PaymentTerms.Net10:
      return new Date(today.setDate(today.getDate() + 10));
    case PaymentTerms.Net30:
      return new Date(today.setDate(today.getDate() + 30));
    case PaymentTerms.Net60:
      return new Date(today.setDate(today.getDate() + 60));
    case PaymentTerms.Net90:
      return new Date(today.setDate(today.getDate() + 90));
    default:
      return new Date(today.setDate(today.getDate() + 30));
  }
}

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
    paymentTerms: PaymentTerms.Net30.toString(),
    paymentMethod: PaymentMethod.BankTransfer.toString(),
    expectedPaymentDate: calculateExpectedPaymentDate(PaymentTerms.Net30).toISOString().split('T')[0],
  });

  // Fetch contract details to display
  const { data: contract, isLoading: contractLoading } = useQuery({
    queryKey: ['contract', contractId, contractType],
    queryFn: async () => {
      if (contractType === 'purchase') {
        return purchaseContractsApi.getById(contractId);
      } else {
        return salesContractsApi.getById(contractId);
      }
    },
  });

  // Create settlement mutation
  const createMutation = useMutation({
    mutationFn: async () => {
      const request: CreateSettlementDto = {
        contractId: contractId,
        externalContractNumber: formData.externalContractNumber || undefined,
        documentNumber: formData.documentNumber,
        documentType: parseInt(formData.documentType) || 1, // BillOfLading = 1
        documentDate: new Date(`${formData.documentDate}T00:00:00Z`),
        actualQuantityMT: 0, // Will be entered in next step
        actualQuantityBBL: 0, // Will be entered in next step
        createdBy: 'CurrentUser',
        settlementCurrency: 'USD',
        autoCalculatePrices: true,
        autoTransitionStatus: false
      };
      return settlementApi.createSettlement(request);
    },
    onSuccess: (data) => {
      if (data.settlementId) {
        onSuccess?.(data.settlementId);
      }
    },
    onError: (error) => {
      onError?.(error instanceof Error ? error : new Error('Failed to create settlement'));
    },
  });

  const handleInputChange = (field: keyof typeof formData) => (
    e: any
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

          {/* Payment Terms Section */}
          <Divider sx={{ my: 2 }} />
          <Typography variant="h6" sx={{ mb: 2 }}>
            Payment Terms
          </Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Payment Terms</InputLabel>
                <Select
                  value={formData.paymentTerms}
                  onChange={(e) => {
                    const newTerms = parseInt(e.target.value);
                    setFormData((prev) => ({
                      ...prev,
                      paymentTerms: e.target.value,
                      expectedPaymentDate: calculateExpectedPaymentDate(newTerms).toISOString().split('T')[0],
                    }));
                  }}
                  label="Payment Terms"
                >
                  {Object.entries(PaymentTermsLabels).map(([key, label]) => (
                    <MenuItem key={key} value={key}>
                      {label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Payment Method</InputLabel>
                <Select
                  value={formData.paymentMethod}
                  onChange={handleInputChange('paymentMethod')}
                  label="Payment Method"
                >
                  {Object.entries(PaymentMethodLabels).map(([key, label]) => (
                    <MenuItem key={key} value={key}>
                      {label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>

            <Grid item xs={12}>
              <TextField
                label="Expected Payment Date"
                type="date"
                value={formData.expectedPaymentDate}
                onChange={handleInputChange('expectedPaymentDate')}
                InputLabelProps={{ shrink: true }}
                fullWidth
              />
            </Grid>
          </Grid>

          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'flex-end', mt: 3 }}>
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
