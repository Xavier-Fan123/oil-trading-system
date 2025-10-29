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
  Divider,
  Alert,
  CircularProgress,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
  useCreateSalesContract,
  useUpdateSalesContract,
  useSalesContract,
} from '@/hooks/useSalesContracts';
import { useTradingPartners, useProducts, usePriceBenchmarks, useUsers } from '@/hooks/useContracts';
import {
  DeliveryTerms,
  SettlementType,
  PricingType,
  QuantityUnit,
  ContractType,
  CreateSalesContractDto,
} from '@/types/salesContracts';

interface SalesContractFormProps {
  contractId?: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export const SalesContractForm: React.FC<SalesContractFormProps> = ({
  contractId,
  onSuccess,
  onCancel,
}) => {
  const isEdit = !!contractId;

  // Form state
  // Initialize with proper date ranges (end > start to pass business rule validation)
  const getInitialDates = () => {
    const startDate = new Date();
    const endDate = new Date(startDate);
    endDate.setDate(endDate.getDate() + 1); // End date must be 1 day after start
    return { startDate, endDate };
  };

  const { startDate: initialStart, endDate: initialEnd } = getInitialDates();

  const [formData, setFormData] = useState<CreateSalesContractDto>({
    externalContractNumber: '',
    contractType: ContractType.CARGO,
    customerId: '',
    productId: '',
    traderId: '', // Empty - user must select a trader
    priceBenchmarkId: '',
    quantity: 0,
    quantityUnit: QuantityUnit.MT,
    tonBarrelRatio: 7.6,
    pricingType: PricingType.Fixed,
    fixedPrice: 0,
    pricingFormula: '',
    pricingPeriodStart: undefined,
    pricingPeriodEnd: undefined,
    deliveryTerms: DeliveryTerms.FOB,
    laycanStart: initialStart,
    laycanEnd: initialEnd,
    loadPort: '',
    dischargePort: '',
    settlementType: SettlementType.TT,
    creditPeriodDays: 30,
    prepaymentPercentage: 0,
    paymentTerms: '',
    qualitySpecifications: '',
    inspectionAgency: '',
    notes: '',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [laycanStart, setLaycanStart] = useState<Date | null>(initialStart);
  const [laycanEnd, setLaycanEnd] = useState<Date | null>(initialEnd);

  // Fetch data
  const { data: existingContract, isLoading: contractLoading } = useSalesContract(contractId || '');
  const { data: tradingPartners, isLoading: loadingPartners } = useTradingPartners();
  const { data: products, isLoading: loadingProducts } = useProducts();
  const { data: priceBenchmarks, isLoading: loadingBenchmarks } = usePriceBenchmarks();
  const { data: users, isLoading: loadingUsers } = useUsers();

  // Mutations
  const createMutation = useCreateSalesContract();
  const updateMutation = useUpdateSalesContract();

  // Helper function to convert string enum names to numeric enum values
  const convertContractTypeToEnum = (value: any): number => {
    if (typeof value === 'number') return value;
    const mapping: Record<string, number> = {
      'Cargo': ContractType.CARGO,
      'Exw': ContractType.EXW,
      'Del': ContractType.DEL,
      'CARGO': ContractType.CARGO,
      'EXW': ContractType.EXW,
      'DEL': ContractType.DEL,
    };
    return mapping[String(value)] || ContractType.CARGO;
  };

  const convertDeliveryTermsToEnum = (value: any): number => {
    if (typeof value === 'number') return value;
    const mapping: Record<string, number> = {
      'FOB': DeliveryTerms.FOB,
      'CIF': DeliveryTerms.CIF,
      'CFR': DeliveryTerms.CFR,
      'DAP': DeliveryTerms.DAP,
      'DDP': DeliveryTerms.DDP,
    };
    return mapping[String(value)] || DeliveryTerms.FOB;
  };

  const convertQuantityUnitToEnum = (value: any): number => {
    if (typeof value === 'number') return value;
    const mapping: Record<string, number> = {
      'MT': QuantityUnit.MT,
      'BBL': QuantityUnit.BBL,
      'GAL': QuantityUnit.GAL,
    };
    return mapping[String(value)] || QuantityUnit.MT;
  };

  const convertPricingTypeToEnum = (value: any): number => {
    if (typeof value === 'number') return value;
    const mapping: Record<string, number> = {
      'Fixed': PricingType.Fixed,
      'Floating': PricingType.Floating,
      'Formula': PricingType.Formula,
    };
    return mapping[String(value)] || PricingType.Fixed;
  };

  const convertSettlementTypeToEnum = (value: any): number => {
    if (typeof value === 'number') return value;
    const mapping: Record<string, number> = {
      'TT': SettlementType.TT,
      'LC': SettlementType.LC,
      'CAD': SettlementType.CAD,
    };
    return mapping[String(value)] || SettlementType.TT;
  };

  // Load existing contract data
  useEffect(() => {
    if (existingContract) {
      // Extract customer and product IDs from nested objects
      const customerId = existingContract.customer?.id || existingContract.customerId || '';
      const productId = existingContract.product?.id || existingContract.productId || '';

      // Calculate unit price from contractValue and quantity
      // contractValue is total, fixedPrice should be unit price (per MT/BBL)
      const quantity = existingContract.quantity || 0;
      const contractValue = existingContract.contractValue || 0;
      const fixedPrice = quantity > 0 ? contractValue / quantity : 0;

      setFormData(prev => ({
        ...prev,
        // Basic Information
        externalContractNumber: existingContract.externalContractNumber || '',
        contractType: convertContractTypeToEnum(existingContract.contractType),
        customerId: customerId,
        productId: productId,
        traderId: existingContract.traderId || '',
        quantity: existingContract.quantity || 0,
        quantityUnit: convertQuantityUnitToEnum(existingContract.quantityUnit),
        tonBarrelRatio: existingContract.tonBarrelRatio || 7.6,

        // Pricing Information
        priceBenchmarkId: existingContract.priceBenchmarkId || '',
        pricingType: convertPricingTypeToEnum(existingContract.pricingType),
        fixedPrice: fixedPrice,
        pricingFormula: existingContract.pricingFormula || '',
        pricingPeriodStart: existingContract.pricingPeriodStart ? new Date(existingContract.pricingPeriodStart) : undefined,
        pricingPeriodEnd: existingContract.pricingPeriodEnd ? new Date(existingContract.pricingPeriodEnd) : undefined,

        // Delivery Information
        deliveryTerms: convertDeliveryTermsToEnum(existingContract.deliveryTerms),
        laycanStart: new Date(existingContract.laycanStart),
        laycanEnd: new Date(existingContract.laycanEnd),
        loadPort: existingContract.loadPort || '',
        dischargePort: existingContract.dischargePort || '',

        // Payment Terms
        settlementType: convertSettlementTypeToEnum(existingContract.settlementType),
        creditPeriodDays: existingContract.creditPeriodDays || 30,
        prepaymentPercentage: existingContract.prepaymentPercentage || 0,
        paymentTerms: existingContract.paymentTerms || '',

        // Additional Information
        qualitySpecifications: existingContract.qualitySpecifications || '',
        inspectionAgency: existingContract.inspectionAgency || '',
        notes: existingContract.notes || '',
      }));
      setLaycanStart(new Date(existingContract.laycanStart));
      setLaycanEnd(new Date(existingContract.laycanEnd));
    }
  }, [existingContract]);

  const handleInputChange = (field: keyof CreateSalesContractDto, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Convert enums to string names for API - using correct enum values
    // Frontend enums are 1-based: ContractType.CARGO = 1, EXW = 2, DEL = 3
    const contractTypeNames: Record<number, string> = { 1: 'Cargo', 2: 'Exw', 3: 'Del' };

    // DeliveryTerms enum: FOB=1, CIF=2, CFR=3, DAP=4, DDP=5
    const deliveryTermsNames: Record<number, string> = { 1: 'FOB', 2: 'CIF', 3: 'CFR', 4: 'DAP', 5: 'DDP' };

    // QuantityUnit enum: MT=1, BBL=2, GAL=3
    const quantityUnitNames: Record<number, string> = { 1: 'MT', 2: 'BBL', 3: 'GAL' };

    // PricingType enum: Fixed=1, Floating=2, Formula=3
    const pricingTypeNames: Record<number, string> = { 1: 'Fixed', 2: 'Floating', 3: 'Formula' };

    // SettlementType enum: TT=1, LC=2, CAD=3
    const settlementTypeNames: Record<number, string> = { 1: 'TT', 2: 'LC', 3: 'CAD' };

    const submitData = {
      ...formData,
      contractType: contractTypeNames[formData.contractType],
      deliveryTerms: deliveryTermsNames[formData.deliveryTerms],
      quantityUnit: quantityUnitNames[formData.quantityUnit],
      pricingType: pricingTypeNames[formData.pricingType],
      settlementType: settlementTypeNames[formData.settlementType],
      // Only include priceBenchmarkId if it's a valid non-empty string (prevent empty string validation error)
      priceBenchmarkId: formData.priceBenchmarkId && formData.priceBenchmarkId.trim() ? formData.priceBenchmarkId : undefined,
      laycanStart: laycanStart || new Date(),
      laycanEnd: laycanEnd || new Date(),
    } as any;

    console.log('Submitting sales contract data:', JSON.stringify(submitData, null, 2));

    try {
      if (isEdit && contractId) {
        await updateMutation.mutateAsync({ id: contractId, contract: submitData });
      } else {
        await createMutation.mutateAsync(submitData);
      }
      onSuccess();
    } catch (error: any) {
      console.error('Error saving contract:', error);
      if (error.response?.data) {
        console.error('API Response Error Details:', error.response.data);
      }
    }
  };

  const totalValue = formData.quantity * (formData.fixedPrice || 0);

  if (contractLoading || loadingPartners || loadingProducts || loadingBenchmarks || loadingUsers) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Box>
        <Paper sx={{ p: 3 }}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h5" gutterBottom>
              {isEdit ? 'Edit Sales Contract' : 'Create New Sales Contract'}
            </Typography>
            <Box display="flex" flexDirection="column" alignItems="flex-end">
              {isEdit && existingContract?.externalContractNumber && (
                <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                  Contract: {existingContract.externalContractNumber}
                </Typography>
              )}
              {isEdit && !existingContract?.externalContractNumber && (
                <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                  No external contract number
                </Typography>
              )}
              {!isEdit && (
                <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                  Enter external contract number below
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
                        <FormControl fullWidth error={!!errors.customerId}>
                          <InputLabel>Customer *</InputLabel>
                          <Select
                            value={formData.customerId}
                            label="Customer *"
                            onChange={(e) => handleInputChange('customerId', e.target.value)}
                          >
                            {tradingPartners?.filter(p => {
                              const partnerTypeStr = String(p.partnerType);
                              return partnerTypeStr === 'Customer' || partnerTypeStr === 'Both';
                            }).map(partner => (
                              <MenuItem key={partner.id} value={partner.id}>
                                {partner.companyName} ({partner.companyCode})
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                        {errors.customerId && (
                          <Typography color="error" variant="caption">
                            {errors.customerId}
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
                            onChange={(e) => handleInputChange('contractType', Number(e.target.value))}
                          >
                            <MenuItem value={ContractType.CARGO}>CARGO</MenuItem>
                            <MenuItem value={ContractType.EXW}>EXW</MenuItem>
                            <MenuItem value={ContractType.DEL}>DEL</MenuItem>
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
                          placeholder="Enter external contract number (e.g., CUSTOMER-001-2024)"
                        />
                      </Grid>

                      <Grid item xs={12} sm={4}>
                        <TextField
                          fullWidth
                          label="Quantity *"
                          type="number"
                          value={formData.quantity || ''}
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
                            onChange={(e) => handleInputChange('quantityUnit', Number(e.target.value))}
                          >
                            <MenuItem value={QuantityUnit.MT}>MT</MenuItem>
                            <MenuItem value={QuantityUnit.BBL}>BBL</MenuItem>
                            <MenuItem value={QuantityUnit.GAL}>GAL</MenuItem>
                          </Select>
                        </FormControl>
                      </Grid>

                      <Grid item xs={12} sm={4}>
                        <FormControl fullWidth>
                          <InputLabel>Trader</InputLabel>
                          <Select
                            value={formData.traderId}
                            label="Trader"
                            onChange={(e) => handleInputChange('traderId', e.target.value)}
                          >
                            {users?.map(user => (
                              <MenuItem key={user.id} value={user.id}>
                                {user.fullName || user.email || `User ${user.id}`}
                              </MenuItem>
                            ))}
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
                            onChange={(e) => handleInputChange('pricingType', Number(e.target.value))}
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
                            label={`Fixed Price (USD per ${formData.quantityUnit === 1 ? 'MT' : formData.quantityUnit === 2 ? 'BBL' : 'Unit'}) *`}
                            type="number"
                            value={formData.fixedPrice ?? ''}
                            onChange={(e) => handleInputChange('fixedPrice', e.target.value ? parseFloat(e.target.value) : undefined)}
                            error={!!errors.fixedPrice}
                            helperText={errors.fixedPrice || `Unit price × ${formData.quantity || 0} = Total: $${((formData.fixedPrice || 0) * (formData.quantity || 0)).toLocaleString()}`}
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

                      <Grid item xs={12} sm={6}>
                        <TextField
                          fullWidth
                          label="Ton/Barrel Ratio"
                          type="number"
                          value={formData.tonBarrelRatio || ''}
                          onChange={(e) => handleInputChange('tonBarrelRatio', e.target.value ? parseFloat(e.target.value) : 0)}
                          inputProps={{ step: 0.1 }}
                        />
                      </Grid>

                      <Grid item xs={12} sm={6}>
                        <Typography variant="h6" sx={{ mt: 2 }}>
                          Total Value: ${totalValue.toLocaleString()}
                        </Typography>
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
                            onChange={(e) => handleInputChange('deliveryTerms', Number(e.target.value))}
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
                            onChange={(e) => handleInputChange('settlementType', Number(e.target.value))}
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
                          value={formData.creditPeriodDays || ''}
                          onChange={(e) => handleInputChange('creditPeriodDays', e.target.value ? parseInt(e.target.value) : 0)}
                        />
                      </Grid>

                      <Grid item xs={12} sm={4}>
                        <TextField
                          fullWidth
                          label="Prepayment (%)"
                          type="number"
                          value={formData.prepaymentPercentage || ''}
                          onChange={(e) => handleInputChange('prepaymentPercentage', e.target.value ? parseFloat(e.target.value) : 0)}
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

            <Divider sx={{ my: 3 }} />

            <Box display="flex" justifyContent="flex-end" gap={2}>
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
                  isEdit ? 'Update Contract' : 'Create Contract'
                )}
              </Button>
            </Box>

            {(createMutation.error || updateMutation.error) && (
              <Alert severity="error" sx={{ mt: 2 }}>
                Error saving contract: {createMutation.error?.message || updateMutation.error?.message}
              </Alert>
            )}
          </form>
        </Paper>
      </Box>
    </LocalizationProvider>
  );
};