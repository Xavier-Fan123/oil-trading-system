import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  Card,
  CardContent,
  CardHeader,
  Alert,
  CircularProgress,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  useCreatePurchaseContract,
  useUpdatePurchaseContract,
  usePurchaseContract,
  useTradingPartners,
  useProducts,
  usePriceBenchmarks
} from '@/hooks/useContracts';
import {
  CreatePurchaseContractDto,
  UpdatePurchaseContractDto,
  ContractType,
  QuantityUnit,
  PricingType,
  DeliveryTerms,
  SettlementType,
  QuantityCalculationMode,
  QuantityCalculationModeLabels,
} from '@/types/contracts';

interface ContractFormProps {
  contractId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const ContractForm: React.FC<ContractFormProps> = ({
  contractId,
  onSuccess,
  onCancel
}) => {
  const isEditing = Boolean(contractId);
  
  const { data: contract, isLoading: loadingContract } = usePurchaseContract(contractId || '');
  const { data: tradingPartners, isLoading: loadingPartners } = useTradingPartners();
  const { data: products, isLoading: loadingProducts } = useProducts();
  const { data: priceBenchmarks, isLoading: loadingBenchmarks } = usePriceBenchmarks();
  
  const createMutation = useCreatePurchaseContract();
  const updateMutation = useUpdatePurchaseContract();

  const [formData, setFormData] = useState<CreatePurchaseContractDto>({
    externalContractNumber: '',
    contractType: ContractType.Physical,
    supplierId: '',
    productId: '',
    traderId: 'system', // Default trader ID
    quantity: 0,
    quantityUnit: QuantityUnit.MT,
    tonBarrelRatio: 7.6,
    priceBenchmarkId: '',
    pricingType: PricingType.Fixed,
    fixedPrice: 0,
    // Mixed-unit pricing defaults
    benchmarkUnit: QuantityUnit.MT,
    adjustmentUnit: QuantityUnit.BBL,
    adjustmentAmount: 0,
    adjustmentCurrency: 'USD',
    calculationMode: QuantityCalculationMode.UseActualQuantities,
    contractualConversionRatio: 7.33,
    deliveryTerms: DeliveryTerms.FOB,
    laycanStart: new Date(),
    laycanEnd: new Date(),
    loadPort: '',
    dischargePort: '',
    settlementType: SettlementType.TT,
    creditPeriodDays: 30,
    prepaymentPercentage: 0,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    console.log('useEffect triggered:', { contract: !!contract, isEditing, contractId });
    if (contract && isEditing && contractId) {
      console.log('Setting formData from contract, adjustmentUnit will be:', QuantityUnit.BBL);
      setFormData({
        externalContractNumber: contract.externalContractNumber || '',
        contractType: contract.contractType,
        supplierId: contract.supplier.id,
        productId: contract.product.id,
        traderId: contract.traderId,
        quantity: contract.quantity,
        quantityUnit: contract.quantityUnit,
        tonBarrelRatio: contract.tonBarrelRatio,
        priceBenchmarkId: contract.priceBenchmarkId,
        pricingType: contract.pricingType,
        fixedPrice: contract.fixedPrice,
        pricingFormula: contract.pricingFormula,
        pricingPeriodStart: contract.pricingPeriodStart ? new Date(contract.pricingPeriodStart) : undefined,
        pricingPeriodEnd: contract.pricingPeriodEnd ? new Date(contract.pricingPeriodEnd) : undefined,
        // Load mixed-unit pricing fields (defaults used as contract structure doesn't include these yet)
        benchmarkUnit: QuantityUnit.MT,
        adjustmentUnit: QuantityUnit.BBL,
        adjustmentAmount: 0,
        adjustmentCurrency: 'USD',
        calculationMode: QuantityCalculationMode.UseActualQuantities,
        contractualConversionRatio: 7.33,
        deliveryTerms: contract.deliveryTerms,
        laycanStart: new Date(contract.laycanStart),
        laycanEnd: new Date(contract.laycanEnd),
        loadPort: contract.loadPort,
        dischargePort: contract.dischargePort,
        settlementType: contract.settlementType,
        creditPeriodDays: contract.creditPeriodDays,
        prepaymentPercentage: contract.prepaymentPercentage,
        paymentTerms: contract.paymentTerms,
        qualitySpecifications: contract.qualitySpecifications,
        inspectionAgency: contract.inspectionAgency,
        notes: contract.notes,
      });
    }
  }, [contract, isEditing, contractId]);

  // Debug: Monitor formData changes
  useEffect(() => {
    console.log('formData.adjustmentUnit changed to:', formData.adjustmentUnit);
  }, [formData.adjustmentUnit]);

  const handleInputChange = (field: keyof CreatePurchaseContractDto, value: any) => {
    // Debug logging for adjustmentUnit changes
    if (field === 'adjustmentUnit') {
      console.log('adjustmentUnit changing:', { field, value, type: typeof value });
    }
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.supplierId) newErrors.supplierId = 'Supplier is required';
    if (!formData.productId) newErrors.productId = 'Product is required';
    if (!formData.quantity || formData.quantity <= 0) newErrors.quantity = 'Valid quantity is required';
    if (!formData.loadPort.trim()) newErrors.loadPort = 'Load port is required';
    if (!formData.dischargePort.trim()) newErrors.dischargePort = 'Discharge port is required';
    if (formData.pricingType === PricingType.Fixed && (!formData.fixedPrice || formData.fixedPrice <= 0)) {
      newErrors.fixedPrice = 'Fixed price is required for fixed pricing';
    }
    if (formData.laycanStart >= formData.laycanEnd) {
      newErrors.laycanEnd = 'Laycan end must be after laycan start';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    
    if (!validateForm()) return;

    try {
      if (isEditing && contractId) {
        const updateData: UpdatePurchaseContractDto = {
          quantity: formData.quantity,
          quantityUnit: formData.quantityUnit,
          tonBarrelRatio: formData.tonBarrelRatio,
          priceBenchmarkId: formData.priceBenchmarkId,
          pricingType: formData.pricingType,
          fixedPrice: formData.fixedPrice,
          pricingFormula: formData.pricingFormula,
          pricingPeriodStart: formData.pricingPeriodStart,
          pricingPeriodEnd: formData.pricingPeriodEnd,
          // Include mixed-unit pricing fields
          benchmarkUnit: formData.benchmarkUnit,
          adjustmentUnit: formData.adjustmentUnit,
          adjustmentAmount: formData.adjustmentAmount,
          adjustmentCurrency: formData.adjustmentCurrency,
          calculationMode: formData.calculationMode,
          contractualConversionRatio: formData.contractualConversionRatio,
          deliveryTerms: formData.deliveryTerms,
          laycanStart: formData.laycanStart,
          laycanEnd: formData.laycanEnd,
          loadPort: formData.loadPort,
          dischargePort: formData.dischargePort,
          settlementType: formData.settlementType,
          creditPeriodDays: formData.creditPeriodDays,
          prepaymentPercentage: formData.prepaymentPercentage,
          paymentTerms: formData.paymentTerms,
          qualitySpecifications: formData.qualitySpecifications,
          inspectionAgency: formData.inspectionAgency,
          notes: formData.notes,
        };
        await updateMutation.mutateAsync({ id: contractId, contract: updateData });
      } else {
        await createMutation.mutateAsync(formData);
      }
      onSuccess();
    } catch (error) {
      console.error('Error saving contract:', error);
    }
  };

  if (loadingContract || loadingPartners || loadingProducts || loadingBenchmarks) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  // Debug: Log current adjustmentUnit value before render
  console.log('Rendering ContractForm - adjustmentUnit:', formData.adjustmentUnit, 'Type:', typeof formData.adjustmentUnit);

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Paper sx={{ p: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h5" gutterBottom>
            {isEditing ? 'Edit Purchase Contract' : 'New Purchase Contract'}
          </Typography>
          <Box display="flex" flexDirection="column" alignItems="flex-end">
            {isEditing && contract?.contractNumber && (
              <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                Contract: {contract.contractNumber.value}
              </Typography>
            )}
            {!isEditing && (
              <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                Contract number will be auto-generated
              </Typography>
            )}
          </Box>
        </Box>

        <form onSubmit={handleSubmit}>
          <Grid container spacing={3}>
            {/* Basic Information */}
            <Grid item xs={12}>
              <Card>
                <CardHeader title="Basic Information" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <FormControl fullWidth error={!!errors.supplierId}>
                        <InputLabel>Supplier *</InputLabel>
                        <Select
                          value={formData.supplierId}
                          label="Supplier *"
                          onChange={(e) => handleInputChange('supplierId', e.target.value)}
                        >
                          {tradingPartners?.filter(p => p.type === 0 || p.type === 2).map(partner => (
                            <MenuItem key={partner.id} value={partner.id}>
                              {partner.name} ({partner.code})
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      {errors.supplierId && (
                        <Typography color="error" variant="caption">
                          {errors.supplierId}
                        </Typography>
                      )}
                    </Grid>

                    <Grid item xs={12} sm={6}>
                      <FormControl fullWidth error={!!errors.productId}>
                        <InputLabel>Product *</InputLabel>
                        <Select
                          value={formData.productId}
                          label="Product *"
                          onChange={(e) => handleInputChange('productId', e.target.value)}
                        >
                          {products?.map(product => (
                            <MenuItem key={product.id} value={product.id}>
                              {product.name} ({product.code})
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      {errors.productId && (
                        <Typography color="error" variant="caption">
                          {errors.productId}
                        </Typography>
                      )}
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Contract Type</InputLabel>
                        <Select
                          value={formData.contractType}
                          label="Contract Type"
                          onChange={(e) => handleInputChange('contractType', e.target.value)}
                        >
                          <MenuItem value={ContractType.Physical}>Physical</MenuItem>
                          <MenuItem value={ContractType.Paper}>Paper</MenuItem>
                          <MenuItem value={ContractType.Financial}>Financial</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>

                    <Grid item xs={12} sm={8}>
                      <TextField
                        fullWidth
                        label="External Contract Number"
                        value={formData.externalContractNumber || ''}
                        onChange={(e) => handleInputChange('externalContractNumber', e.target.value)}
                        helperText="Optional: Manual contract number for official records and reconciliation"
                        placeholder="Enter external contract number (e.g., SUPPLIER-001-2024)"
                      />
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Quantity *"
                        type="number"
                        value={formData.quantity}
                        onChange={(e) => handleInputChange('quantity', parseFloat(e.target.value) || 0)}
                        error={!!errors.quantity}
                        helperText={errors.quantity}
                      />
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Unit</InputLabel>
                        <Select
                          value={formData.quantityUnit}
                          label="Unit"
                          onChange={(e) => handleInputChange('quantityUnit', e.target.value)}
                        >
                          <MenuItem value={QuantityUnit.MT}>MT</MenuItem>
                          <MenuItem value={QuantityUnit.BBL}>BBL</MenuItem>
                          <MenuItem value={QuantityUnit.GAL}>GAL</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Pricing Information */}
            <Grid item xs={12}>
              <Card>
                <CardHeader title="Pricing Information" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <FormControl fullWidth>
                        <InputLabel>Pricing Type</InputLabel>
                        <Select
                          value={formData.pricingType}
                          label="Pricing Type"
                          onChange={(e) => handleInputChange('pricingType', e.target.value)}
                        >
                          <MenuItem value={PricingType.Fixed}>Fixed</MenuItem>
                          <MenuItem value={PricingType.Floating}>Floating</MenuItem>
                          <MenuItem value={PricingType.Formula}>Formula</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>

                    {(formData.pricingType === PricingType.Floating || formData.pricingType === PricingType.Formula) && (
                      <Grid item xs={12} sm={6}>
                        <FormControl fullWidth>
                          <InputLabel>Price Benchmark</InputLabel>
                          <Select
                            value={formData.priceBenchmarkId || ''}
                            label="Price Benchmark"
                            onChange={(e) => handleInputChange('priceBenchmarkId', e.target.value)}
                          >
                            <MenuItem value="">
                              <em>None</em>
                            </MenuItem>
                            {priceBenchmarks?.map(benchmark => (
                              <MenuItem key={benchmark.id} value={benchmark.id}>
                                {benchmark.benchmarkName} ({benchmark.currency}/{benchmark.unit})
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      </Grid>
                    )}

                    {formData.pricingType === PricingType.Fixed && (
                      <Grid item xs={12} sm={6}>
                        <TextField
                          fullWidth
                          label="Fixed Price (USD) *"
                          type="number"
                          value={formData.fixedPrice || ''}
                          onChange={(e) => handleInputChange('fixedPrice', parseFloat(e.target.value) || undefined)}
                          error={!!errors.fixedPrice}
                          helperText={errors.fixedPrice}
                        />
                      </Grid>
                    )}

                    {formData.pricingType === PricingType.Formula && (
                      <Grid item xs={12}>
                        <TextField
                          fullWidth
                          label="Pricing Formula"
                          multiline
                          rows={2}
                          value={formData.pricingFormula || ''}
                          onChange={(e) => handleInputChange('pricingFormula', e.target.value)}
                          placeholder="e.g., AVG(BRENT) + 5 USD/BBL"
                        />
                      </Grid>
                    )}

                    {formData.pricingType !== PricingType.Fixed && (
                      <>
                        <Grid item xs={12} sm={6}>
                          <DatePicker
                            label="Pricing Period Start"
                            value={formData.pricingPeriodStart}
                            onChange={(date) => handleInputChange('pricingPeriodStart', date)}
                            slotProps={{ textField: { fullWidth: true } }}
                          />
                        </Grid>
                        <Grid item xs={12} sm={6}>
                          <DatePicker
                            label="Pricing Period End"
                            value={formData.pricingPeriodEnd}
                            onChange={(date) => handleInputChange('pricingPeriodEnd', date)}
                            slotProps={{ textField: { fullWidth: true } }}
                          />
                        </Grid>
                      </>
                    )}
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Settlement Configuration */}
            <Grid item xs={12}>
              <Card>
                <CardHeader 
                  title="Settlement Configuration" 
                  subheader="Configure mixed-unit pricing and quantity calculation modes for settlement"
                />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Benchmark Unit</InputLabel>
                        <Select
                          value={formData.benchmarkUnit ?? QuantityUnit.MT}
                          label="Benchmark Unit"
                          displayEmpty
                          onChange={(e) => handleInputChange('benchmarkUnit', Number(e.target.value))}
                        >
                          <MenuItem key={QuantityUnit.MT} value={QuantityUnit.MT}>MT</MenuItem>
                          <MenuItem key={QuantityUnit.BBL} value={QuantityUnit.BBL}>BBL</MenuItem>
                          <MenuItem key={QuantityUnit.GAL} value={QuantityUnit.GAL}>GAL</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Adjustment Unit</InputLabel>
                        <Select
                          key={`adjustment-unit-${formData.adjustmentUnit}`}
                          value={formData.adjustmentUnit ?? QuantityUnit.BBL}
                          label="Adjustment Unit"
                          displayEmpty
                          onChange={(e) => {
                            console.log('=== ADJUSTMENT UNIT CHANGE ===');
                            console.log('e.target.value:', e.target.value);
                            console.log('Number(e.target.value):', Number(e.target.value));
                            console.log('Current formData.adjustmentUnit:', formData.adjustmentUnit);
                            console.log('QuantityUnit.MT:', QuantityUnit.MT);
                            console.log('QuantityUnit.BBL:', QuantityUnit.BBL);
                            handleInputChange('adjustmentUnit', Number(e.target.value));
                          }}
                        >
                          <MenuItem key={QuantityUnit.MT} value={QuantityUnit.MT}>MT</MenuItem>
                          <MenuItem key={QuantityUnit.BBL} value={QuantityUnit.BBL}>BBL</MenuItem>
                          <MenuItem key={QuantityUnit.GAL} value={QuantityUnit.GAL}>GAL</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Adjustment Amount"
                        type="number"
                        value={formData.adjustmentAmount || 0}
                        onChange={(e) => handleInputChange('adjustmentAmount', parseFloat(e.target.value) || 0)}
                        helperText="Premium (+) or Discount (-)"
                        inputProps={{ step: 0.01 }}
                      />
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Adjustment Currency"
                        value={formData.adjustmentCurrency || 'USD'}
                        onChange={(e) => handleInputChange('adjustmentCurrency', e.target.value)}
                        placeholder="USD"
                      />
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Calculation Mode</InputLabel>
                        <Select
                          value={formData.calculationMode || QuantityCalculationMode.UseActualQuantities}
                          label="Calculation Mode"
                          onChange={(e) => handleInputChange('calculationMode', e.target.value)}
                        >
                          {Object.entries(QuantityCalculationModeLabels).map(([value, label]) => (
                            <MenuItem key={value} value={parseInt(value)}>
                              {label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Contractual Conversion Ratio"
                        type="number"
                        value={formData.contractualConversionRatio || 7.33}
                        onChange={(e) => handleInputChange('contractualConversionRatio', parseFloat(e.target.value) || 7.33)}
                        helperText="BBL per MT"
                        inputProps={{ step: 0.01, min: 0 }}
                      />
                    </Grid>

                    <Grid item xs={12}>
                      <Alert severity="info">
                        <Typography variant="body2">
                          <strong>Settlement Configuration:</strong> This configuration allows contracts to use different units for benchmark prices and adjustments. 
                          For example, benchmark price in MT and adjustments in BBL. The calculation mode determines how quantities are handled during settlement.
                        </Typography>
                      </Alert>
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Delivery Information */}
            <Grid item xs={12}>
              <Card>
                <CardHeader title="Delivery Information" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Delivery Terms</InputLabel>
                        <Select
                          value={formData.deliveryTerms}
                          label="Delivery Terms"
                          onChange={(e) => handleInputChange('deliveryTerms', e.target.value)}
                        >
                          <MenuItem value={DeliveryTerms.FOB}>FOB</MenuItem>
                          <MenuItem value={DeliveryTerms.CIF}>CIF</MenuItem>
                          <MenuItem value={DeliveryTerms.CFR}>CFR</MenuItem>
                          <MenuItem value={DeliveryTerms.DAP}>DAP</MenuItem>
                          <MenuItem value={DeliveryTerms.DDP}>DDP</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Load Port *"
                        value={formData.loadPort}
                        onChange={(e) => handleInputChange('loadPort', e.target.value)}
                        error={!!errors.loadPort}
                        helperText={errors.loadPort}
                      />
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Discharge Port *"
                        value={formData.dischargePort}
                        onChange={(e) => handleInputChange('dischargePort', e.target.value)}
                        error={!!errors.dischargePort}
                        helperText={errors.dischargePort}
                      />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                      <DatePicker
                        label="Laycan Start *"
                        value={formData.laycanStart}
                        onChange={(date) => handleInputChange('laycanStart', date)}
                        slotProps={{ textField: { fullWidth: true } }}
                      />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                      <DatePicker
                        label="Laycan End *"
                        value={formData.laycanEnd}
                        onChange={(date) => handleInputChange('laycanEnd', date)}
                        slotProps={{ 
                          textField: { 
                            fullWidth: true,
                            error: !!errors.laycanEnd,
                            helperText: errors.laycanEnd
                          } 
                        }}
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Payment Terms */}
            <Grid item xs={12}>
              <Card>
                <CardHeader title="Payment Terms" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={4}>
                      <FormControl fullWidth>
                        <InputLabel>Settlement Type</InputLabel>
                        <Select
                          value={formData.settlementType}
                          label="Settlement Type"
                          onChange={(e) => handleInputChange('settlementType', e.target.value)}
                        >
                          <MenuItem value={SettlementType.TT}>TT (Telegraphic Transfer)</MenuItem>
                          <MenuItem value={SettlementType.LC}>LC (Letter of Credit)</MenuItem>
                          <MenuItem value={SettlementType.CAD}>CAD (Cash Against Documents)</MenuItem>
                        </Select>
                      </FormControl>
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Credit Period (Days)"
                        type="number"
                        value={formData.creditPeriodDays}
                        onChange={(e) => handleInputChange('creditPeriodDays', parseInt(e.target.value) || 0)}
                      />
                    </Grid>

                    <Grid item xs={12} sm={4}>
                      <TextField
                        fullWidth
                        label="Prepayment (%)"
                        type="number"
                        value={formData.prepaymentPercentage}
                        onChange={(e) => handleInputChange('prepaymentPercentage', parseFloat(e.target.value) || 0)}
                        inputProps={{ min: 0, max: 100 }}
                      />
                    </Grid>

                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Payment Terms"
                        multiline
                        rows={2}
                        value={formData.paymentTerms || ''}
                        onChange={(e) => handleInputChange('paymentTerms', e.target.value)}
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Additional Information */}
            <Grid item xs={12}>
              <Card>
                <CardHeader title="Additional Information" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Inspection Agency"
                        value={formData.inspectionAgency || ''}
                        onChange={(e) => handleInputChange('inspectionAgency', e.target.value)}
                      />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                      <TextField
                        fullWidth
                        label="Ton/Barrel Ratio"
                        type="number"
                        value={formData.tonBarrelRatio}
                        onChange={(e) => handleInputChange('tonBarrelRatio', parseFloat(e.target.value) || 7.6)}
                        inputProps={{ step: 0.1 }}
                      />
                    </Grid>

                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Quality Specifications"
                        multiline
                        rows={3}
                        value={formData.qualitySpecifications || ''}
                        onChange={(e) => handleInputChange('qualitySpecifications', e.target.value)}
                      />
                    </Grid>

                    <Grid item xs={12}>
                      <TextField
                        fullWidth
                        label="Notes"
                        multiline
                        rows={3}
                        value={formData.notes || ''}
                        onChange={(e) => handleInputChange('notes', e.target.value)}
                      />
                    </Grid>
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          {/* Form Actions */}
          <Box mt={3} display="flex" gap={2} justifyContent="flex-end">
            <Button variant="outlined" onClick={onCancel}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              disabled={createMutation.isPending || updateMutation.isPending}
            >
              {createMutation.isPending || updateMutation.isPending ? (
                <CircularProgress size={20} />
              ) : (
                isEditing ? 'Update Contract' : 'Create Contract'
              )}
            </Button>
          </Box>

          {/* Error Message */}
          {(createMutation.error || updateMutation.error) && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {createMutation.error?.message || updateMutation.error?.message}
            </Alert>
          )}
        </form>
      </Paper>
    </LocalizationProvider>
  );
};