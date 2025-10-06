import React from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Chip,
  CircularProgress,
  Button,
} from '@mui/material';
import {
  Edit as EditIcon,
  ArrowBack as ArrowBackIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { usePurchaseContract } from '@/hooks/useContracts';
import { ContractTagSelector } from '@/components/Tags/ContractTagSelector';
import {
  ContractStatus,
  QuantityUnit,
  PricingType,
  DeliveryTerms,
  SettlementType,
} from '@/types/contracts';

interface ContractDetailProps {
  contractId: string;
  onEdit: () => void;
  onBack: () => void;
}

const getStatusColor = (status: ContractStatus): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (status) {
    case ContractStatus.Draft:
      return 'default';
    case ContractStatus.PendingApproval:
      return 'warning';
    case ContractStatus.Active:
      return 'success';
    case ContractStatus.Completed:
      return 'info';
    case ContractStatus.Cancelled:
      return 'error';
    default:
      return 'default';
  }
};

const getStatusLabel = (status: ContractStatus): string => {
  switch (status) {
    case ContractStatus.Draft:
      return 'Draft';
    case ContractStatus.PendingApproval:
      return 'Pending Approval';
    case ContractStatus.Active:
      return 'Active';
    case ContractStatus.Completed:
      return 'Completed';
    case ContractStatus.Cancelled:
      return 'Cancelled';
    default:
      return 'Unknown';
  }
};

const getQuantityUnitLabel = (unit: QuantityUnit): string => {
  switch (unit) {
    case QuantityUnit.MT:
      return 'MT';
    case QuantityUnit.BBL:
      return 'BBL';
    case QuantityUnit.GAL:
      return 'GAL';
    default:
      return 'Unknown';
  }
};

const getPricingTypeLabel = (type: PricingType): string => {
  switch (type) {
    case PricingType.Fixed:
      return 'Fixed Price';
    case PricingType.Floating:
      return 'Floating Price';
    case PricingType.Formula:
      return 'Formula-based';
    default:
      return 'Unknown';
  }
};

const getDeliveryTermsLabel = (terms: DeliveryTerms): string => {
  switch (terms) {
    case DeliveryTerms.FOB:
      return 'FOB (Free on Board)';
    case DeliveryTerms.CIF:
      return 'CIF (Cost, Insurance & Freight)';
    case DeliveryTerms.CFR:
      return 'CFR (Cost and Freight)';
    case DeliveryTerms.DAP:
      return 'DAP (Delivered at Place)';
    case DeliveryTerms.DDP:
      return 'DDP (Delivered Duty Paid)';
    default:
      return 'Unknown';
  }
};

const getSettlementTypeLabel = (type: SettlementType): string => {
  switch (type) {
    case SettlementType.TT:
      return 'TT (Telegraphic Transfer)';
    case SettlementType.LC:
      return 'LC (Letter of Credit)';
    case SettlementType.CAD:
      return 'CAD (Cash Against Documents)';
    default:
      return 'Unknown';
  }
};

