import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Button,
  Chip,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Stack,
  IconButton,
  Tooltip,
  Grid,
  Card,
  CardContent,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Block as BlockIcon,
  CheckCircle as UnblockIcon,
  Person as PersonIcon,
  Business as BusinessIcon,
  Warning as WarningIcon,
  Assessment as AssessmentIcon,
  Cancel as CancelIcon
} from '@mui/icons-material';
import { TradingPartnerSummary } from '../../types/tradingPartner';
import { tradingPartnerService } from '../../services/tradingPartnerService';
import { TradingPartnerForm } from './TradingPartnerForm';
import { TradingPartnerAnalysisComponent } from './TradingPartnerAnalysis';

export const TradingPartnersList: React.FC = () => {
  const [tradingPartners, setTradingPartners] = useState<TradingPartnerSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<string>('All');
  const [filterStatus, setFilterStatus] = useState<string>('All');
  const [formOpen, setFormOpen] = useState(false);
  const [selectedPartner, setSelectedPartner] = useState<TradingPartnerSummary | null>(null);
  const [analysisOpen, setAnalysisOpen] = useState(false);
  const [analysisPartnerId, setAnalysisPartnerId] = useState<string | null>(null);

  useEffect(() => {
    loadTradingPartners();
  }, []);

  const loadTradingPartners = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await tradingPartnerService.getTradingPartners();
      setTradingPartners(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load trading partners');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setSelectedPartner(null);
    setFormOpen(true);
  };

  const handleEdit = async (partner: TradingPartnerSummary) => {
    // Load complete trading partner data (includes all credit info, contact details, etc)
    // Don't edit with summary data which is missing fields
    setSelectedPartner(partner);
    setFormOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this trading partner?')) {
      try {
        await tradingPartnerService.deleteTradingPartner(id);
        await loadTradingPartners();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to delete trading partner');
      }
    }
  };

  const handleBlock = async (id: string) => {
    const reason = prompt('Enter reason for blocking:');
    if (reason) {
      try {
        await tradingPartnerService.blockTradingPartner(id, reason);
        await loadTradingPartners();
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to block trading partner');
      }
    }
  };

  const handleUnblock = async (id: string) => {
    try {
      await tradingPartnerService.unblockTradingPartner(id);
      await loadTradingPartners();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to unblock trading partner');
    }
  };

  const handleFormSubmit = async (data: any) => {
    try {
      if (selectedPartner) {
        await tradingPartnerService.updateTradingPartner(selectedPartner.id, data);
      } else {
        await tradingPartnerService.createTradingPartner(data);
      }
      await loadTradingPartners();
      setFormOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save trading partner');
    }
  };

  const handleViewAnalysis = (partner: TradingPartnerSummary) => {
    setAnalysisPartnerId(partner.id);
    setAnalysisOpen(true);
  };

  const filteredPartners = tradingPartners.filter(partner => {
    const matchesSearch = partner.companyName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         partner.companyCode.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesType = filterType === 'All' || partner.partnerType === filterType;
    const matchesStatus = filterStatus === 'All' || 
                         (filterStatus === 'Active' && partner.isActive) ||
                         (filterStatus === 'Inactive' && !partner.isActive);
    return matchesSearch && matchesType && matchesStatus;
  });

  const getPartnerTypeIcon = (type: string) => {
    switch (type) {
      case 'Supplier': return <BusinessIcon fontSize="small" />;
      case 'Customer': return <PersonIcon fontSize="small" />;
      default: return <BusinessIcon fontSize="small" />;
    }
  };

  const getCreditStatusColor = (partner: TradingPartnerSummary): 'success' | 'warning' | 'error' => {
    if (partner.isCreditExceeded) return 'error';
    if (partner.creditUtilization > 0.8) return 'warning';
    return 'success';
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', { 
      style: 'currency', 
      currency: 'USD' 
    }).format(amount);
  };

  // Statistics
  const totalPartners = tradingPartners.length;
  const activePartners = tradingPartners.filter(p => p.isActive).length;
  const suppliers = tradingPartners.filter(p => p.partnerType === 'Supplier').length;
  const customers = tradingPartners.filter(p => p.partnerType === 'Customer').length;
  const creditExceeded = tradingPartners.filter(p => p.isCreditExceeded).length;

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" fontWeight="bold">
          Trading Partners
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={handleCreate}
        >
          Add Trading Partner
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Statistics Cards */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={2.4}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="text.secondary" gutterBottom>
                Total Partners
              </Typography>
              <Typography variant="h4" component="div">
                {totalPartners}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={2.4}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="text.secondary" gutterBottom>
                Active
              </Typography>
              <Typography variant="h4" component="div" color="success.main">
                {activePartners}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={2.4}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="text.secondary" gutterBottom>
                Suppliers
              </Typography>
              <Typography variant="h4" component="div" color="primary.main">
                {suppliers}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={2.4}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="text.secondary" gutterBottom>
                Customers
              </Typography>
              <Typography variant="h4" component="div" color="secondary.main">
                {customers}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={2.4}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography color="text.secondary" gutterBottom>
                Credit Exceeded
              </Typography>
              <Typography variant="h4" component="div" color="error.main">
                {creditExceeded}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Filters */}
      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField
            label="Search"
            variant="outlined"
            size="small"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            sx={{ minWidth: 200 }}
          />
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Type</InputLabel>
            <Select
              value={filterType}
              label="Type"
              onChange={(e) => setFilterType(e.target.value)}
            >
              <MenuItem value="All">All Types</MenuItem>
              <MenuItem value="Supplier">Supplier</MenuItem>
              <MenuItem value="Customer">Customer</MenuItem>
              <MenuItem value="Both">Both</MenuItem>
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={filterStatus}
              label="Status"
              onChange={(e) => setFilterStatus(e.target.value)}
            >
              <MenuItem value="All">All Status</MenuItem>
              <MenuItem value="Active">Active</MenuItem>
              <MenuItem value="Inactive">Inactive</MenuItem>
            </Select>
          </FormControl>
        </Stack>
      </Paper>

      {/* Trading Partners Table */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Company</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Credit Limit</TableCell>
              <TableCell>Current Exposure</TableCell>
              <TableCell>Utilization</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  Loading...
                </TableCell>
              </TableRow>
            ) : filteredPartners.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  No trading partners found
                </TableCell>
              </TableRow>
            ) : (
              filteredPartners.map((partner) => (
                <TableRow key={partner.id} hover>
                  <TableCell>
                    <Box>
                      <Typography variant="body1" fontWeight="medium">
                        {partner.companyName}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {partner.companyCode}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      icon={getPartnerTypeIcon(partner.partnerType)}
                      label={partner.partnerType}
                      size="small"
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>{formatCurrency(partner.creditLimit)}</TableCell>
                  <TableCell>
                    <Box display="flex" alignItems="center">
                      {formatCurrency(partner.currentExposure)}
                      {partner.isCreditExceeded && (
                        <Tooltip title="Credit limit exceeded">
                          <WarningIcon color="error" sx={{ ml: 1 }} fontSize="small" />
                        </Tooltip>
                      )}
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={`${(partner.creditUtilization * 100).toFixed(1)}%`}
                      color={getCreditStatusColor(partner)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={partner.isActive ? 'Active' : 'Inactive'}
                      color={partner.isActive ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={1}>
                      <Tooltip title="View Analysis">
                        <IconButton
                          size="small"
                          color="info"
                          onClick={() => handleViewAnalysis(partner)}
                        >
                          <AssessmentIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Edit">
                        <IconButton
                          size="small"
                          onClick={() => handleEdit(partner)}
                        >
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={partner.isActive ? "Block" : "Unblock"}>
                        <IconButton
                          size="small"
                          color={partner.isActive ? "warning" : "success"}
                          onClick={() => partner.isActive ? handleBlock(partner.id) : handleUnblock(partner.id)}
                        >
                          {partner.isActive ? <BlockIcon /> : <UnblockIcon />}
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete">
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleDelete(partner.id)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Tooltip>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Form Dialog */}
      <TradingPartnerForm
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSubmit={handleFormSubmit}
        initialData={selectedPartner}
      />

      {/* Analysis Dialog */}
      {analysisOpen && analysisPartnerId && (
        <Dialog open={analysisOpen} onClose={() => setAnalysisOpen(false)} maxWidth="xl" fullWidth>
          <DialogTitle>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="h6">Trading Partner Analysis</Typography>
              <IconButton onClick={() => setAnalysisOpen(false)}>
                <CancelIcon />
              </IconButton>
            </Box>
          </DialogTitle>
          <DialogContent>
            <TradingPartnerAnalysisComponent 
              tradingPartnerId={analysisPartnerId}
            />
          </DialogContent>
        </Dialog>
      )}
    </Box>
  );
};