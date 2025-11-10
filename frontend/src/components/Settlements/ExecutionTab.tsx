import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Chip,
  Alert,
  Divider,
} from '@mui/material';
import { ContractSettlementDto, ContractSettlementStatus, ContractSettlementStatusLabels } from '@/types/settlement';
import { format } from 'date-fns';

interface ExecutionTabProps {
  settlement: ContractSettlementDto;
}

const StatusColors: Record<ContractSettlementStatus, 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning'> = {
  [ContractSettlementStatus.Draft]: 'default',
  [ContractSettlementStatus.DataEntered]: 'info',
  [ContractSettlementStatus.Calculated]: 'primary',
  [ContractSettlementStatus.Reviewed]: 'secondary',
  [ContractSettlementStatus.Approved]: 'warning',
  [ContractSettlementStatus.Finalized]: 'success',
  [ContractSettlementStatus.Cancelled]: 'error'
};

const getStatusDescription = (status: ContractSettlementStatus): string => {
  switch (status) {
    case ContractSettlementStatus.Draft:
      return 'Settlement is in draft status, no data entered yet';
    case ContractSettlementStatus.DataEntered:
      return 'Quantity and document information has been entered';
    case ContractSettlementStatus.Calculated:
      return 'Amounts have been calculated and pricing finalized';
    case ContractSettlementStatus.Reviewed:
      return 'Settlement has been reviewed and is ready for approval';
    case ContractSettlementStatus.Approved:
      return 'Settlement has been approved and is awaiting finalization';
    case ContractSettlementStatus.Finalized:
      return 'Settlement is finalized and locked for processing';
    case ContractSettlementStatus.Cancelled:
      return 'Settlement has been cancelled';
    default:
      return 'Unknown status';
  }
};

export const ExecutionTab: React.FC<ExecutionTabProps> = ({ settlement }) => {
  // Parse current status
  const currentStatus = (Object.values(ContractSettlementStatus).find(
    s => ContractSettlementStatusLabels[s as ContractSettlementStatus] === settlement.displayStatus
  ) as ContractSettlementStatus) || ContractSettlementStatus.Draft;

  const statusSequence: ContractSettlementStatus[] = [
    ContractSettlementStatus.Draft,
    ContractSettlementStatus.DataEntered,
    ContractSettlementStatus.Calculated,
    ContractSettlementStatus.Reviewed,
    ContractSettlementStatus.Approved,
    ContractSettlementStatus.Finalized
  ];

  const currentStatusIndex = statusSequence.indexOf(currentStatus);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {/* Current Status */}
      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">Settlement Execution Status</Typography>
            <Chip
              label={settlement.displayStatus}
              color={StatusColors[currentStatus]}
              variant="filled"
              size="medium"
              icon={settlement.isFinalized ? undefined : undefined}
            />
          </Box>

          <Typography variant="body2" sx={{ mt: 2, mb: 2 }}>
            {getStatusDescription(currentStatus)}
          </Typography>

          {settlement.isFinalized && (
            <Alert severity="success">
              <Typography variant="body2">
                This settlement has been finalized and is locked from further modifications.
              </Typography>
            </Alert>
          )}
        </CardContent>
      </Card>

      {/* Execution Progress */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Settlement Workflow Progress
          </Typography>

          <Box sx={{ mt: 3 }}>
            {statusSequence.map((status, index) => (
              <Box key={status} sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Box
                  sx={{
                    width: 40,
                    height: 40,
                    borderRadius: '50%',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    backgroundColor: index <= currentStatusIndex ? '#4caf50' : '#e0e0e0',
                    color: 'white',
                    fontWeight: 'bold',
                    mr: 2
                  }}
                >
                  {index + 1}
                </Box>
                <Box sx={{ flex: 1 }}>
                  <Typography
                    variant="body1"
                    sx={{
                      fontWeight: index === currentStatusIndex ? 'bold' : 'normal',
                      color: index <= currentStatusIndex ? '#4caf50' : '#9e9e9e'
                    }}
                  >
                    {ContractSettlementStatusLabels[status]}
                  </Typography>
                </Box>
                {index <= currentStatusIndex && (
                  <Chip
                    label={index < currentStatusIndex ? 'Completed' : 'Current'}
                    size="small"
                    color={index < currentStatusIndex ? 'success' : 'primary'}
                    variant={index < currentStatusIndex ? 'filled' : 'outlined'}
                  />
                )}
              </Box>
            ))}
          </Box>
        </CardContent>
      </Card>

      {/* Key Milestones */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Settlement Milestones
          </Typography>
          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="body2" color="text.secondary">
                    Created On
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 'bold', mt: 1 }}>
                    {format(new Date(settlement.createdDate), 'PPP')}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {format(new Date(settlement.createdDate), 'p')}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                    by {settlement.createdBy}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            {settlement.lastModifiedDate && (
              <Grid item xs={12} md={4}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography variant="body2" color="text.secondary">
                      Last Modified On
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', mt: 1 }}>
                      {format(new Date(settlement.lastModifiedDate), 'PPP')}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {format(new Date(settlement.lastModifiedDate), 'p')}
                    </Typography>
                    {settlement.lastModifiedBy && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                        by {settlement.lastModifiedBy}
                      </Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            )}

            {settlement.finalizedDate && (
              <Grid item xs={12} md={4}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography variant="body2" color="text.secondary">
                      Finalized On
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', mt: 1 }}>
                      {format(new Date(settlement.finalizedDate), 'PPP')}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {format(new Date(settlement.finalizedDate), 'p')}
                    </Typography>
                    {settlement.finalizedBy && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                        by {settlement.finalizedBy}
                      </Typography>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            )}
          </Grid>
        </CardContent>
      </Card>

      {/* Shipping Operations Connected to Settlement */}
      {settlement && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Related Logistics Information
            </Typography>
            <Alert severity="info">
              <Typography variant="body2">
                {settlement.purchaseContract ? (
                  <>
                    <strong>Purchase Contract:</strong> {settlement.purchaseContract.contractNumber}
                    <br />
                    <strong>Supplier:</strong> {settlement.purchaseContract.supplierName}
                    <br />
                    <strong>Product:</strong> {settlement.purchaseContract.productName}
                    <br />
                    <strong>Quantity:</strong> {settlement.purchaseContract.quantity.toLocaleString()} units
                    <br />
                    <strong>Laycan Period:</strong> {format(new Date(settlement.purchaseContract.laycanStart), 'MMM dd')} - {format(new Date(settlement.purchaseContract.laycanEnd), 'MMM dd, yyyy')}
                  </>
                ) : (
                  <>
                    <strong>Sales Contract:</strong> {settlement.contractNumber}
                    <br />
                    See the related shipping operations for detailed logistics information.
                  </>
                )}
              </Typography>
            </Alert>
          </CardContent>
        </Card>
      )}

      {/* Execution Notes */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Execution Notes
          </Typography>
          {settlement.quantityCalculationNote ? (
            <Typography variant="body2">
              {settlement.quantityCalculationNote}
            </Typography>
          ) : (
            <Typography variant="body2" color="text.secondary">
              No notes recorded for this settlement.
            </Typography>
          )}

          <Divider sx={{ my: 2 }} />

          <Alert severity="info" sx={{ mt: 2 }}>
            <Typography variant="body2">
              All settlement processing is recorded and audited. For any modifications or concerns,
              please contact the settlement administrator.
            </Typography>
          </Alert>
        </CardContent>
      </Card>
    </Box>
  );
};
