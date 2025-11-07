import React, { useState, useEffect } from 'react';
import {
  Card,
  CardHeader,
  CardContent,
  Typography,
  Box,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Chip,
  CircularProgress,
  Alert,
  Button,
  Divider
} from '@mui/material';
import {
  Schedule as ScheduleIcon,
  Assignment as AssignmentIcon,
  ArrowForward as ArrowForwardIcon,
  Edit as EditIcon,
  PlayArrow as StartIcon
} from '@mui/icons-material';
import { ContractSettlementListDto, SettlementSearchFilters } from '@/types/settlement';
import { useSettlementSearch } from '@/hooks/useSettlements';
import { useNavigate } from 'react-router-dom';

interface PendingSettlementsProps {
  height?: number;
}

export const PendingSettlements: React.FC<PendingSettlementsProps> = ({ height = 400 }) => {
  const navigate = useNavigate();
  const { searchWithFilters, loading, error } = useSettlementSearch();
  const [pendingSettlements, setPendingSettlements] = useState<ContractSettlementListDto[]>([]);
  const [awaitingSettlements, setAwaitingSettlements] = useState<any[]>([]); // Contracts awaiting settlement

  useEffect(() => {
    loadPendingSettlements();
    loadContractsAwaitingSettlement();
  }, []);

  const loadPendingSettlements = async () => {
    const filters: SettlementSearchFilters = {
      pageNumber: 1,
      pageSize: 10,
      // statusFilter: 'draft,calculated,reviewed' // Filter for non-finalized settlements
    };

    try {
      const results = await searchWithFilters(filters);
      // Filter for non-finalized settlements
      const data = results || [];
      const pending = data.filter(s => !s.isFinalized);
      setPendingSettlements(pending);
    } catch (err) {
      console.error('Error loading pending settlements:', err);
      setPendingSettlements([]);
    }
  };

  const loadContractsAwaitingSettlement = async () => {
    // This would typically load from a contracts API endpoint
    // For now, we'll use mock data representing contracts that need settlement
    const mockAwaitingContracts = [
      {
        id: 'contract-1',
        contractNumber: 'PC-2024-003',
        externalContractNumber: 'EXT-003',
        supplierName: 'Nordic Oil Supply',
        productName: 'Brent Crude',
        quantity: 15000,
        quantityUnit: 'MT',
        laycanEnd: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), // 2 days ago
        urgencyLevel: 'high'
      },
      {
        id: 'contract-2',
        contractNumber: 'PC-2024-004',
        externalContractNumber: 'EXT-004',
        supplierName: 'Mediterranean Trading',
        productName: 'WTI Crude',
        quantity: 20000,
        quantityUnit: 'MT',
        laycanEnd: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000), // 1 day ago
        urgencyLevel: 'medium'
      }
    ];
    setAwaitingSettlements(mockAwaitingContracts);
  };

  const formatDate = (date: Date | string) => {
    const d = new Date(date);
    return d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  };

  const getUrgencyColor = (urgency: string): 'default' | 'error' | 'warning' | 'success' => {
    switch (urgency) {
      case 'high': return 'error';
      case 'medium': return 'warning';
      case 'low': return 'success';
      default: return 'default';
    }
  };

  const handleStartSettlement = (contractId: string) => {
    navigate(`/settlements?create=true&contractId=${contractId}`);
  };

  const handleEditSettlement = (settlementId: string) => {
    navigate(`/settlements?edit=${settlementId}`);
  };

  const handleViewAll = () => {
    navigate('/settlements');
  };

  if (loading) {
    return (
      <Card sx={{ height }}>
        <CardHeader title="Pending Settlements" />
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
        <CardHeader title="Pending Settlements" />
        <CardContent>
          <Alert severity="error">{error}</Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ height, display: 'flex', flexDirection: 'column' }}>
      <CardHeader 
        title="Pending Settlements" 
        subheader="Settlements requiring attention"
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
      <CardContent sx={{ flexGrow: 1, overflow: 'auto', p: 0 }}>
        {/* Contracts Awaiting Settlement */}
        {awaitingSettlements.length > 0 && (
          <>
            <Box sx={{ p: 2, pb: 1 }}>
              <Typography variant="subtitle2" color="error.main" gutterBottom>
                <ScheduleIcon sx={{ fontSize: 16, mr: 1, verticalAlign: 'middle' }} />
                Awaiting Settlement ({awaitingSettlements.length})
              </Typography>
            </Box>
            <List dense>
              {awaitingSettlements.map((contract) => (
                <ListItem key={contract.id}>
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" fontWeight="medium">
                          {contract.contractNumber}
                        </Typography>
                        <Chip 
                          label={contract.urgencyLevel} 
                          size="small" 
                          color={getUrgencyColor(contract.urgencyLevel)}
                          variant="outlined"
                        />
                      </Box>
                    }
                    secondary={
                      <React.Fragment>
                        <Typography component="span" variant="caption" color="text.secondary">
                          {contract.supplierName} • {contract.productName}
                        </Typography>
                        <br />
                        <Typography component="span" variant="caption" color="text.secondary">
                          Laycan ended: {formatDate(contract.laycanEnd)}
                        </Typography>
                      </React.Fragment>
                    }
                  />
                  <ListItemSecondaryAction>
                    <IconButton 
                      size="small" 
                      onClick={() => handleStartSettlement(contract.id)}
                      color="primary"
                      title="Start Settlement"
                    >
                      <StartIcon />
                    </IconButton>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
            </List>
            <Divider />
          </>
        )}

        {/* In-Progress Settlements */}
        {pendingSettlements.length > 0 && (
          <>
            <Box sx={{ p: 2, pb: 1 }}>
              <Typography variant="subtitle2" color="primary.main" gutterBottom>
                <AssignmentIcon sx={{ fontSize: 16, mr: 1, verticalAlign: 'middle' }} />
                In Progress ({pendingSettlements.length})
              </Typography>
            </Box>
            <List dense>
              {pendingSettlements.slice(0, 5).map((settlement) => (
                <ListItem key={settlement.id}>
                  <ListItemText
                    primary={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" fontWeight="medium">
                          {settlement.contractNumber}
                        </Typography>
                        <Chip 
                          label={settlement.status || 'Draft'} 
                          size="small" 
                          color="primary"
                          variant="outlined"
                        />
                      </Box>
                    }
                    secondary={
                      <React.Fragment>
                        <Typography component="span" variant="caption" color="text.secondary">
                          Amount: {new Intl.NumberFormat('en-US', {
                            style: 'currency',
                            currency: settlement.settlementCurrency || 'USD',
                            minimumFractionDigits: 0
                          }).format(settlement.totalSettlementAmount || 0)}
                        </Typography>
                        <br />
                        <Typography component="span" variant="caption" color="text.secondary">
                          Created: {formatDate(settlement.createdDate)}
                        </Typography>
                      </React.Fragment>
                    }
                  />
                  <ListItemSecondaryAction>
                    <IconButton 
                      size="small" 
                      onClick={() => handleEditSettlement(settlement.id)}
                      title="Edit Settlement"
                    >
                      <EditIcon />
                    </IconButton>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
            </List>
          </>
        )}

        {/* Empty State */}
        {pendingSettlements.length === 0 && awaitingSettlements.length === 0 && (
          <Box sx={{ 
            display: 'flex', 
            flexDirection: 'column', 
            alignItems: 'center', 
            justifyContent: 'center', 
            minHeight: 200,
            textAlign: 'center',
            p: 3
          }}>
            <ScheduleIcon sx={{ fontSize: 48, color: 'grey.300', mb: 2 }} />
            <Typography variant="h6" color="text.secondary" gutterBottom>
              No Pending Settlements
            </Typography>
            <Typography variant="body2" color="text.secondary">
              All settlements are up to date
            </Typography>
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default PendingSettlements;