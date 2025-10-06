import React, { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Alert,
  CircularProgress,
  Tabs,
  Tab,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  TrendingUp as TrendingUpIcon,
  Balance as BalanceIcon,
  Assessment as AssessmentIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { contractMatchingApi } from '@/services/contractMatchingApi';
import type {
  AvailablePurchase,
  UnmatchedSales,
  CreateMatchingRequest,
  EnhancedNetPosition,
  PurchaseMatching,
} from '@/services/contractMatchingApi';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`contract-matching-tabpanel-${index}`}
      aria-labelledby={`contract-matching-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export default function ContractMatchingDashboard() {
  const [tabValue, setTabValue] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Data states
  const [availablePurchases, setAvailablePurchases] = useState<AvailablePurchase[]>([]);
  const [unmatchedSales, setUnmatchedSales] = useState<UnmatchedSales[]>([]);
  const [enhancedPositions, setEnhancedPositions] = useState<EnhancedNetPosition[]>([]);
  const [selectedPurchaseMatchings, setSelectedPurchaseMatchings] = useState<PurchaseMatching[]>([]);
  
  // Dialog states
  const [matchingDialogOpen, setMatchingDialogOpen] = useState(false);
  const [selectedPurchase, setSelectedPurchase] = useState<AvailablePurchase | null>(null);
  const [selectedSales, setSelectedSales] = useState<UnmatchedSales | null>(null);
  const [matchingRequest, setMatchingRequest] = useState<Partial<CreateMatchingRequest>>({});
  const [viewMatchingsDialogOpen, setViewMatchingsDialogOpen] = useState(false);

  // Load data
  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [purchasesResult, salesResult, positionsResult] = await Promise.all([
        contractMatchingApi.getAvailablePurchases(),
        contractMatchingApi.getUnmatchedSales(),
        contractMatchingApi.getEnhancedNetPosition(),
      ]);
      
      if (purchasesResult.success && purchasesResult.data) setAvailablePurchases(purchasesResult.data);
      if (salesResult.success && salesResult.data) setUnmatchedSales(salesResult.data);
      if (positionsResult.success && positionsResult.data) setEnhancedPositions(positionsResult.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleCreateMatching = (purchase: AvailablePurchase, sales?: UnmatchedSales) => {
    setSelectedPurchase(purchase);
    setSelectedSales(sales || null);
    setMatchingRequest({
      purchaseContractId: purchase.id,
      salesContractId: sales?.id,
      quantity: sales ? Math.min(purchase.availableQuantity, sales.contractQuantity) : 0,
      matchedBy: 'current-user@company.com', // This would come from auth context
      notes: '',
    });
    setMatchingDialogOpen(true);
  };

  const handleViewMatchings = async (purchaseId: string) => {
    try {
      const matchings = await contractMatchingApi.getPurchaseMatchings(purchaseId);
      setSelectedPurchaseMatchings(matchings);
      setViewMatchingsDialogOpen(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load matchings');
    }
  };

  const submitMatching = async () => {
    if (!matchingRequest.purchaseContractId || !matchingRequest.salesContractId || !matchingRequest.quantity) {
      setError('Please fill in all required fields');
      return;
    }

    try {
      await contractMatchingApi.createMatching(matchingRequest as CreateMatchingRequest);
      setMatchingDialogOpen(false);
      loadData(); // Refresh data
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create matching');
    }
  };

  // getStatusColor function removed - not used

  // formatCurrency function removed - not used

  const formatQuantity = (quantity: number) => {
    return new Intl.NumberFormat('en-US').format(quantity);
  };

  // Calculate summary statistics
  const stats = {
    totalAvailablePurchases: availablePurchases.length,
    totalUnmatchedSales: unmatchedSales.length,
    totalAvailableQuantity: availablePurchases.reduce((sum, p) => sum + p.availableQuantity, 0),
    totalUnmatchedQuantity: unmatchedSales.reduce((sum, s) => sum + s.contractQuantity, 0),
    totalNaturalHedge: enhancedPositions.reduce((sum, p) => sum + p.naturalHedge, 0),
    averageHedgeRatio: enhancedPositions.length > 0 
      ? enhancedPositions.reduce((sum, p) => sum + p.hedgeRatio, 0) / enhancedPositions.length 
      : 0,
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1">
          Contract Matching System
        </Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={loadData}
            disabled={loading}
            sx={{ mr: 2 }}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => handleCreateMatching(availablePurchases[0])}
            disabled={loading || availablePurchases.length === 0}
          >
            Create Matching
          </Button>
        </Box>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Statistics Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <BalanceIcon color="primary" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{stats.totalAvailablePurchases}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Available Purchases
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <TrendingUpIcon color="warning" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{stats.totalUnmatchedSales}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Unmatched Sales
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <AssessmentIcon color="success" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{formatQuantity(stats.totalNaturalHedge)}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Natural Hedge (MT)
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center' }}>
                <BalanceIcon color="info" sx={{ mr: 2 }} />
                <Box>
                  <Typography variant="h6">{stats.averageHedgeRatio.toFixed(1)}%</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Avg Hedge Ratio
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="Available Purchases" />
          <Tab label="Unmatched Sales" />
          <Tab label="Enhanced Net Position" />
        </Tabs>
      </Box>

      {/* Tab Panels */}
      <TabPanel value={tabValue} index={0}>
        <Card>
          <CardHeader title="Available Purchase Contracts" />
          <CardContent>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                <CircularProgress />
              </Box>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Contract Number</TableCell>
                      <TableCell>Trading Partner</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell align="right">Total Quantity</TableCell>
                      <TableCell align="right">Matched</TableCell>
                      <TableCell align="right">Available</TableCell>
                      <TableCell align="center">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {availablePurchases.map((purchase) => (
                      <TableRow key={purchase.id}>
                        <TableCell>{purchase.contractNumber}</TableCell>
                        <TableCell>{purchase.tradingPartnerName}</TableCell>
                        <TableCell>{purchase.productName}</TableCell>
                        <TableCell align="right">{formatQuantity(purchase.contractQuantity)}</TableCell>
                        <TableCell align="right">{formatQuantity(purchase.matchedQuantity)}</TableCell>
                        <TableCell align="right">
                          <Typography color="primary" fontWeight="bold">
                            {formatQuantity(purchase.availableQuantity)}
                          </Typography>
                        </TableCell>
                        <TableCell align="center">
                          <Tooltip title="Create Matching">
                            <IconButton
                              size="small"
                              onClick={() => handleCreateMatching(purchase)}
                            >
                              <AddIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="View Matchings">
                            <IconButton
                              size="small"
                              onClick={() => handleViewMatchings(purchase.id)}
                            >
                              <ViewIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      <TabPanel value={tabValue} index={1}>
        <Card>
          <CardHeader title="Unmatched Sales Contracts" />
          <CardContent>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                <CircularProgress />
              </Box>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Contract Number</TableCell>
                      <TableCell>Trading Partner</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="center">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {unmatchedSales.map((sales) => (
                      <TableRow key={sales.id}>
                        <TableCell>{sales.contractNumber}</TableCell>
                        <TableCell>{sales.tradingPartnerName}</TableCell>
                        <TableCell>{sales.productName}</TableCell>
                        <TableCell align="right">{formatQuantity(sales.contractQuantity)}</TableCell>
                        <TableCell align="center">
                          <Tooltip title="Find Matching Purchase">
                            <IconButton
                              size="small"
                              onClick={() => {
                                const compatiblePurchase = availablePurchases.find(
                                  p => p.productName === sales.productName && p.availableQuantity > 0
                                );
                                if (compatiblePurchase) {
                                  handleCreateMatching(compatiblePurchase, sales);
                                }
                              }}
                              disabled={!availablePurchases.some(
                                p => p.productName === sales.productName && p.availableQuantity > 0
                              )}
                            >
                              <AddIcon />
                            </IconButton>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      <TabPanel value={tabValue} index={2}>
        <Card>
          <CardHeader title="Enhanced Net Position with Natural Hedging" />
          <CardContent>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                <CircularProgress />
              </Box>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Product</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell align="right">Total Purchased</TableCell>
                      <TableCell align="right">Total Sold</TableCell>
                      <TableCell align="right">Net Position</TableCell>
                      <TableCell align="right">Natural Hedge</TableCell>
                      <TableCell align="right">Net Exposure</TableCell>
                      <TableCell align="right">Hedge Ratio</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {enhancedPositions.map((position) => (
                      <TableRow key={position.productId}>
                        <TableCell>{position.productName}</TableCell>
                        <TableCell>
                          <Chip 
                            label={position.productType} 
                            size="small" 
                            color={position.productType?.toString() === 'CrudeOil' ? 'primary' : 'secondary'}
                          />
                        </TableCell>
                        <TableCell align="right">{formatQuantity(position.totalPurchased)}</TableCell>
                        <TableCell align="right">{formatQuantity(position.totalSold)}</TableCell>
                        <TableCell align="right">
                          <Typography 
                            color={position.netPosition >= 0 ? 'success.main' : 'error.main'}
                            fontWeight="bold"
                          >
                            {formatQuantity(position.netPosition)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography color="primary" fontWeight="bold">
                            {formatQuantity(position.naturalHedge)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography 
                            color={Math.abs(position.netExposure) < Math.abs(position.netPosition) ? 'success.main' : 'warning.main'}
                            fontWeight="bold"
                          >
                            {formatQuantity(position.netExposure)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography color="info.main" fontWeight="bold">
                            {position.hedgeRatio.toFixed(1)}%
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Create Matching Dialog */}
      <Dialog 
        open={matchingDialogOpen} 
        onClose={() => setMatchingDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Create Contract Matching</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Typography variant="h6" gutterBottom>Purchase Contract</Typography>
                {selectedPurchase && (
                  <Box>
                    <Typography>Contract: {selectedPurchase.contractNumber}</Typography>
                    <Typography>Supplier: {selectedPurchase.tradingPartnerName}</Typography>
                    <Typography>Product: {selectedPurchase.productName}</Typography>
                    <Typography>Available: {formatQuantity(selectedPurchase.availableQuantity)}</Typography>
                  </Box>
                )}
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="h6" gutterBottom>Sales Contract</Typography>
                {selectedSales ? (
                  <Box>
                    <Typography>Contract: {selectedSales.contractNumber}</Typography>
                    <Typography>Customer: {selectedSales.tradingPartnerName}</Typography>
                    <Typography>Product: {selectedSales.productName}</Typography>
                    <Typography>Quantity: {formatQuantity(selectedSales.contractQuantity)}</Typography>
                  </Box>
                ) : (
                  <Typography color="text.secondary">Select a sales contract</Typography>
                )}
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Matching Quantity"
                  type="number"
                  value={matchingRequest.quantity || ''}
                  onChange={(e) => setMatchingRequest(prev => ({ 
                    ...prev, 
                    quantity: Number(e.target.value) 
                  }))}
                  inputProps={{ 
                    min: 0, 
                    max: selectedPurchase?.availableQuantity || 0 
                  }}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Notes"
                  multiline
                  rows={3}
                  value={matchingRequest.notes || ''}
                  onChange={(e) => setMatchingRequest(prev => ({ 
                    ...prev, 
                    notes: e.target.value 
                  }))}
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setMatchingDialogOpen(false)}>Cancel</Button>
          <Button onClick={submitMatching} variant="contained">
            Create Matching
          </Button>
        </DialogActions>
      </Dialog>

      {/* View Matchings Dialog */}
      <Dialog 
        open={viewMatchingsDialogOpen} 
        onClose={() => setViewMatchingsDialogOpen(false)}
        maxWidth="lg"
        fullWidth
      >
        <DialogTitle>Purchase Contract Matchings</DialogTitle>
        <DialogContent>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Sales Contract</TableCell>
                  <TableCell>Customer</TableCell>
                  <TableCell align="right">Matched Quantity</TableCell>
                  <TableCell>Date</TableCell>
                  <TableCell>Notes</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {selectedPurchaseMatchings.map((matching) => (
                  <TableRow key={matching.id}>
                    <TableCell>{matching.salesContractNumber}</TableCell>
                    <TableCell>{matching.salesTradingPartner}</TableCell>
                    <TableCell align="right">{formatQuantity(matching.matchedQuantity)}</TableCell>
                    <TableCell>{new Date(matching.matchedDate).toLocaleDateString()}</TableCell>
                    <TableCell>{matching.notes || '-'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setViewMatchingsDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}