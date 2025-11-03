import React from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  CircularProgress,
  Box,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  Stack,
  Divider,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import VisibilityIcon from '@mui/icons-material/Visibility';
import settlementApi, { Settlement } from '../../services/settlementsApi';

export interface SettlementsListProps {
  contractId: string;
  contractType: 'purchase' | 'sales';
  onViewSettlement?: (settlement: Settlement) => void;
}

/**
 * Settlements List Component
 * Displays all settlements for a contract
 */
export const SettlementsList: React.FC<SettlementsListProps> = ({
  contractId,
  contractType,
  onViewSettlement,
}) => {
  const [selectedSettlement, setSelectedSettlement] = React.useState<Settlement | null>(null);

  const { data: settlements, isLoading, error } = useQuery({
    queryKey: ['settlements', contractId, contractType],
    queryFn: async () => {
      if (contractType === 'purchase') {
        return settlementApi.getPurchaseSettlementsByContract(contractId);
      } else {
        return settlementApi.getSalesSettlementsByContract(contractId);
      }
    },
  });

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Draft':
        return 'default';
      case 'Calculated':
        return 'info';
      case 'Approved':
        return 'warning';
      case 'Finalized':
        return 'success';
      default:
        return 'default';
    }
  };

  if (isLoading) {
    return <CircularProgress />;
  }

  if (error) {
    return (
      <Typography color="error">
        Failed to load settlements: {error instanceof Error ? error.message : 'Unknown error'}
      </Typography>
    );
  }

  if (!settlements || settlements.length === 0) {
    return (
      <Box sx={{ p: 2, bgcolor: '#f5f5f5', borderRadius: 1 }}>
        <Typography variant="body2" color="textSecondary">
          No settlements found for this contract
        </Typography>
      </Box>
    );
  }

  return (
    <>
      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: '#f5f5f5' }}>
              <TableCell sx={{ fontWeight: 'bold' }}>Settlement #</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Status</TableCell>
              <TableCell align="right" sx={{ fontWeight: 'bold' }}>
                Quantity (MT)
              </TableCell>
              <TableCell align="right" sx={{ fontWeight: 'bold' }}>
                Total Amount
              </TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Created</TableCell>
              <TableCell align="center" sx={{ fontWeight: 'bold' }}>
                Actions
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {settlements.map((settlement) => (
              <TableRow key={settlement.id} hover>
                <TableCell>{settlement.settlementNumber}</TableCell>
                <TableCell>
                  <Chip
                    label={settlement.status}
                    size="small"
                    color={getStatusColor(settlement.status) as any}
                    variant="outlined"
                  />
                </TableCell>
                <TableCell align="right">
                  {settlement.calculationQuantityMT?.toFixed(2) || '0.00'}
                </TableCell>
                <TableCell align="right">
                  {settlement.currency} {settlement.totalAmount?.toFixed(2) || '0.00'}
                </TableCell>
                <TableCell>
                  {new Date(settlement.createdDate).toLocaleDateString()}
                </TableCell>
                <TableCell align="center">
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<VisibilityIcon />}
                    onClick={() => {
                      setSelectedSettlement(settlement);
                      onViewSettlement?.(settlement);
                    }}
                  >
                    View
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {selectedSettlement && (
        <Dialog open={true} onClose={() => setSelectedSettlement(null)} maxWidth="sm" fullWidth>
          <DialogTitle>Settlement Details</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 2 }}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Settlement Number
                </Typography>
                <Typography variant="body1">{selectedSettlement.settlementNumber}</Typography>
              </Box>

              <Divider />

              <Box>
                <Typography variant="caption" color="textSecondary">
                  Status
                </Typography>
                <Chip
                  label={selectedSettlement.status}
                  color={getStatusColor(selectedSettlement.status) as any}
                  size="small"
                  sx={{ mt: 0.5 }}
                />
              </Box>

              <Divider />

              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Quantity (MT)
                  </Typography>
                  <Typography variant="body2">
                    {selectedSettlement.calculationQuantityMT?.toFixed(2) || '0.00'}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Quantity (BBL)
                  </Typography>
                  <Typography variant="body2">
                    {selectedSettlement.calculationQuantityBBL?.toFixed(2) || '0.00'}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Benchmark Amount
                  </Typography>
                  <Typography variant="body2">
                    {selectedSettlement.benchmarkAmount?.toFixed(2) || '0.00'}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Adjustment Amount
                  </Typography>
                  <Typography variant="body2">
                    {selectedSettlement.adjustmentAmount?.toFixed(2) || '0.00'}
                  </Typography>
                </Box>
              </Box>

              <Divider />

              <Box>
                <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                  Total Amount
                </Typography>
                <Typography variant="h6" color="primary">
                  {selectedSettlement.currency} {selectedSettlement.totalAmount?.toFixed(2) || '0.00'}
                </Typography>
              </Box>

              <Divider />

              <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Created By
                  </Typography>
                  <Typography variant="body2">{selectedSettlement.createdBy}</Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="textSecondary">
                    Created Date
                  </Typography>
                  <Typography variant="body2">
                    {new Date(selectedSettlement.createdDate).toLocaleString()}
                  </Typography>
                </Box>

                {selectedSettlement.approvedBy && (
                  <>
                    <Box>
                      <Typography variant="caption" color="textSecondary">
                        Approved By
                      </Typography>
                      <Typography variant="body2">{selectedSettlement.approvedBy}</Typography>
                    </Box>

                    <Box>
                      <Typography variant="caption" color="textSecondary">
                        Approved Date
                      </Typography>
                      <Typography variant="body2">
                        {new Date(selectedSettlement.approvedDate!).toLocaleString()}
                      </Typography>
                    </Box>
                  </>
                )}

                {selectedSettlement.finalizedBy && (
                  <>
                    <Box>
                      <Typography variant="caption" color="textSecondary">
                        Finalized By
                      </Typography>
                      <Typography variant="body2">{selectedSettlement.finalizedBy}</Typography>
                    </Box>

                    <Box>
                      <Typography variant="caption" color="textSecondary">
                        Finalized Date
                      </Typography>
                      <Typography variant="body2">
                        {new Date(selectedSettlement.finalizedDate!).toLocaleString()}
                      </Typography>
                    </Box>
                  </>
                )}
              </Box>
            </Stack>
          </DialogContent>
        </Dialog>
      )}
    </>
  );
};

export default SettlementsList;
