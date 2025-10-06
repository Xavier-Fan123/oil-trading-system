import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  TextField,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  CircularProgress,
  Stepper,
  Step,
  StepLabel,
  StepContent
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Save as SaveIcon,
  Add as AddIcon,
  NavigateNext as NextIcon,
  NavigateBefore as BackIcon
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  CreateSettlementDto,
  DocumentType,
  DocumentTypeLabels,
  QuantityUnit,
  SettlementFormData,
  ChargeFormData,
  ChargeType,
  ChargeTypeLabels
} from '@/types/settlement';
import { settlementApi, getSettlementWithFallback } from '@/services/settlementApi';
import { purchaseContractsApi } from '@/services/contractsApi';
import { salesContractsApi } from '@/services/salesContractsApi';
import { QuantityCalculator } from './QuantityCalculator';

interface SettlementEntryProps {
  mode: 'create' | 'edit';
  settlementId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

interface ContractInfo {
  id: string;
  contractNumber: string;
  externalContractNumber?: string;
  type: 'purchase' | 'sales';
  supplierName?: string;
  customerName?: string;
  productName: string;
  quantity: number;
  quantityUnit: QuantityUnit;
  tonBarrelRatio: number;
}

const steps = [
  'Contract Selection',
  'Document Information',
  'Quantity Calculation',
  'Initial Charges',
  'Review & Submit'
];

export const SettlementEntry: React.FC<SettlementEntryProps> = ({
  mode,
  settlementId,
  onSuccess,
  onCancel
}) => {
  const [activeStep, setActiveStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [contracts, setContracts] = useState<ContractInfo[]>([]);
  const [selectedContract, setSelectedContract] = useState<ContractInfo | null>(null);
  
  const [formData, setFormData] = useState<SettlementFormData>({
    contractId: '',
    externalContractNumber: '',
    documentNumber: '',
    documentType: DocumentType.BillOfLading,
    documentDate: new Date(),
    actualQuantityMT: 0,
    actualQuantityBBL: 0,
    notes: '',
    charges: []
  });

  // Load existing settlement for edit mode
  useEffect(() => {
    if (mode === 'edit' && settlementId) {
      loadExistingSettlement();
    } else {
      loadContracts();
    }
  }, [mode, settlementId]);

  const loadExistingSettlement = async () => {
    if (!settlementId) return;

    setLoading(true);
    try {
      const settlement = await getSettlementWithFallback(settlementId);
      if (settlement) {
        setFormData({
          contractId: settlement.contractId,
          externalContractNumber: settlement.externalContractNumber,
          documentNumber: settlement.documentNumber || '',
          documentType: Object.keys(DocumentTypeLabels).find(key => 
            DocumentTypeLabels[key as unknown as DocumentType] === settlement.documentType
          ) as unknown as DocumentType || DocumentType.BillOfLading,
          documentDate: new Date(settlement.documentDate),
          actualQuantityMT: settlement.actualQuantityMT,
          actualQuantityBBL: settlement.actualQuantityBBL,
          notes: settlement.quantityCalculationNote || '',
          charges: settlement.charges.map(charge => ({
            id: charge.id,
            chargeType: Object.keys(ChargeTypeLabels).find(key => 
              ChargeTypeLabels[key as unknown as ChargeType] === charge.chargeTypeDisplayName
            ) as unknown as ChargeType || ChargeType.Other,
            description: charge.description,
            amount: charge.amount,
            currency: charge.currency,
            incurredDate: charge.incurredDate || new Date(),
            referenceDocument: charge.referenceDocument || '',
            notes: charge.notes || ''
          }))
        });

        // Load contract info
        if (settlement.purchaseContract) {
          setSelectedContract({
            id: settlement.contractId,
            contractNumber: settlement.contractNumber,
            externalContractNumber: settlement.externalContractNumber,
            type: 'purchase',
            supplierName: settlement.purchaseContract.supplierName,
            productName: settlement.purchaseContract.productName,
            quantity: settlement.purchaseContract.quantity,
            quantityUnit: settlement.purchaseContract.quantityUnit,
            tonBarrelRatio: 7.33 // Default, would come from contract
          });
        } else if (settlement.salesContract) {
          setSelectedContract({
            id: settlement.contractId,
            contractNumber: settlement.contractNumber,
            externalContractNumber: settlement.externalContractNumber || '',
            type: 'sales',
            customerName: settlement.salesContract.customerName,
            productName: settlement.salesContract.productName,
            quantity: settlement.salesContract.quantity,
            quantityUnit: settlement.salesContract.quantityUnit,
            tonBarrelRatio: 7.33 // Default, would come from contract
          });
        }
      }
    } catch (err) {
      setError('Failed to load settlement for editing');
    } finally {
      setLoading(false);
    }
  };

  const loadContracts = async () => {
    setLoading(true);
    try {
      // Load both purchase and sales contracts
      const [purchaseResult, salesResult] = await Promise.all([
        purchaseContractsApi.getAll({ pageNumber: 1, pageSize: 100 }),
        salesContractsApi.getAll({ pageNumber: 1, pageSize: 100 })
      ]);

      const contractList: ContractInfo[] = [
        ...purchaseResult.items.map(contract => ({
          id: contract.id,
          contractNumber: contract.contractNumber,
          externalContractNumber: contract.externalContractNumber,
          type: 'purchase' as const,
          supplierName: contract.supplierName,
          productName: contract.productName,
          quantity: contract.quantity,
          quantityUnit: contract.quantityUnit,
          tonBarrelRatio: (contract as any).tonBarrelRatio || 7.33
        })),
        ...salesResult.items.map(contract => ({
          id: contract.id,
          contractNumber: (contract.contractNumber as any).value || (contract as any).contractNumber,
          externalContractNumber: undefined,
          type: 'sales' as const,
          customerName: (contract as any).customer?.name || (contract as any).customerName,
          productName: (contract as any).product?.name || (contract as any).productName,
          quantity: contract.quantity,
          quantityUnit: (contract as any).quantityUnit || 0,
          tonBarrelRatio: (contract as any).tonBarrelRatio || 7.33
        }))
      ];

      setContracts(contractList);
    } catch (err) {
      console.error('Error loading contracts:', err);
      // Use mock data on error
      setContracts([
        {
          id: 'mock-purchase-1',
          contractNumber: 'PC-2024-001',
          externalContractNumber: 'EXT-001',
          type: 'purchase',
          supplierName: 'Global Oil Supply Co.',
          productName: 'Brent Crude Oil',
          quantity: 25000,
          quantityUnit: QuantityUnit.MT,
          tonBarrelRatio: 7.33
        },
        {
          id: 'mock-purchase-2',
          contractNumber: 'PC-2024-002',
          externalContractNumber: 'EXT-002',
          type: 'purchase',
          supplierName: 'Arabian Oil Trading',
          productName: 'WTI Crude Oil',
          quantity: 30000,
          quantityUnit: QuantityUnit.MT,
          tonBarrelRatio: 7.45
        }
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleNext = () => {
    if (validateStep(activeStep)) {
      setActiveStep((prev) => prev + 1);
    }
  };

  const handleBack = () => {
    setActiveStep((prev) => prev - 1);
  };

  const validateStep = (step: number): boolean => {
    setError(null);

    switch (step) {
      case 0: // Contract Selection
        if (!selectedContract) {
          setError('Please select a contract');
          return false;
        }
        return true;

      case 1: // Document Information
        if (!formData.documentNumber || !formData.documentNumber.trim()) {
          setError('Document number is required');
          return false;
        }
        if (!formData.documentDate) {
          setError('Document date is required');
          return false;
        }
        return true;

      case 2: // Quantity Calculation
        if (formData.actualQuantityMT <= 0 || formData.actualQuantityBBL <= 0) {
          setError('Both MT and BBL quantities must be greater than zero');
          return false;
        }
        return true;

      case 3: // Initial Charges
        // Charges are optional, always valid
        return true;

      default:
        return true;
    }
  };

  const handleSubmit = async () => {
    if (!selectedContract) {
      setError('No contract selected');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const dto: CreateSettlementDto = {
        contractId: selectedContract.id,
        documentNumber: formData.documentNumber?.trim(),
        documentType: formData.documentType,
        documentDate: formData.documentDate,
        actualQuantityMT: formData.actualQuantityMT,
        actualQuantityBBL: formData.actualQuantityBBL,
        createdBy: 'CurrentUser', // This would come from auth context
        notes: formData.notes?.trim(),
        settlementCurrency: 'USD',
        autoCalculatePrices: true,
        autoTransitionStatus: false
      };

      if (mode === 'create') {
        const result = await settlementApi.createSettlement(dto);
        if (result.isSuccessful && result.settlementId) {
          // Add initial charges if any
          if (formData.charges.length > 0) {
            // Would add charges here using settlementChargeApi
          }
          onSuccess();
        } else {
          setError(result.errorMessage || 'Failed to create settlement');
        }
      } else if (mode === 'edit' && settlementId) {
        await settlementApi.updateSettlement(settlementId, {
          documentNumber: dto.documentNumber,
          documentType: dto.documentType,
          documentDate: dto.documentDate,
          actualQuantityMT: dto.actualQuantityMT,
          actualQuantityBBL: dto.actualQuantityBBL,
          notes: dto.notes
        });
        onSuccess();
      }
    } catch (err) {
      console.error('Error saving settlement:', err);
      setError('Failed to save settlement. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleContractSelect = (contractId: string) => {
    const contract = contracts.find(c => c.id === contractId);
    if (contract) {
      setSelectedContract(contract);
      setFormData(prev => ({
        ...prev,
        contractId: contract.id,
        externalContractNumber: contract.externalContractNumber || ''
      }));
    }
  };

  const addCharge = () => {
    const newCharge: ChargeFormData = {
      chargeType: ChargeType.Other,
      description: '',
      amount: 0,
      currency: 'USD',
      incurredDate: new Date(),
      referenceDocument: '',
      notes: ''
    };
    setFormData(prev => ({
      ...prev,
      charges: [...prev.charges, newCharge]
    }));
  };

  const updateCharge = (index: number, charge: ChargeFormData) => {
    setFormData(prev => ({
      ...prev,
      charges: prev.charges.map((c, i) => i === index ? charge : c)
    }));
  };

  const removeCharge = (index: number) => {
    setFormData(prev => ({
      ...prev,
      charges: prev.charges.filter((_, i) => i !== index)
    }));
  };

  const renderStepContent = (step: number) => {
    switch (step) {
      case 0: // Contract Selection
        return (
          <Box>
            <Typography paragraph>
              Select the contract for which you want to create a settlement.
            </Typography>
            <FormControl fullWidth required>
              <InputLabel>Select Contract</InputLabel>
              <Select
                value={selectedContract?.id || ''}
                label="Select Contract"
                onChange={(e) => handleContractSelect(e.target.value)}
                disabled={loading || mode === 'edit'}
              >
                {contracts.map((contract) => (
                  <MenuItem key={contract.id} value={contract.id}>
                    <Box>
                      <Typography variant="body1">
                        {contract.contractNumber} 
                        {contract.externalContractNumber && ` (${contract.externalContractNumber})`}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {contract.type === 'purchase' ? contract.supplierName : contract.customerName} • 
                        {contract.productName} • 
                        {contract.quantity.toLocaleString()} {contract.quantityUnit === QuantityUnit.MT ? 'MT' : 'BBL'}
                      </Typography>
                    </Box>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            
            {selectedContract && (
              <Alert severity="info" sx={{ mt: 2 }}>
                Selected: <strong>{selectedContract.contractNumber}</strong> • 
                {selectedContract.type === 'purchase' ? selectedContract.supplierName : selectedContract.customerName} • 
                {selectedContract.productName}
              </Alert>
            )}
          </Box>
        );

      case 1: // Document Information
        return (
          <Box>
            <Typography paragraph>
              Enter the Bill of Lading or Certificate of Quantity information.
            </Typography>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  required
                  label="Document Number"
                  placeholder="e.g., BL-2024-001"
                  value={formData.documentNumber}
                  onChange={(e) => setFormData(prev => ({ ...prev, documentNumber: e.target.value }))}
                  disabled={loading}
                />
              </Grid>
              <Grid item xs={12} md={6}>
                <FormControl fullWidth required>
                  <InputLabel>Document Type</InputLabel>
                  <Select
                    value={formData.documentType}
                    label="Document Type"
                    onChange={(e) => setFormData(prev => ({ ...prev, documentType: e.target.value as DocumentType }))}
                    disabled={loading}
                  >
                    {Object.entries(DocumentTypeLabels).map(([value, label]) => (
                      <MenuItem key={value} value={value}>
                        {label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} md={6}>
                <DatePicker
                  label="Document Date"
                  value={formData.documentDate}
                  onChange={(date) => setFormData(prev => ({ ...prev, documentDate: date || new Date() }))}
                  disabled={loading}
                  slotProps={{ textField: { fullWidth: true, required: true } }}
                />
              </Grid>
            </Grid>
          </Box>
        );

      case 2: // Quantity Calculation
        return (
          <Box>
            <Typography paragraph>
              Enter the actual quantities from the document and configure calculation settings.
            </Typography>
            <QuantityCalculator
              initialData={{
                actualQuantityMT: formData.actualQuantityMT,
                actualQuantityBBL: formData.actualQuantityBBL
              }}
              contractQuantity={selectedContract?.quantity}
              contractUnit={selectedContract?.quantityUnit}
              productDensity={850} // This would come from product data
              onChange={(data) => {
                setFormData(prev => ({
                  ...prev,
                  actualQuantityMT: data.actualQuantityMT,
                  actualQuantityBBL: data.actualQuantityBBL,
                  notes: data.calculationNote
                }));
              }}
              readOnly={loading}
            />
          </Box>
        );

      case 3: // Initial Charges
        return (
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography>
                Add any initial charges (optional). You can add more charges later.
              </Typography>
              <Button
                variant="outlined"
                startIcon={<AddIcon />}
                onClick={addCharge}
                disabled={loading}
              >
                Add Charge
              </Button>
            </Box>

            {formData.charges.map((charge, index) => (
              <Card key={index} sx={{ mb: 2 }}>
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={4}>
                      <FormControl fullWidth required>
                        <InputLabel>Charge Type</InputLabel>
                        <Select
                          value={charge.chargeType}
                          label="Charge Type"
                          onChange={(e) => updateCharge(index, { ...charge, chargeType: e.target.value as ChargeType })}
                          disabled={loading}
                        >
                          {Object.entries(ChargeTypeLabels).map(([value, label]) => (
                            <MenuItem key={value} value={value}>
                              {label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <TextField
                        fullWidth
                        required
                        label="Amount"
                        type="number"
                        value={charge.amount}
                        onChange={(e) => updateCharge(index, { ...charge, amount: parseFloat(e.target.value) || 0 })}
                        disabled={loading}
                      />
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <TextField
                        fullWidth
                        required
                        label="Description"
                        value={charge.description}
                        onChange={(e) => updateCharge(index, { ...charge, description: e.target.value })}
                        disabled={loading}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <Button
                        variant="text"
                        color="error"
                        onClick={() => removeCharge(index)}
                        disabled={loading}
                      >
                        Remove Charge
                      </Button>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            ))}
          </Box>
        );

      case 4: // Review & Submit
        return (
          <Box>
            <Typography variant="h6" gutterBottom>Review Settlement Details</Typography>
            
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" gutterBottom>Contract Information</Typography>
                <Typography variant="body2">Contract: {selectedContract?.contractNumber}</Typography>
                <Typography variant="body2">
                  {selectedContract?.type === 'purchase' ? 'Supplier' : 'Customer'}: {' '}
                  {selectedContract?.type === 'purchase' ? selectedContract?.supplierName : selectedContract?.customerName}
                </Typography>
                <Typography variant="body2">Product: {selectedContract?.productName}</Typography>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" gutterBottom>Document Information</Typography>
                <Typography variant="body2">Document: {formData.documentNumber}</Typography>
                <Typography variant="body2">Type: {DocumentTypeLabels[formData.documentType]}</Typography>
                <Typography variant="body2">Date: {formData.documentDate.toDateString()}</Typography>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" gutterBottom>Quantities</Typography>
                <Typography variant="body2">MT: {formData.actualQuantityMT.toLocaleString()}</Typography>
                <Typography variant="body2">BBL: {formData.actualQuantityBBL.toLocaleString()}</Typography>
              </Grid>
              
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" gutterBottom>Initial Charges</Typography>
                <Typography variant="body2">{formData.charges.length} charges added</Typography>
                {formData.charges.length > 0 && (
                  <Typography variant="body2">
                    Total: {formData.charges.reduce((sum, c) => sum + c.amount, 0).toLocaleString()} USD
                  </Typography>
                )}
              </Grid>
            </Grid>
          </Box>
        );

      default:
        return null;
    }
  };

  if (loading && mode === 'edit') {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Box>
        {/* Header */}
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            <Button
              startIcon={<ArrowBackIcon />}
              onClick={onCancel}
              sx={{ mr: 2 }}
            >
              Cancel
            </Button>
            <Typography variant="h4" component="h1">
              {mode === 'create' ? 'Create Settlement' : 'Edit Settlement'}
            </Typography>
          </Box>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        {/* Stepper */}
        <Card>
          <CardContent>
            <Stepper activeStep={activeStep} orientation="vertical">
              {steps.map((label, index) => (
                <Step key={label}>
                  <StepLabel>{label}</StepLabel>
                  <StepContent>
                    {renderStepContent(index)}
                    
                    <Box sx={{ mt: 2 }}>
                      <Button
                        disabled={loading}
                        onClick={index === steps.length - 1 ? handleSubmit : handleNext}
                        variant="contained"
                        startIcon={index === steps.length - 1 ? (loading ? <CircularProgress size={20} /> : <SaveIcon />) : <NextIcon />}
                      >
                        {loading ? 'Saving...' : (index === steps.length - 1 ? (mode === 'create' ? 'Create Settlement' : 'Update Settlement') : 'Next')}
                      </Button>
                      {index > 0 && (
                        <Button
                          disabled={loading}
                          onClick={handleBack}
                          sx={{ ml: 1 }}
                          startIcon={<BackIcon />}
                        >
                          Back
                        </Button>
                      )}
                    </Box>
                  </StepContent>
                </Step>
              ))}
            </Stepper>
          </CardContent>
        </Card>
      </Box>
    </LocalizationProvider>
  );
};