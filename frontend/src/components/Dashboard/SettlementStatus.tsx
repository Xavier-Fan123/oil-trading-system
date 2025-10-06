import React from 'react';
import {
  Card,
  CardHeader,
  CardContent,
  Typography,
  Box,
  Grid,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  CircularProgress,
  Alert
} from '@mui/material';
import {
  Assignment as AssignmentIcon,
  CheckCircle as CompletedIcon,
  Schedule as PendingIcon,
  ArrowForward as ArrowForwardIcon
} from '@mui/icons-material';
import { useSettlementStats } from '@/hooks/useSettlements';

interface SettlementStatusProps {
  height?: number;
}

export const SettlementStatus: React.FC<SettlementStatusProps> = ({ height = 400 }) => {
  const { stats, loading, error } = useSettlementStats();

  const statusItems = [
    {
      icon: <CompletedIcon sx={{ color: 'success.main' }} />,
      label: 'Finalized',
      count: stats.finalizedCount,
      color: 'success' as const,
    },
    {
      icon: <PendingIcon sx={{ color: 'warning.main' }} />,
      label: 'Draft/Pending',
      count: stats.draftCount,
      color: 'warning' as const,
    },
    {
      icon: <AssignmentIcon sx={{ color: 'primary.main' }} />,
      label: 'Total Settlements',
      count: stats.totalSettlements,
      color: 'primary' as const,
    }
  ];

  const formatCurrency = (amount: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
  };

  if (loading) {
    return (
      <Card sx={{ height }}>
        <CardHeader title="Settlement Status" />
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
        <CardHeader title="Settlement Status" />
        <CardContent>
          <Alert severity="error">{error}</Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ height }}>
      <CardHeader 
        title="Settlement Status" 
        subheader="Current settlement overview"
        action={
          <IconButton size="small" href="/settlements" title="View All Settlements">
            <ArrowForwardIcon />
          </IconButton>
        }
      />
      <CardContent>
        <Grid container spacing={2} sx={{ mb: 3 }}>
          {statusItems.map((item, index) => (
            <Grid item xs={12} sm={4} key={index}>
              <Box 
                sx={{ 
                  display: 'flex', 
                  alignItems: 'center', 
                  p: 2, 
                  borderRadius: 1, 
                  bgcolor: 'grey.50',
                  border: `1px solid`,
                  borderColor: `${item.color}.200`
                }}
              >
                <Box sx={{ mr: 2 }}>
                  {item.icon}
                </Box>
                <Box>
                  <Typography variant="h6" color={`${item.color}.main`}>
                    {item.count}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {item.label}
                  </Typography>
                </Box>
              </Box>
            </Grid>
          ))}
        </Grid>

        {/* Total Settlement Value */}
        <Box sx={{ p: 2, bgcolor: 'primary.50', borderRadius: 1, mb: 2 }}>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Total Settlement Value
          </Typography>
          <Typography variant="h5" color="primary.main">
            {formatCurrency(stats.totalValue, stats.currency)}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Across all settlements
          </Typography>
        </Box>

        {/* Quick Stats */}
        <Box>
          <Typography variant="subtitle2" gutterBottom>
            Quick Stats
          </Typography>
          <List dense>
            <ListItem>
              <ListItemText 
                primary="Average Settlement Value" 
                secondary={stats.totalSettlements > 0 ? 
                  formatCurrency(stats.totalValue / stats.totalSettlements, stats.currency) : 
                  'N/A'
                }
              />
            </ListItem>
            <ListItem>
              <ListItemText 
                primary="Completion Rate" 
                secondary={stats.totalSettlements > 0 ? 
                  `${((stats.finalizedCount / stats.totalSettlements) * 100).toFixed(1)}%` : 
                  'N/A'
                }
              />
              <ListItemSecondaryAction>
                <Chip 
                  size="small" 
                  label={stats.totalSettlements > 0 ? 
                    `${stats.finalizedCount}/${stats.totalSettlements}` : 
                    '0/0'
                  }
                  color={stats.totalSettlements > 0 && (stats.finalizedCount / stats.totalSettlements) > 0.8 ? 'success' : 'warning'}
                />
              </ListItemSecondaryAction>
            </ListItem>
          </List>
        </Box>
      </CardContent>
    </Card>
  );
};

export default SettlementStatus;