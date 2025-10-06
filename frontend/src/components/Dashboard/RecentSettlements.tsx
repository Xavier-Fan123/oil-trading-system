import React, { useState, useEffect } from 'react';
import {
  Card,
  CardHeader,
  CardContent,
  Typography,
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  IconButton,
  CircularProgress,
  Alert,
  Button
} from '@mui/material';
import {
  ArrowForward as ArrowForwardIcon,
  Visibility as ViewIcon,
  Assignment as SettlementIcon
} from '@mui/icons-material';
import { ContractSettlementListDto, SettlementSearchFilters } from '@/types/settlement';
import { useSettlementSearch } from '@/hooks/useSettlements';
import { useNavigate } from 'react-router-dom';

interface RecentSettlementsProps {
  height?: number;
}

export const RecentSettlements: React.FC<RecentSettlementsProps> = ({ height = 400 }) => {
  const navigate = useNavigate();
  const { searchWithFilters, searchResults: _searchResults, loading, error } = useSettlementSearch();
  const [settlements, setSettlements] = useState<ContractSettlementListDto[]>([]);

  useEffect(() => {
    loadRecentSettlements();
  }, []);

  const loadRecentSettlements = async () => {
    const filters: SettlementSearchFilters = {
      pageNumber: 1,
      pageSize: 5, // Show only recent 5 settlements
    };

    try {
      const results = await searchWithFilters(filters);
      setSettlements(results || []);
    } catch (err) {
      console.error('Error loading recent settlements:', err);
      setSettlements([]);
    }
  };

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatDate = (date: Date | string) => {
    const d = new Date(date);
    return d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getStatusColor = (status: string): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    const statusLower = status.toLowerCase();
    if (statusLower.includes('finalized')) return 'success';
    if (statusLower.includes('approved')) return 'info';
    if (statusLower.includes('calculated')) return 'primary';
    if (statusLower.includes('draft')) return 'warning';
    return 'default';
  };

  const handleViewSettlement = (settlementId: string) => {
    navigate(`/settlements?id=${settlementId}`);
  };

  const handleViewAll = () => {
    navigate('/settlements');
  };

  if (loading) {
    return (
      <Card sx={{ height }}>
        <CardHeader title="Recent Settlements" />
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 200 }}>
            <CircularProgress />
          </Box>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card sx={{ height }}>
        <CardHeader title="Recent Settlements" />
        <CardContent>
          <Alert severity="error">{error}</Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ height, display: 'flex', flexDirection: 'column' }}>
      <CardHeader 
        title="Recent Settlements" 
        subheader="Latest settlement activities"
        action={
          <Button
            size="small"
            onClick={handleViewAll}
            endIcon={<ArrowForwardIcon />}
          >
            View All
          </Button>
        }
      />
      <CardContent sx={{ flexGrow: 1, overflow: 'hidden' }}>
        {settlements.length === 0 ? (
          <Box sx={{ 
            display: 'flex', 
            flexDirection: 'column', 
            alignItems: 'center', 
            justifyContent: 'center', 
            minHeight: 200,
            textAlign: 'center'
          }}>
            <SettlementIcon sx={{ fontSize: 48, color: 'grey.300', mb: 2 }} />
            <Typography variant="h6" color="text.secondary" gutterBottom>
              No Settlements Yet
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Settlements will appear here once contracts are settled
            </Typography>
          </Box>
        ) : (
          <TableContainer sx={{ height: '100%' }}>
            <Table stickyHeader>
              <TableHead>
                <TableRow>
                  <TableCell>Contract</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Amount</TableCell>
                  <TableCell align="right">Date</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {settlements.map((settlement) => (
                  <TableRow key={settlement.id} hover>
                    <TableCell>
                      <Box>
                        <Typography variant="body2" fontWeight="medium">
                          {settlement.contractNumber}
                        </Typography>
                        {settlement.externalContractNumber && (
                          <Typography variant="caption" color="text.secondary">
                            {settlement.externalContractNumber}
                          </Typography>
                        )}
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={settlement.status || 'Draft'}
                        size="small"
                        color={getStatusColor(settlement.status || 'draft')}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Typography variant="body2" fontWeight="medium">
                        {formatCurrency(settlement.totalSettlementAmount, settlement.settlementCurrency)}
                      </Typography>
                    </TableCell>
                    <TableCell align="right">
                      <Typography variant="caption" color="text.secondary">
                        {formatDate(settlement.createdDate)}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">
                      <IconButton 
                        size="small" 
                        onClick={() => handleViewSettlement(settlement.id)}
                        title="View Settlement"
                      >
                        <ViewIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>
    </Card>
  );
};

export default RecentSettlements;