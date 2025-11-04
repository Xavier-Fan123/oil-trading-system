import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Typography,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { settlementHistoryApi, SettlementHistoryDto } from '@/services/settlementApi';

interface SettlementHistoryTabProps {
  settlementId: string;
}

/**
 * Settlement History Tab Component
 * Displays timeline of settlement creation, calculation, approval, and finalization
 */
export const SettlementHistoryTab: React.FC<SettlementHistoryTabProps> = ({ settlementId }) => {
  const {
    data: history,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['settlementHistory', settlementId],
    queryFn: () => settlementHistoryApi.getHistory(settlementId),
    enabled: !!settlementId,
  });

  const getActionColor = (action: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    switch (action) {
      case 'Created':
        return 'info';
      case 'Calculated':
        return 'primary';
      case 'Reviewed':
        return 'secondary';
      case 'Approved':
        return 'warning';
      case 'Finalized':
        return 'success';
      case 'Cancelled':
        return 'error';
      case 'PaymentRecorded':
        return 'success';
      case 'StatusChanged':
        return 'info';
      default:
        return 'default';
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 2 }}>
        Failed to load settlement history: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  if (!history || history.length === 0) {
    return (
      <Alert severity="info">
        No settlement history available yet. Settlement actions will appear here.
      </Alert>
    );
  }

  return (
    <Card>
      <CardHeader
        title="Settlement History"
        subheader={`${history.length} event${history.length !== 1 ? 's' : ''}`}
      />
      <CardContent>
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow sx={{ backgroundColor: '#f5f5f5' }}>
                <TableCell>Date/Time</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Status Change</TableCell>
                <TableCell>Performed By</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {history.map((item, index) => (
                <TableRow key={index} sx={{ '&:hover': { backgroundColor: '#fafafa' } }}>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>
                    <Typography variant="body2">
                      {format(new Date(item.timestamp), 'MMM dd, yyyy')}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {format(new Date(item.timestamp), 'HH:mm:ss')}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={item.action}
                      size="small"
                      color={getActionColor(item.action)}
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{item.description}</Typography>
                  </TableCell>
                  <TableCell>
                    {item.previousStatus && item.newStatus ? (
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Chip label={item.previousStatus} size="small" variant="outlined" />
                        <Typography variant="caption">→</Typography>
                        <Chip label={item.newStatus} size="small" color="success" variant="outlined" />
                      </Box>
                    ) : (
                      <Typography variant="body2" color="textSecondary">
                        -
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{item.performedBy}</Typography>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </CardContent>
    </Card>
  );
};

export default SettlementHistoryTab;
