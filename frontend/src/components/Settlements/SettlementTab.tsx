import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Divider
} from '@mui/material';
import { format } from 'date-fns';
import { ContractSettlementDto,    } from '@/types/settlement';

interface SettlementTabProps {
  settlement: ContractSettlementDto;
}

export const SettlementTab: React.FC<SettlementTabProps> = ({ settlement }) => {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {/* Quantities Section */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Quantity Information
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="body2" color="text.secondary">
                Actual Quantities from Document
              </Typography>
              <Box sx={{ mt: 1 }}>
                <Typography variant="body1">
                  MT: <strong>{settlement.actualQuantityMT.toLocaleString()}</strong>
                </Typography>
                <Typography variant="body1">
                  BBL: <strong>{settlement.actualQuantityBBL.toLocaleString()}</strong>
                </Typography>
              </Box>
            </Grid>

            <Grid item xs={12} md={6}>
              <Typography variant="body2" color="text.secondary">
                Calculation Quantities
              </Typography>
              <Box sx={{ mt: 1 }}>
                <Typography variant="body1">
                  MT: <strong>{settlement.calculationQuantityMT.toLocaleString()}</strong>
                </Typography>
                <Typography variant="body1">
                  BBL: <strong>{settlement.calculationQuantityBBL.toLocaleString()}</strong>
                </Typography>
              </Box>
            </Grid>

            {settlement.quantityCalculationNote && (
              <Grid item xs={12}>
                <Divider sx={{ my: 1 }} />
                <Typography variant="body2" color="text.secondary">
                  Calculation Note
                </Typography>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  {settlement.quantityCalculationNote}
                </Typography>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {/* Pricing & Settlement Amount Section */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Pricing & Settlement Amount
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={3}>
              <Typography variant="body2" color="text.secondary">
                Benchmark Amount
              </Typography>
              <Typography variant="h6">
                ${settlement.benchmarkAmount.toFixed(2)}
              </Typography>
              {settlement.benchmarkPriceFormula && (
                <Typography variant="caption" color="text.secondary">
                  Formula: {settlement.benchmarkPriceFormula}
                </Typography>
              )}
            </Grid>

            <Grid item xs={12} md={3}>
              <Typography variant="body2" color="text.secondary">
                Adjustment Amount
              </Typography>
              <Typography variant="h6">
                ${settlement.adjustmentAmount.toFixed(2)}
              </Typography>
            </Grid>

            <Grid item xs={12} md={3}>
              <Typography variant="body2" color="text.secondary">
                Cargo Value (Subtotal)
              </Typography>
              <Typography variant="h6" sx={{ color: 'primary.main' }}>
                ${settlement.cargoValue.toFixed(2)}
              </Typography>
            </Grid>

            <Grid item xs={12} md={3}>
              <Typography variant="body2" color="text.secondary">
                Total Charges
              </Typography>
              <Typography variant="h6">
                ${settlement.totalCharges.toFixed(2)}
              </Typography>
            </Grid>

            <Grid item xs={12}>
              <Divider sx={{ my: 1 }} />
            </Grid>

            <Grid item xs={12} md={6}>
              <Typography variant="body2" color="text.secondary">
                Total Settlement Amount
              </Typography>
              <Typography variant="h5" sx={{ color: 'success.main', fontWeight: 'bold' }}>
                ${settlement.totalSettlementAmount.toFixed(2)} {settlement.settlementCurrency}
              </Typography>
            </Grid>

            {settlement.exchangeRate && (
              <Grid item xs={12} md={6}>
                <Typography variant="body2" color="text.secondary">
                  Exchange Rate
                </Typography>
                <Typography variant="body1">
                  {settlement.exchangeRate.toFixed(4)}
                </Typography>
                {settlement.exchangeRateNote && (
                  <Typography variant="caption" color="text.secondary">
                    {settlement.exchangeRateNote}
                  </Typography>
                )}
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {/* Charges Breakdown */}
      {settlement.charges && settlement.charges.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Settlement Charges
            </Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                    <TableCell><strong>Charge Type</strong></TableCell>
                    <TableCell><strong>Description</strong></TableCell>
                    <TableCell align="right"><strong>Amount</strong></TableCell>
                    <TableCell><strong>Currency</strong></TableCell>
                    <TableCell><strong>Incurred Date</strong></TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {settlement.charges.map((charge) => (
                    <TableRow key={charge.id}>
                      <TableCell>
                        <Chip
                          label={charge.chargeTypeDisplayName}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>{charge.description}</TableCell>
                      <TableCell align="right">
                        <Typography
                          variant="body2"
                          sx={{ fontWeight: 'bold', color: charge.isNegativeCharge ? 'error.main' : 'success.main' }}
                        >
                          {charge.isNegativeCharge ? '-' : '+'}${Math.abs(charge.amount).toFixed(2)}
                        </Typography>
                      </TableCell>
                      <TableCell>{charge.currency}</TableCell>
                      <TableCell>
                        {charge.incurredDate ? format(new Date(charge.incurredDate), 'MMM dd, yyyy') : 'N/A'}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {/* Audit Information */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Audit Information
          </Typography>
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Typography variant="body2" color="text.secondary">
                Created By
              </Typography>
              <Typography variant="body1">{settlement.createdBy}</Typography>
              <Typography variant="body2" color="text.secondary">
                {format(new Date(settlement.createdDate), 'PPpp')}
              </Typography>
            </Grid>

            {settlement.lastModifiedBy && (
              <Grid item xs={12} md={6}>
                <Typography variant="body2" color="text.secondary">
                  Last Modified By
                </Typography>
                <Typography variant="body1">{settlement.lastModifiedBy}</Typography>
                {settlement.lastModifiedDate && (
                  <Typography variant="body2" color="text.secondary">
                    {format(new Date(settlement.lastModifiedDate), 'PPpp')}
                  </Typography>
                )}
              </Grid>
            )}

            {settlement.finalizedBy && (
              <Grid item xs={12} md={6}>
                <Typography variant="body2" color="text.secondary">
                  Finalized By
                </Typography>
                <Typography variant="body1">{settlement.finalizedBy}</Typography>
                {settlement.finalizedDate && (
                  <Typography variant="body2" color="text.secondary">
                    {format(new Date(settlement.finalizedDate), 'PPpp')}
                  </Typography>
                )}
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>
    </Box>
  );
};
