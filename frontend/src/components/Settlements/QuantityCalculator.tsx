import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  Divider,
  Tooltip,
  IconButton,
  InputAdornment
} from '@mui/material';
import {
  Info as InfoIcon,
  SwapHoriz as SwapIcon,
} from '@mui/icons-material';
import {
  QuantityUnit,
  CalculationMode,
  QuantityUnitLabels
} from '@/types/settlement';

interface QuantityData {
  actualQuantityMT: number;
  actualQuantityBBL: number;
  calculationQuantityMT: number;
  calculationQuantityBBL: number;
  calculationMode: CalculationMode;
  tonBarrelRatio: number;
  calculationNote: string;
}

interface QuantityCalculatorProps {
  initialData?: Partial<QuantityData>;
  contractQuantity?: number;
  contractUnit?: QuantityUnit | string;  // Accept both enum and string from API
  productDensity?: number; // kg/m³
  onChange: (data: QuantityData) => void;
  readOnly?: boolean;
}

export const QuantityCalculator: React.FC<QuantityCalculatorProps> = ({
  initialData,
  contractQuantity,
  contractUnit = QuantityUnit.MT,
  productDensity = 850, // Default crude oil density
  onChange,
  readOnly = false
}) => {
  const [data, setData] = useState<QuantityData>({
    actualQuantityMT: initialData?.actualQuantityMT || 0,
    actualQuantityBBL: initialData?.actualQuantityBBL || 0,
    calculationQuantityMT: initialData?.calculationQuantityMT || 0,
    calculationQuantityBBL: initialData?.calculationQuantityBBL || 0,
    calculationMode: initialData?.calculationMode || CalculationMode.UseActualQuantities,
    tonBarrelRatio: initialData?.tonBarrelRatio || 7.33, // Default ratio for Brent crude
    calculationNote: initialData?.calculationNote || ''
  });

  // Standard conversion factors
  const BARRELS_PER_CUBIC_METER = 6.2898;
  const TONNES_TO_KG = 1000;

  // Calculate ton-barrel ratio from density
  useEffect(() => {
    if (productDensity > 0) {
      // Calculate barrels per MT using density
      // 1 MT = 1000 kg
      // Volume (m³) = mass (kg) / density (kg/m³)
      // Barrels = Volume (m³) * 6.2898
      const volumePerTonne = TONNES_TO_KG / productDensity; // m³ per MT
      const barrelsPerTonne = volumePerTonne * BARRELS_PER_CUBIC_METER;
      
      setData(prev => ({
        ...prev,
        tonBarrelRatio: Number(barrelsPerTonne.toFixed(2))
      }));
    }
  }, [productDensity]);

  // Auto-calculate based on conversion ratio
  const handleMTChange = (mt: number) => {
    const bbl = mt * data.tonBarrelRatio;
    setData(prev => ({
      ...prev,
      actualQuantityMT: mt,
      actualQuantityBBL: Number(bbl.toFixed(2))
    }));
  };

  const handleBBLChange = (bbl: number) => {
    const mt = bbl / data.tonBarrelRatio;
    setData(prev => ({
      ...prev,
      actualQuantityMT: Number(mt.toFixed(2)),
      actualQuantityBBL: bbl
    }));
  };

  // Calculate calculation quantities based on mode
  const calculateQuantities = () => {
    let calcMT = data.actualQuantityMT;
    let calcBBL = data.actualQuantityBBL;
    let note = '';

    switch (data.calculationMode) {
      case CalculationMode.UseActualQuantities:
        calcMT = data.actualQuantityMT;
        calcBBL = data.actualQuantityBBL;
        note = 'Using actual B/L quantities for calculation';
        break;

      case CalculationMode.UseMTForAll:
        calcMT = data.actualQuantityMT;
        calcBBL = calcMT * data.tonBarrelRatio;
        note = `Using MT quantity converted to BBL (ratio: 1 MT = ${data.tonBarrelRatio} BBL)`;
        break;

      case CalculationMode.UseBBLForAll:
        calcBBL = data.actualQuantityBBL;
        calcMT = calcBBL / data.tonBarrelRatio;
        note = `Using BBL quantity converted to MT (ratio: ${data.tonBarrelRatio} BBL = 1 MT)`;
        break;

      case CalculationMode.UseContractSpecified:
        if (contractQuantity && contractUnit !== undefined) {
          if (contractUnit === QuantityUnit.MT) {
            calcMT = contractQuantity;
            calcBBL = contractQuantity * data.tonBarrelRatio;
            note = `Using contract quantity: ${contractQuantity} MT (converted to ${calcBBL.toFixed(2)} BBL)`;
          } else if (contractUnit === QuantityUnit.BBL) {
            calcBBL = contractQuantity;
            calcMT = contractQuantity / data.tonBarrelRatio;
            note = `Using contract quantity: ${contractQuantity} BBL (converted to ${calcMT.toFixed(2)} MT)`;
          }
        } else {
          calcMT = data.actualQuantityMT;
          calcBBL = data.actualQuantityBBL;
          note = 'Contract quantity not available, using actual quantities';
        }
        break;

      default:
        calcMT = data.actualQuantityMT;
        calcBBL = data.actualQuantityBBL;
        note = 'Default calculation using actual quantities';
    }

    const updatedData = {
      ...data,
      calculationQuantityMT: Number(calcMT.toFixed(2)),
      calculationQuantityBBL: Number(calcBBL.toFixed(2)),
      calculationNote: note
    };

    setData(updatedData);
    onChange(updatedData);
  };

  useEffect(() => {
    calculateQuantities();
  }, [data.calculationMode, data.actualQuantityMT, data.actualQuantityBBL, data.tonBarrelRatio]);

  const getDifferencePercent = (actual: number, calculated: number): number => {
    if (actual === 0) return 0;
    return ((calculated - actual) / actual) * 100;
  };

  const formatNumber = (num: number, decimals: number = 2): string => {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    }).format(num);
  };

  const handleSwapConversion = () => {
    // Convert current MT value to BBL using the ratio and vice versa
    const mtToBbl = data.actualQuantityMT * data.tonBarrelRatio;
    const bblToMt = data.actualQuantityBBL / data.tonBarrelRatio;
    
    // Use the more precise conversion
    if (Math.abs(data.actualQuantityBBL - mtToBbl) < Math.abs(data.actualQuantityMT - bblToMt)) {
      // MT is more accurate, convert to BBL
      handleMTChange(data.actualQuantityMT);
    } else {
      // BBL is more accurate, convert to MT
      handleBBLChange(data.actualQuantityBBL);
    }
  };

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          <Typography variant="h6">
            Quantity Calculator
          </Typography>
          <Tooltip title="Mixed-unit pricing calculation tool">
            <IconButton size="small">
              <InfoIcon />
            </IconButton>
          </Tooltip>
        </Box>

        {/* Conversion Settings */}
        <Box sx={{ mb: 3, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
          <Typography variant="subtitle2" gutterBottom>Conversion Settings</Typography>
          <Grid container spacing={2} alignItems="center">
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Ton-Barrel Ratio"
                type="number"
                value={data.tonBarrelRatio}
                onChange={(e) => setData(prev => ({ ...prev, tonBarrelRatio: parseFloat(e.target.value) || 7.33 }))}
                disabled={readOnly}
                helperText="Barrels per MT"
                InputProps={{
                  endAdornment: <InputAdornment position="end">BBL/MT</InputAdornment>
                }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              <TextField
                fullWidth
                label="Product Density"
                type="number"
                value={productDensity}
                disabled // This comes from contract/product data
                helperText="From product specification"
                InputProps={{
                  endAdornment: <InputAdornment position="end">kg/m³</InputAdornment>
                }}
              />
            </Grid>
            <Grid item xs={12} md={4}>
              {contractQuantity && contractUnit !== undefined && (
                <Box>
                  <Typography variant="body2" color="text.secondary">Contract Quantity</Typography>
                  <Typography variant="body1" fontWeight="medium">
                    {formatNumber(contractQuantity)} {typeof contractUnit === 'string' ? contractUnit : QuantityUnitLabels[contractUnit]}
                  </Typography>
                </Box>
              )}
            </Grid>
          </Grid>
        </Box>

        {/* Actual Quantities Input */}
        <Box sx={{ mb: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6">Actual Quantities (B/L)</Typography>
            {!readOnly && (
              <Tooltip title="Auto-sync MT and BBL using conversion ratio">
                <IconButton size="small" onClick={handleSwapConversion}>
                  <SwapIcon />
                </IconButton>
              </Tooltip>
            )}
          </Box>

          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <TextField
                fullWidth
                label="Metric Tons (MT)"
                type="number"
                value={data.actualQuantityMT}
                onChange={(e) => handleMTChange(parseFloat(e.target.value) || 0)}
                disabled={readOnly}
                inputProps={{ min: 0, step: 0.01 }}
                helperText={`≈ ${formatNumber(data.actualQuantityMT * data.tonBarrelRatio)} BBL`}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                fullWidth
                label="Barrels (BBL)"
                type="number"
                value={data.actualQuantityBBL}
                onChange={(e) => handleBBLChange(parseFloat(e.target.value) || 0)}
                disabled={readOnly}
                inputProps={{ min: 0, step: 0.01 }}
                helperText={`≈ ${formatNumber(data.actualQuantityBBL / data.tonBarrelRatio)} MT`}
              />
            </Grid>
          </Grid>
        </Box>

        <Divider sx={{ my: 3 }} />

        {/* Calculation Mode */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="h6" gutterBottom>Calculation Method</Typography>
          <FormControl fullWidth>
            <InputLabel>Calculation Mode</InputLabel>
            <Select
              value={data.calculationMode}
              label="Calculation Mode"
              onChange={(e) => setData(prev => ({ ...prev, calculationMode: e.target.value as CalculationMode }))}
              disabled={readOnly}
            >
              <MenuItem value={CalculationMode.UseActualQuantities}>
                Use Actual Quantities (Mixed Units)
              </MenuItem>
              <MenuItem value={CalculationMode.UseMTForAll}>
                Use MT for All Calculations
              </MenuItem>
              <MenuItem value={CalculationMode.UseBBLForAll}>
                Use BBL for All Calculations
              </MenuItem>
              {contractQuantity && contractUnit !== undefined && (
                <MenuItem value={CalculationMode.UseContractSpecified}>
                  Use Contract Specified Quantity
                </MenuItem>
              )}
            </Select>
          </FormControl>
        </Box>

        {/* Calculation Results */}
        <Box sx={{ p: 2, bgcolor: 'primary.50', borderRadius: 1 }}>
          <Typography variant="h6" gutterBottom>Calculation Quantities</Typography>
          
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Box>
                <Typography variant="body2" color="text.secondary">Calculation MT</Typography>
                <Typography variant="h6" color="primary.main">
                  {formatNumber(data.calculationQuantityMT)} MT
                </Typography>
                {data.calculationQuantityMT !== data.actualQuantityMT && (
                  <Typography variant="caption" color="text.secondary">
                    Diff: {getDifferencePercent(data.actualQuantityMT, data.calculationQuantityMT).toFixed(2)}%
                  </Typography>
                )}
              </Box>
            </Grid>
            <Grid item xs={12} md={6}>
              <Box>
                <Typography variant="body2" color="text.secondary">Calculation BBL</Typography>
                <Typography variant="h6" color="primary.main">
                  {formatNumber(data.calculationQuantityBBL)} BBL
                </Typography>
                {data.calculationQuantityBBL !== data.actualQuantityBBL && (
                  <Typography variant="caption" color="text.secondary">
                    Diff: {getDifferencePercent(data.actualQuantityBBL, data.calculationQuantityBBL).toFixed(2)}%
                  </Typography>
                )}
              </Box>
            </Grid>
          </Grid>

          {data.calculationNote && (
            <Alert severity="info" sx={{ mt: 2 }}>
              <Typography variant="body2">{data.calculationNote}</Typography>
            </Alert>
          )}
        </Box>

        {/* Variance Analysis */}
        {(data.calculationQuantityMT !== data.actualQuantityMT || data.calculationQuantityBBL !== data.actualQuantityBBL) && (
          <Box sx={{ mt: 2, p: 2, bgcolor: 'warning.50', borderRadius: 1 }}>
            <Typography variant="subtitle2" gutterBottom>Quantity Variance Analysis</Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} md={6}>
                <Typography variant="body2" color="text.secondary">MT Variance</Typography>
                <Typography variant="body2" color={data.calculationQuantityMT >= data.actualQuantityMT ? 'success.main' : 'error.main'}>
                  {data.calculationQuantityMT >= data.actualQuantityMT ? '+' : ''}{formatNumber(data.calculationQuantityMT - data.actualQuantityMT)} MT
                  ({getDifferencePercent(data.actualQuantityMT, data.calculationQuantityMT).toFixed(2)}%)
                </Typography>
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="body2" color="text.secondary">BBL Variance</Typography>
                <Typography variant="body2" color={data.calculationQuantityBBL >= data.actualQuantityBBL ? 'success.main' : 'error.main'}>
                  {data.calculationQuantityBBL >= data.actualQuantityBBL ? '+' : ''}{formatNumber(data.calculationQuantityBBL - data.actualQuantityBBL)} BBL
                  ({getDifferencePercent(data.actualQuantityBBL, data.calculationQuantityBBL).toFixed(2)}%)
                </Typography>
              </Grid>
            </Grid>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};