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
  StepContent,
  Tabs,
  Tab,
  InputAdornment
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
import { ContractResolver } from '../Contracts/ContractResolver';
import { SettlementCalculationForm } from './SettlementCalculationForm';

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
  quantityUnit: QuantityUnit | string;  // Accept both enum and string from API
  tonBarrelRatio: number;
}

const steps = [
  'Contract & Document Setup',
  'Quantities & Pricing',
  'Payment & Charges',
  'Review & Finalize'
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
  const [contractSelectionTab, setContractSelectionTab] = useState<'dropdown' | 'external'>(0 as any);
  
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

  // Settlement calculation data
  const [calculationData, setCalculationData] = useState({
    calculationQuantityMT: 0,
    calculationQuantityBBL: 0,
    benchmarkAmount: 0,
    adjustmentAmount: 0,
    calculationNote: ''
  });

  // Payment terms data
  const [paymentTermsData, setPaymentTermsData] = useState({
    paymentTerms: '',
    creditPeriodDays: 30,
    settlementType: 'TT',
    prepaymentPercentage: 0
  });

  // Store created settlement for calculation step
  const [createdSettlement, setCreatedSettlement] = useState<any>(null);

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

  const handleNext = async () => {
    // IMPORTANT: Check what step we're LEAVING, not entering
    // If leaving step 1 (quantities), we need to create settlement BEFORE moving to step 2 (pricing)
    if (activeStep === 0) {
      // Validating Step 0: Contract & Document Setup
      if (!validateStep(0)) return;
      // Step 0 is valid, move to Step 1
      setActiveStep(1);
    } else if (activeStep === 1) {
      // Validating Step 1: Quantities & Pricing
      // CRITICAL: First validate quantities exist
      if (formData.actualQuantityMT <= 0 || formData.actualQuantityBBL <= 0) {
        setError('Both MT and BBL quantities must be greater than zero');
        return;
      }

      // CRITICAL: If in create mode and settlement doesn't exist, CREATE IT NOW
      if (mode === 'create' && !createdSettlement) {
        try {
          setError(null);
          setLoading(true);

          // CRITICAL FIX: Use the returned settlement data, not the state!
          // State updates are asynchronous and won't take effect immediately
          const settlement = await handleCreateSettlement();

          // Check if settlement was actually created
          if (!settlement) {
            setError('Settlement creation failed. Please check the error message above.');
            setLoading(false);
            return;
          }

          // CRITICAL: Settlement created successfully!
          // DO NOT move to next step yet - user needs to see and fill pricing form on this step
          // The component will re-render with createdSettlement now truthy
          // The pricing form {createdSettlement && (...)} will now display on Step 1
          // User can fill benchmark amount, adjustment amount, and click Calculate
          // When user clicks Next button again, THEN we move to Step 2
          setLoading(false);
          return; // Stay on Step 1, let component re-render with pricing form visible
        } catch (err: any) {
          // handleCreateSettlement already set error message
          console.error('Settlement creation error in handleNext:', err);
          setLoading(false);
        }
        return; // Don't proceed to next step on error
      }

      // If we reach here, settlement exists or we're in edit mode
      // User has already filled in pricing, now moving to Step 2 (Payment & Charges)
      setActiveStep(2);
    } else if (activeStep === 2) {
      // Validating Step 2: Payment & Charges
      if (!validateStep(2)) return;
      // Step 2 is valid, move to Step 3
      setActiveStep(3);
    } else if (activeStep === 3) {
      // Final step - will trigger handleSubmit instead
      handleSubmit();
    }
  };

  const handleCreateSettlement = async (): Promise<ContractSettlementDto | null> => {
    if (!selectedContract) {
      setError('No contract selected');
      return null;
    }

    setLoading(true);
    setError(null);

    try {
      const dto: CreateSettlementDto = {
        contractId: selectedContract.id,
        externalContractNumber: formData.externalContractNumber?.trim() || selectedContract.externalContractNumber,
        documentNumber: formData.documentNumber?.trim(),
        documentType: formData.documentType,
        documentDate: formData.documentDate,
        actualQuantityMT: formData.actualQuantityMT,
        actualQuantityBBL: formData.actualQuantityBBL,
        createdBy: 'CurrentUser',
        notes: formData.notes?.trim(),
        settlementCurrency: 'USD',
        autoCalculatePrices: false, // Don't auto-calculate, user will do it manually
        autoTransitionStatus: false
      };

      const result = await settlementApi.createSettlement(dto);
      if (result.isSuccessful && result.settlementId) {
        // Reload the created settlement to get its full data
        const createdData = await getSettlementWithFallback(result.settlementId);
        setCreatedSettlement(createdData);
        // CRITICAL: Return the settlement data immediately so caller can use it
        // Don't rely on state update which is asynchronous!
        return createdData;
      } else {
        setError(result.errorMessage || 'Failed to create settlement');
        throw new Error(result.errorMessage || 'Failed to create settlement');
      }
    } catch (err: any) {
      console.error('Error creating settlement:', err);
      const errorMessage = err?.response?.data?.errorMessage || err?.response?.data?.message || err?.message || 'Failed to create settlement';
      const validationErrors = err?.response?.data?.validationErrors || [];
      const detailedErrors = validationErrors.length > 0
        ? `${errorMessage} - ${validationErrors.join(', ')}`
        : errorMessage;
      setError(`Failed to create settlement: ${detailedErrors}`);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const handleBack = () => {
    setActiveStep((prev) => prev - 1);
  };

  const validateStep = (step: number): boolean => {
    setError(null);

    switch (step) {
      case 0: // Contract & Document Setup (merged: Contract Selection + Document Information)
        // Contract validation
        if (!selectedContract) {
          setError('Please select a contract');
          return false;
        }
        // Document information validation
        if (!formData.documentNumber || !formData.documentNumber.trim()) {
          setError('Document number is required');
          return false;
        }
        if (!formData.documentDate) {
          setError('Document date is required');
          return false;
        }
        return true;

      case 1: // Quantities & Pricing (merged: Quantity Calculation + Settlement Calculation)
        // NOTE: Quantity validation is done in handleNext() before creating settlement
        // This validateStep is called from Step 2 validation only
        // So if we reach here, settlement should already exist in create mode
        if (mode === 'create' && createdSettlement && calculationData.benchmarkAmount === 0) {
          setError('Settlement calculation is required. Please enter the benchmark amount and click "Calculate" button to persist your values.');
          return false;
        }
        return true;

      case 2: // Payment & Charges (merged: Payment Terms + Initial Charges)
        // Payment terms validation
        if (!paymentTermsData.paymentTerms || !paymentTermsData.paymentTerms.trim()) {
          setError('Payment terms are required');
          return false;
        }
        if (paymentTermsData.creditPeriodDays < 0) {
          setError('Credit period days cannot be negative');
          return false;
        }
        if (paymentTermsData.prepaymentPercentage < 0 || paymentTermsData.prepaymentPercentage > 100) {
          setError('Prepayment percentage must be between 0 and 100');
          return false;
        }
        // Charges are optional, no additional validation needed
        return true;

      default:
        return true;
    }
  };

  const handleSubmit = async () => {
    if (mode === 'create') {
      // In create mode, we already created the settlement when transitioning to calculation step
      // Just call onSuccess to complete the workflow
      if (!createdSettlement) {
        setError('Settlement was not created. Please go back and review your information.');
        return;
      }
      onSuccess();
    } else if (mode === 'edit' && settlementId) {
      // In edit mode, update the settlement
      if (!selectedContract) {
        setError('No contract selected');
        return;
      }

      setLoading(true);
      setError(null);

      try {
        const dto: any = {
          documentNumber: formData.documentNumber?.trim(),
          documentType: formData.documentType,
          documentDate: formData.documentDate,
          actualQuantityMT: formData.actualQuantityMT,
          actualQuantityBBL: formData.actualQuantityBBL,
          notes: formData.notes?.trim()
        };

        await settlementApi.updateSettlement(settlementId, dto);
        onSuccess();
      } catch (err: any) {
        console.error('Error updating settlement:', err);
        const errorMessage = err?.response?.data?.errorMessage || err?.response?.data?.message || err?.message || 'Failed to update settlement';
        const validationErrors = err?.response?.data?.validationErrors || [];
        const detailedErrors = validationErrors.length > 0
          ? `${errorMessage} - ${validationErrors.join(', ')}`
          : errorMessage;
        setError(`Failed to update settlement: ${detailedErrors}`);
      } finally {
        setLoading(false);
      }
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
      // Load payment terms from contract (if available from full contract data)
      loadPaymentTermsFromContract(contractId);
    }
  };

  const loadPaymentTermsFromContract = async (contractId: string) => {
    try {
      // Try to load the full contract to get payment terms
      const contract = selectedContract;
      if (contract?.type === 'purchase') {
        const fullContract = await purchaseContractsApi.getById(contractId);
        if (fullContract) {
          const settlementTypeValue = typeof fullContract.settlementType === 'string'
            ? fullContract.settlementType
            : String(fullContract.settlementType);
          setPaymentTermsData({
            paymentTerms: fullContract.paymentTerms || '',
            creditPeriodDays: fullContract.creditPeriodDays || 30,
            settlementType: settlementTypeValue || 'TT',
            prepaymentPercentage: fullContract.prepaymentPercentage || 0
          });
        }
      } else if (contract?.type === 'sales') {
        const fullContract = await salesContractsApi.getById(contractId);
        if (fullContract) {
          const settlementTypeValue = typeof fullContract.settlementType === 'string'
            ? fullContract.settlementType
            : String(fullContract.settlementType);
          setPaymentTermsData({
            paymentTerms: fullContract.paymentTerms || '',
            creditPeriodDays: fullContract.creditPeriodDays || 30,
            settlementType: settlementTypeValue || 'TT',
            prepaymentPercentage: fullContract.prepaymentPercentage || 0
          });
        }
      }
    } catch (err) {
      console.error('Error loading payment terms from contract:', err);
      // Use defaults if loading fails
    }
  };

  const getContractDisplayLabel = (contract: ContractInfo): string => {
    const external = contract.externalContractNumber ? ` (${contract.externalContractNumber})` : '';
    return `${contract.contractNumber}${external}`;
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
      case 0: // Contract & Document Setup (merged: Contract Selection + Document Information)
        return (
          <Box>
            <Typography paragraph sx={{ mb: 3 }}>
              Select the contract and enter the Bill of Lading or Certificate of Quantity information.
            </Typography>

            {/* Contract Selection Section */}
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
              1. Select Contract
            </Typography>

            <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
              <Tabs
                value={contractSelectionTab === 'external' ? 1 : 0}
                onChange={(_e, newValue) => setContractSelectionTab(newValue === 1 ? 'external' : 'dropdown')}
              >
                <Tab label="Select from Dropdown" />
                <Tab label="Resolve by External Number" />
              </Tabs>
            </Box>

            {contractSelectionTab === 'dropdown' ? (
              <>
                <FormControl fullWidth required sx={{ mb: 2 }}>
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
                  <Alert severity="info" sx={{ mb: 3 }}>
                    Selected: <strong>{getContractDisplayLabel(selectedContract)}</strong> •
                    {selectedContract.type === 'purchase' ? selectedContract.supplierName : selectedContract.customerName} •
                    {selectedContract.productName}
                  </Alert>
                )}
              </>
            ) : (
              <Box sx={{ mb: 3 }}>
                <ContractResolver
                  onContractSelected={(contractId, contract) => {
                    if (contract) {
                      const contractInfo: ContractInfo = {
                        id: contractId,
                        contractNumber: contract.contractNumber,
                        externalContractNumber: contract.externalContractNumber,
                        type: contract.contractType.toLowerCase() as 'purchase' | 'sales',
                        [contract.contractType === 'Purchase' ? 'supplierName' : 'customerName']: contract.tradingPartnerName,
                        productName: contract.productName,
                        quantity: contract.quantity,
                        quantityUnit: contract.quantityUnit,
                        tonBarrelRatio: 7.33
                      };
                      setSelectedContract(contractInfo);
                      setFormData(prev => ({
                        ...prev,
                        contractId: contractId,
                        externalContractNumber: contract.externalContractNumber
                      }));
                    }
                  }}
                  allowManualInput={true}
                />
              </Box>
            )}

            {/* Document Information Section */}
            {selectedContract && (
              <>
                <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2, mt: 4 }}>
                  2. Document Information
                </Typography>
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
              </>
            )}
          </Box>
        );

      case 1: // Quantities & Pricing (merged: Quantity Calculation + Settlement Calculation)
        return (
          <Box>
            <Typography paragraph sx={{ mb: 3 }}>
              Enter the actual quantities from the document and configure pricing calculations.
            </Typography>

            {/* Quantity Calculation Section */}
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
              1. Actual Quantities
            </Typography>
            <Box sx={{ mb: 4 }}>
              <QuantityCalculator
                initialData={{
                  actualQuantityMT: formData.actualQuantityMT,
                  actualQuantityBBL: formData.actualQuantityBBL
                }}
                contractQuantity={selectedContract?.quantity}
                contractUnit={selectedContract?.quantityUnit}
                productDensity={850}
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

            {/* Settlement Pricing Section */}
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
              2. Settlement Pricing
            </Typography>
            <Box>
              {createdSettlement && (
                <>
                  <Alert severity="info" sx={{ mb: 3 }}>
                    ℹ️ <strong>Settlement created successfully!</strong> Now enter the pricing information and click the "Calculate" button to calculate final settlement amounts.
                  </Alert>
                  <Alert severity="warning" sx={{ mb: 3 }}>
                    ⚠️ <strong>Important:</strong> You must enter the Benchmark Amount and click "Calculate" below. Your pricing and quantity information will NOT be saved unless you click the Calculate button.
                  </Alert>
                  <SettlementCalculationForm
                    settlement={createdSettlement}
                    contractType={selectedContract?.type || 'purchase'}
                    onSuccess={(updatedSettlement) => {
                      setCreatedSettlement(updatedSettlement);
                      setCalculationData({
                        calculationQuantityMT: updatedSettlement.calculationQuantityMT || 0,
                        calculationQuantityBBL: updatedSettlement.calculationQuantityBBL || 0,
                        benchmarkAmount: updatedSettlement.benchmarkAmount || 0,
                        adjustmentAmount: updatedSettlement.adjustmentAmount || 0,
                        calculationNote: updatedSettlement.quantityCalculationNote || ''
                      });
                    }}
                    onError={(error) => {
                      setError(`Calculation failed: ${error.message}`);
                    }}
                  />
                </>
              )}
            </Box>
          </Box>
        );

      case 2: // Payment & Charges (merged: Payment Terms + Initial Charges)
        return (
          <Box>
            <Typography paragraph sx={{ mb: 3 }}>
              Configure payment terms and add any charges for this settlement.
            </Typography>

            {/* Payment Terms Section */}
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
              1. Payment Terms
            </Typography>
            <Grid container spacing={3} sx={{ mb: 4 }}>
              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  required
                  label="Payment Terms"
                  placeholder="e.g., NET 30, CASH ON DELIVERY, LC AT SIGHT"
                  value={paymentTermsData.paymentTerms}
                  onChange={(e) => setPaymentTermsData(prev => ({ ...prev, paymentTerms: e.target.value }))}
                  disabled={loading}
                  helperText="Specify the agreed payment terms (e.g., NET 30, LC, SWIFT)"
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  required
                  label="Credit Period (Days)"
                  type="number"
                  value={paymentTermsData.creditPeriodDays}
                  onChange={(e) => setPaymentTermsData(prev => ({ ...prev, creditPeriodDays: parseInt(e.target.value) || 0 }))}
                  disabled={loading}
                  inputProps={{ min: 0, max: 365 }}
                  helperText="Number of days for payment settlement after delivery"
                />
              </Grid>

              <Grid item xs={12} md={6}>
                <FormControl fullWidth required>
                  <InputLabel>Settlement Type</InputLabel>
                  <Select
                    value={paymentTermsData.settlementType}
                    label="Settlement Type"
                    onChange={(e) => setPaymentTermsData(prev => ({ ...prev, settlementType: e.target.value }))}
                    disabled={loading}
                  >
                    <MenuItem value="TT">Telegraphic Transfer (TT)</MenuItem>
                    <MenuItem value="LC">Letter of Credit (LC)</MenuItem>
                    <MenuItem value="DP">Documents Against Payment (DP)</MenuItem>
                    <MenuItem value="DA">Documents Against Acceptance (DA)</MenuItem>
                    <MenuItem value="CAD">Cash Against Documents (CAD)</MenuItem>
                    <MenuItem value="OA">Open Account (OA)</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12} md={6}>
                <TextField
                  fullWidth
                  label="Prepayment Percentage"
                  type="number"
                  value={paymentTermsData.prepaymentPercentage}
                  onChange={(e) => setPaymentTermsData(prev => ({ ...prev, prepaymentPercentage: parseFloat(e.target.value) || 0 }))}
                  disabled={loading}
                  inputProps={{ min: 0, max: 100, step: 0.01 }}
                  helperText="Percentage of contract value required as prepayment (0-100%)"
                  InputProps={{
                    endAdornment: <InputAdornment position="end">%</InputAdornment>
                  }}
                />
              </Grid>
            </Grid>

            {/* Charges Section */}
            <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
              2. Initial Charges
            </Typography>
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

      case 3: // Review & Finalize
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
                <Typography variant="subtitle2" gutterBottom>Actual Quantities</Typography>
                <Typography variant="body2">MT: {formData.actualQuantityMT.toLocaleString()}</Typography>
                <Typography variant="body2">BBL: {formData.actualQuantityBBL.toLocaleString()}</Typography>
              </Grid>

              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" gutterBottom>Settlement Calculation</Typography>
                <Typography variant="body2">Benchmark Amount: ${calculationData.benchmarkAmount.toFixed(2)}</Typography>
                <Typography variant="body2">Adjustment Amount: ${calculationData.adjustmentAmount.toFixed(2)}</Typography>
                {calculationData.benchmarkAmount > 0 && (
                  <Typography variant="body2" sx={{ fontWeight: 'bold', color: 'primary.main', mt: 1 }}>
                    Calculation MT: {calculationData.calculationQuantityMT.toLocaleString()} MT
                  </Typography>
                )}
              </Grid>

              <Grid item xs={12} md={6}>
                <Typography variant="subtitle2" gutterBottom>Payment Terms</Typography>
                <Typography variant="body2">Payment Terms: {paymentTermsData.paymentTerms}</Typography>
                <Typography variant="body2">Credit Period: {paymentTermsData.creditPeriodDays} days</Typography>
                <Typography variant="body2">Settlement Type: {paymentTermsData.settlementType}</Typography>
                {paymentTermsData.prepaymentPercentage > 0 && (
                  <Typography variant="body2">Prepayment: {paymentTermsData.prepaymentPercentage}%</Typography>
                )}
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
                        onClick={handleNext}
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