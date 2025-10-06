import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardHeader,
  CardContent,
  TextField,
  Button,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  CircularProgress,
  Stack,
  Chip
} from '@mui/material';
import {
  Save as SaveIcon,
  Refresh as RefreshIcon,
  Link as LinkIcon
} from '@mui/icons-material';
import { useErrorHandler, useFormErrorHandler } from '@/hooks/useErrorHandler';
import ErrorDisplay from '@/components/Common/ErrorDisplay';
import { ValidationErrorDisplay } from '@/components/Common/ErrorDisplay';
import { contractMatchingApi, AvailablePurchase, UnmatchedSales, CreateMatchingRequest } from '@/services/contractMatchingApi';
import { ErrorCodes } from '@/types';

interface ContractMatchingFormProps {
  onMatchingCreated?: () => void;
  onCancel?: () => void;
}

const ContractMatchingForm: React.FC<ContractMatchingFormProps> = ({
  onMatchingCreated,
  onCancel
}) => {
  const [formData, setFormData] = useState<CreateMatchingRequest>({
    purchaseContractId: '',
    salesContractId: '',
    quantity: 0,
    notes: '',
    matchedBy: ''
  });
  
  const [availablePurchases, setAvailablePurchases] = useState<AvailablePurchase[]>([]);
  const [unmatchedSales, setUnmatchedSales] = useState<UnmatchedSales[]>([]);
  const [selectedPurchase, setSelectedPurchase] = useState<AvailablePurchase | null>(null);
  const [selectedSales, setSelectedSales] = useState<UnmatchedSales | null>(null);
  
  const { 
    error: loadError, 
    isLoading: isLoadingData, 
    executeWithErrorHandling 
  } = useErrorHandler({
    context: { component: 'ContractMatchingForm' }
  });
  
  const { 
    error: submitError, 
    isLoading: isSubmitting, 
    executeWithErrorHandling: executeSubmit 
  } = useErrorHandler({
    context: { component: 'ContractMatchingForm', action: 'submit' }
  });
  
  const {
    validationErrors,
    hasErrors: hasValidationErrors,
    clearValidationErrors,
    handleValidationError,
    getFieldErrors,
    hasFieldError
  } = useFormErrorHandler();
  
  // Load initial data
  useEffect(() => {
    loadInitialData();
  }, []);
  
  const loadInitialData = async () => {
    await executeWithErrorHandling(async () => {
      const [purchasesResult, salesResult] = await Promise.all([
        contractMatchingApi.getAvailablePurchases(),
        contractMatchingApi.getUnmatchedSales()
      ]);
      
      if (purchasesResult.success && purchasesResult.data) {
        setAvailablePurchases(purchasesResult.data);
      } else if (!purchasesResult.success && purchasesResult.error) {
        throw purchasesResult.error;
      }
      
      if (salesResult.success && salesResult.data) {
        setUnmatchedSales(salesResult.data);
      } else if (!salesResult.success && salesResult.error) {
        throw salesResult.error;
      }
    }, 'Failed to load contract data');
  };
  
  const handlePurchaseChange = (purchaseId: string) => {
    const purchase = availablePurchases.find(p => p.id === purchaseId);
    setSelectedPurchase(purchase || null);
    setFormData(prev => ({
      ...prev,
      purchaseContractId: purchaseId,
      salesContractId: '', // Reset sales selection
      quantity: 0 // Reset quantity
    }));
    setSelectedSales(null);
    clearValidationErrors();
  };
  
  const handleSalesChange = (salesId: string) => {
    const sales = unmatchedSales.find(s => s.id === salesId);
    setSelectedSales(sales || null);
    setFormData(prev => ({
      ...prev,
      salesContractId: salesId,
      quantity: 0 // Reset quantity when changing sales contract
    }));
    clearValidationErrors();
  };
  
  const handleQuantityChange = (quantity: number) => {
    setFormData(prev => ({ ...prev, quantity }));
    clearValidationErrors();
  };
  
  const getMaxQuantity = (): number => {
    if (!selectedPurchase || !selectedSales) return 0;
    return Math.min(selectedPurchase.availableQuantity, selectedSales.contractQuantity);
  };
  
  const getFilteredSales = (): UnmatchedSales[] => {
    if (!selectedPurchase) return unmatchedSales;
    // Only show sales contracts for the same product
    return unmatchedSales.filter(s => s.productName === selectedPurchase.productName);
  };
  
  const validateForm = (): boolean => {
    const errors: Record<string, string[]> = {};
    
    if (!formData.purchaseContractId) {
      errors.purchaseContractId = ['Please select a purchase contract'];
    }
    
    if (!formData.salesContractId) {
      errors.salesContractId = ['Please select a sales contract'];
    }
    
    if (!formData.quantity || formData.quantity <= 0) {
      errors.quantity = ['Quantity must be greater than zero'];
    }
    
    if (formData.quantity > getMaxQuantity()) {
      errors.quantity = [
        `Quantity cannot exceed ${getMaxQuantity()} (maximum available for matching)`
      ];
    }
    
    if (selectedPurchase && selectedSales && selectedPurchase.productName !== selectedSales.productName) {
      errors.compatibility = ['Purchase and sales contracts must be for the same product'];
    }
    
    if (Object.keys(errors).length > 0) {
      handleValidationError({
        code: ErrorCodes.ValidationFailed,
        message: 'Please correct the validation errors',
        timestamp: new Date().toISOString(),
        traceId: '',
        statusCode: 400,
        validationErrors: errors
      });
      return false;
    }
    
    return true;
  };
  
  const handleSubmit = async () => {
    if (!validateForm()) return;
    
    const result = await executeSubmit(async () => {
      const result = await contractMatchingApi.createMatching(formData);
      
      if (!result.success) {
        if (result.error?.validationErrors) {
          handleValidationError(result.error);
        }
        throw result.error;
      }
      
      return result.data;
    }, 'Failed to create contract matching');
    
    if (result) {
      // Success - reset form and notify parent
      setFormData({
        purchaseContractId: '',
        salesContractId: '',
        quantity: 0,
        notes: '',
        matchedBy: ''
      });
      setSelectedPurchase(null);
      setSelectedSales(null);
      clearValidationErrors();
      
      // Reload data to reflect changes
      await loadInitialData();
      
      if (onMatchingCreated) {
        onMatchingCreated();
      }
    }
  };
  
  if (loadError) {
    return (
      <Card>
        <CardContent>
          <ErrorDisplay
            error={loadError}
            onRetry={loadInitialData}
            showTitle={true}
          />
        </CardContent>
      </Card>
    );
  }
  
  return (
    <Card>
      <CardHeader
        title="Create Contract Matching"
        subheader="Match purchase contracts with sales contracts for natural hedging"
        avatar={<LinkIcon color="primary" />}
      />
      
      <CardContent>
        <Stack spacing={3}>
          {hasValidationErrors && (
            <ValidationErrorDisplay
              errors={validationErrors}
              onDismiss={clearValidationErrors}
            />
          )}
          
          {submitError && (
            <ErrorDisplay
              error={submitError}
              onRetry={hasValidationErrors ? undefined : handleSubmit}
              compact={true}
            />
          )}
          
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <FormControl fullWidth error={hasFieldError('purchaseContractId')}>
                <InputLabel>Purchase Contract</InputLabel>
                <Select
                  value={formData.purchaseContractId}
                  onChange={(e) => handlePurchaseChange(e.target.value)}
                  disabled={isLoadingData || isSubmitting}
                >
                  {availablePurchases.map((purchase) => (
                    <MenuItem key={purchase.id} value={purchase.id}>
                      <Box>
                        <Typography variant="body2">
                          {purchase.contractNumber} - {purchase.tradingPartnerName}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          {purchase.productName} • Available: {purchase.availableQuantity.toLocaleString()}
                        </Typography>
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
                {hasFieldError('purchaseContractId') && (
                  <Typography variant="caption" color="error">
                    {getFieldErrors('purchaseContractId').join(', ')}
                  </Typography>
                )}
              </FormControl>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <FormControl fullWidth error={hasFieldError('salesContractId')}>
                <InputLabel>Sales Contract</InputLabel>
                <Select
                  value={formData.salesContractId}
                  onChange={(e) => handleSalesChange(e.target.value)}
                  disabled={isLoadingData || isSubmitting || !selectedPurchase}
                >
                  {getFilteredSales().map((sales) => (
                    <MenuItem key={sales.id} value={sales.id}>
                      <Box>
                        <Typography variant="body2">
                          {sales.contractNumber} - {sales.tradingPartnerName}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          {sales.productName} • Quantity: {sales.contractQuantity.toLocaleString()}
                        </Typography>
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
                {hasFieldError('salesContractId') && (
                  <Typography variant="caption" color="error">
                    {getFieldErrors('salesContractId').join(', ')}
                  </Typography>
                )}
              </FormControl>
            </Grid>
            
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Quantity to Match"
                type="number"
                value={formData.quantity || ''}
                onChange={(e) => handleQuantityChange(Number(e.target.value))}
                error={hasFieldError('quantity')}
                helperText={
                  hasFieldError('quantity') 
                    ? getFieldErrors('quantity').join(', ')
                    : selectedPurchase && selectedSales 
                      ? `Max: ${getMaxQuantity().toLocaleString()}`
                      : 'Select contracts first'
                }
                disabled={isSubmitting || !selectedPurchase || !selectedSales}
                inputProps={{ min: 0, max: getMaxQuantity() }}
              />
            </Grid>
            
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Matched By"
                value={formData.matchedBy}
                onChange={(e) => setFormData(prev => ({ ...prev, matchedBy: e.target.value }))}
                disabled={isSubmitting}
                placeholder="Enter your name"
              />
            </Grid>
            
            <Grid item xs={12} md={4}>
              {selectedPurchase && selectedSales && (
                <Box>
                  <Typography variant="caption" display="block">
                    Product Compatibility:
                  </Typography>
                  <Chip
                    label={selectedPurchase.productName === selectedSales.productName ? 'Compatible' : 'Incompatible'}
                    color={selectedPurchase.productName === selectedSales.productName ? 'success' : 'error'}
                    size="small"
                  />
                </Box>
              )}
            </Grid>
            
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Notes (Optional)"
                multiline
                rows={3}
                value={formData.notes}
                onChange={(e) => setFormData(prev => ({ ...prev, notes: e.target.value }))}
                disabled={isSubmitting}
                placeholder="Add any additional notes about this matching..."
              />
            </Grid>
          </Grid>
          
          <Box display="flex" gap={2} justifyContent="flex-end">
            {onCancel && (
              <Button
                variant="outlined"
                onClick={onCancel}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
            )}
            
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={loadInitialData}
              disabled={isLoadingData || isSubmitting}
            >
              {isLoadingData ? <CircularProgress size={20} /> : 'Refresh Data'}
            </Button>
            
            <Button
              variant="contained"
              startIcon={isSubmitting ? <CircularProgress size={20} /> : <SaveIcon />}
              onClick={handleSubmit}
              disabled={isSubmitting || !formData.purchaseContractId || !formData.salesContractId || !formData.quantity}
            >
              {isSubmitting ? 'Creating...' : 'Create Matching'}
            </Button>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default ContractMatchingForm;