export const ContractDetail: React.FC<ContractDetailProps> = ({
  contractId,
  onEdit,
  onBack,
}) => {
  const { data: contract, isLoading, error } = usePurchaseContract(contractId);

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (error || !contract) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <Typography color="error">Error loading contract details</Typography>
      </Box>
    );
  }

  const canEdit = contract.status === ContractStatus.Draft || contract.status === ContractStatus.PendingApproval;

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Box display="flex" alignItems="center">
          <Button 
            startIcon={<ArrowBackIcon />} 
            onClick={onBack}
            sx={{ mr: 2 }}
          >
            Back to List
          </Button>
          <Typography variant="h4" component="h1">
            Contract Details
          </Typography>
        </Box>
        <Box display="flex" alignItems="center" gap={2}>
          <Chip
            label={getStatusLabel(contract.status)}
            color={getStatusColor(contract.status)}
            size="medium"
          />
          {canEdit && (
            <Button
              variant="contained"
              startIcon={<EditIcon />}
              onClick={onEdit}
            >
              Edit Contract
            </Button>
          )}
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* Contract Information */}
        <Grid item xs={12} md={8}>
          <Grid container spacing={3}>
            {/* Basic Information */}
            <Grid item xs={12}>
              <Card>
                <CardHeader title="Basic Information" />
                <CardContent>
                  <Grid container spacing={2}>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        System Contract Number
                      </Typography>
                      <Typography variant="body1" fontWeight="medium">
                        {contract.contractNumber.value}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        External Contract Number
                      </Typography>
                      <Typography variant="body1">
                        {contract.externalContractNumber || '—'}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Supplier
                      </Typography>
                      <Typography variant="body1">
                        {contract.supplier.name} ({contract.supplier.code})
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Product
                      </Typography>
                      <Typography variant="body1">
                        {contract.product.name} ({contract.product.code})
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Quantity
                      </Typography>
                      <Typography variant="body1">
                        {contract.quantity.toLocaleString()} {getQuantityUnitLabel(contract.quantityUnit)}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Created
                      </Typography>
                      <Typography variant="body1">
                        {format(new Date(contract.createdAt), 'MMM dd, yyyy HH:mm')}
                      </Typography>
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
                      <Typography variant="subtitle2" color="text.secondary">
                        Pricing Type
                      </Typography>
                      <Typography variant="body1">
                        {getPricingTypeLabel(contract.pricingType)}
                      </Typography>
                    </Grid>
                    {contract.fixedPrice && (
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Fixed Price
                        </Typography>
                        <Typography variant="body1">
                          ${contract.fixedPrice.toLocaleString()} USD
                        </Typography>
                      </Grid>
                    )}
                    {contract.pricingFormula && (
                      <Grid item xs={12}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Pricing Formula
                        </Typography>
                        <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                          {contract.pricingFormula}
                        </Typography>
                      </Grid>
                    )}
                    {contract.pricingPeriodStart && contract.pricingPeriodEnd && (
                      <>
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Pricing Period Start
                          </Typography>
                          <Typography variant="body1">
                            {format(new Date(contract.pricingPeriodStart), 'MMM dd, yyyy')}
                          </Typography>
                        </Grid>
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Pricing Period End
                          </Typography>
                          <Typography variant="body1">
                            {format(new Date(contract.pricingPeriodEnd), 'MMM dd, yyyy')}
                          </Typography>
                        </Grid>
                      </>
                    )}
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
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Delivery Terms
                      </Typography>
                      <Typography variant="body1">
                        {getDeliveryTermsLabel(contract.deliveryTerms)}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Load Port
                      </Typography>
                      <Typography variant="body1">
                        {contract.loadPort}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Discharge Port
                      </Typography>
                      <Typography variant="body1">
                        {contract.dischargePort}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Laycan Period
                      </Typography>
                      <Typography variant="body1">
                        {format(new Date(contract.laycanStart), 'MMM dd')} - {format(new Date(contract.laycanEnd), 'MMM dd, yyyy')}
                      </Typography>
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
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Settlement Type
                      </Typography>
                      <Typography variant="body1">
                        {getSettlementTypeLabel(contract.settlementType)}
                      </Typography>
                    </Grid>
                    <Grid item xs={12} sm={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Credit Period
                      </Typography>
                      <Typography variant="body1">
                        {contract.creditPeriodDays} days
                      </Typography>
                    </Grid>
                    {contract.prepaymentPercentage > 0 && (
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Prepayment
                        </Typography>
                        <Typography variant="body1">
                          {contract.prepaymentPercentage}%
                        </Typography>
                      </Grid>
                    )}
                    {contract.paymentTerms && (
                      <Grid item xs={12}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Payment Terms
                        </Typography>
                        <Typography variant="body1">
                          {contract.paymentTerms}
                        </Typography>
                      </Grid>
                    )}
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            {/* Additional Information */}
            {(contract.qualitySpecifications || contract.inspectionAgency || contract.notes) && (
              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Additional Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      {contract.inspectionAgency && (
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Inspection Agency
                          </Typography>
                          <Typography variant="body1">
                            {contract.inspectionAgency}
                          </Typography>
                        </Grid>
                      )}
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Ton/Barrel Ratio
                        </Typography>
                        <Typography variant="body1">
                          {contract.tonBarrelRatio}
                        </Typography>
                      </Grid>
                      {contract.qualitySpecifications && (
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Quality Specifications
                          </Typography>
                          <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
                            {contract.qualitySpecifications}
                          </Typography>
                        </Grid>
                      )}
                      {contract.notes && (
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Notes
                          </Typography>
                          <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
                            {contract.notes}
                          </Typography>
                        </Grid>
                      )}
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>
            )}
          </Grid>
        </Grid>

        {/* Tags Section */}
        <Grid item xs={12} md={4}>
          <ContractTagSelector
            contractId={contractId}
            contractType="PurchaseContract"
          />
        </Grid>
      </Grid>
    </Box>
  );
};