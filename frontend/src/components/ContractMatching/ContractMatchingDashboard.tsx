import React, { useState, useEffect, useMemo } from 'react';
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
  LinearProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Visibility as ViewIcon,
  TrendingUp as TrendingUpIcon,
  Balance as BalanceIcon,
  Assessment as AssessmentIcon,
  Refresh as RefreshIcon,
  LinkOff as UnlinkIcon,
  AutoAwesome as SuggestIcon,
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

const formatCurrency = (value: number, currency = 'USD'): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
};

const formatUnitPrice = (value: number, currency = 'USD'): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
};

export default function ContractMatchingDashboard() {
  const [tabValue, setTabValue] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Data states
  const [availablePurchases, setAvailablePurchases] = useState<AvailablePurchase[]>([]);
  const [unmatchedSales, setUnmatchedSales] = useState<UnmatchedSales[]>([]);
  const [enhancedPositions, setEnhancedPositions] = useState<EnhancedNetPosition[]>([]);
  const [selectedPurchaseMatchings, setSelectedPurchaseMatchings] = useState<PurchaseMatching[]>([]);
  const [viewMatchingsPurchaseId, setViewMatchingsPurchaseId] = useState<string | null>(null);

  // Dialog states
  const [matchingDialogOpen, setMatchingDialogOpen] = useState(false);
  const [selectedPurchase, setSelectedPurchase] = useState<AvailablePurchase | null>(null);
  const [selectedSales, setSelectedSales] = useState<UnmatchedSales | null>(null);
  const [matchingRequest, setMatchingRequest] = useState<Partial<CreateMatchingRequest>>({});
  const [viewMatchingsDialogOpen, setViewMatchingsDialogOpen] = useState(false);

  // Suggested matches (T3.2)
  const suggestedMatches = useMemo(() => {
    return contractMatchingApi.findPotentialMatches(availablePurchases, unmatchedSales);
  }, [availablePurchases, unmatchedSales]);

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
      quantity: sales ? Math.min(purchase.availableQuantity, sales.availableQuantity) : 0,
      matchedBy: 'current-user@company.com',
      notes: '',
    });
    setMatchingDialogOpen(true);
  };

  const handleQuickMatch = (suggestion: typeof suggestedMatches[0]) => {
    const purchase = availablePurchases.find(p => p.id === suggestion.purchaseId);
    const sales = unmatchedSales.find(s => s.id === suggestion.salesId);
    if (purchase && sales) {
      handleCreateMatching(purchase, sales);
    }
  };

  const handleViewMatchings = async (purchaseId: string) => {
    try {
      const matchings = await contractMatchingApi.getPurchaseMatchings(purchaseId);
      setSelectedPurchaseMatchings(matchings);
      setViewMatchingsPurchaseId(purchaseId);
      setViewMatchingsDialogOpen(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load matchings');
    }
  };

  const handleUnmatch = async (matchingId: string) => {
    if (!confirm('Are you sure you want to remove this matching?')) return;
    try {
      const result = await contractMatchingApi.deleteMatching(matchingId);
      if (result.success) {
        // Refresh the matchings dialog and main data
        if (viewMatchingsPurchaseId) {
          const matchings = await contractMatchingApi.getPurchaseMatchings(viewMatchingsPurchaseId);
          setSelectedPurchaseMatchings(matchings);
        }
        loadData();
      } else {
        setError('Failed to remove matching');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to remove matching');
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
      loadData();
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create matching');
    }
  };

  const formatQuantity = (quantity: number) => {
    return new Intl.NumberFormat('en-US').format(quantity);
  };

  // Calculate P&L preview for matching dialog (T3.1)
  const matchingPnLPreview = useMemo(() => {
    if (!selectedPurchase || !selectedSales || !matchingRequest.quantity) return null;
    const buyPrice = selectedPurchase.unitPrice;
    const sellPrice = selectedSales.unitPrice;
    if (buyPrice == null || sellPrice == null) return null;
    const qty = matchingRequest.quantity;
    const grossMargin = (sellPrice - buyPrice) * qty;
    const marginPerUnit = sellPrice - buyPrice;
    return {
      buyPrice,
      sellPrice,
      grossMargin,
      marginPerUnit,
      currency: selectedPurchase.currency || 'USD',
    };
  }, [selectedPurchase, selectedSales, matchingRequest.quantity]);

  // Calculate summary statistics
  const stats = {
    totalAvailablePurchases: availablePurchases.length,
    totalUnmatchedSales: unmatchedSales.length,
    totalAvailableQuantity: availablePurchases.reduce((sum, p) => sum + p.availableQuantity, 0),
    totalUnmatchedQuantity: unmatchedSales.reduce((sum, s) => sum + s.availableQuantity, 0),
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

      {/* Tabs - now with 4 tabs including Suggested Matches */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={tabValue} onChange={handleTabChange}>
          <Tab label="Available Purchases" />
          <Tab label="Unmatched Sales" />
          <Tab label={`Suggested Matches (${suggestedMatches.length})`} />
          <Tab label="Enhanced Net Position" />
        </Tabs>
      </Box>

      {/* Tab 0: Available Purchases */}
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
                      <TableCell align="right">Unit Price</TableCell>
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
                        <TableCell align="right">
                          {purchase.unitPrice != null ? (
                            <Typography variant="body2">
                              {formatUnitPrice(purchase.unitPrice, purchase.currency)}
                            </Typography>
                          ) : (
                            <Chip label="Floating" size="small" variant="outlined" />
                          )}
                        </TableCell>
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

      {/* Tab 1: Unmatched Sales - now shows matched/available quantities (T3.4) */}
      <TabPanel value={tabValue} index={1}>
        <Card>
          <CardHeader title="Sales Contracts with Available Quantity" />
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
                      <TableCell align="right">Unit Price</TableCell>
                      <TableCell align="right">Total Qty</TableCell>
                      <TableCell align="right">Matched</TableCell>
                      <TableCell align="right">Available</TableCell>
                      <TableCell align="center">Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {unmatchedSales.map((sales) => (
                      <TableRow key={sales.id}>
                        <TableCell>{sales.contractNumber}</TableCell>
                        <TableCell>{sales.tradingPartnerName}</TableCell>
                        <TableCell>{sales.productName}</TableCell>
                        <TableCell align="right">
                          {sales.unitPrice != null ? (
                            <Typography variant="body2">
                              {formatUnitPrice(sales.unitPrice, sales.currency)}
                            </Typography>
                          ) : (
                            <Chip label="Floating" size="small" variant="outlined" />
                          )}
                        </TableCell>
                        <TableCell align="right">{formatQuantity(sales.contractQuantity)}</TableCell>
                        <TableCell align="right">
                          {sales.matchedQuantity > 0 ? (
                            <Box>
                              <Typography variant="body2">{formatQuantity(sales.matchedQuantity)}</Typography>
                              <LinearProgress
                                variant="determinate"
                                value={(sales.matchedQuantity / sales.contractQuantity) * 100}
                                sx={{ mt: 0.5, height: 4, borderRadius: 2 }}
                              />
                            </Box>
                          ) : (
                            <Typography variant="body2" color="text.secondary">0</Typography>
                          )}
                        </TableCell>
                        <TableCell align="right">
                          <Typography color="warning.main" fontWeight="bold">
                            {formatQuantity(sales.availableQuantity)}
                          </Typography>
                        </TableCell>
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

      {/* Tab 2: Suggested Matches (T3.2) */}
      <TabPanel value={tabValue} index={2}>
        <Card>
          <CardHeader
            title="Suggested Matches"
            subheader="Auto-generated match suggestions ranked by estimated margin"
            avatar={<SuggestIcon color="primary" />}
          />
          <CardContent>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                <CircularProgress />
              </Box>
            ) : suggestedMatches.length === 0 ? (
              <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
                No matching suggestions available. Ensure both purchase and sales contracts exist for the same products.
              </Typography>
            ) : (
              <TableContainer component={Paper}>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableCell>Purchase Contract</TableCell>
                      <TableCell>Sales Contract</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell align="right">Buy Price</TableCell>
                      <TableCell align="right">Sell Price</TableCell>
                      <TableCell align="right">Max Qty</TableCell>
                      <TableCell align="right">Est. Margin</TableCell>
                      <TableCell align="center">Action</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {suggestedMatches.slice(0, 20).map((suggestion, index) => (
                      <TableRow key={`${suggestion.purchaseId}-${suggestion.salesId}-${index}`}>
                        <TableCell>
                          <Typography variant="body2" fontWeight="medium">
                            {suggestion.purchaseContract}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontWeight="medium">
                            {suggestion.salesContract}
                          </Typography>
                        </TableCell>
                        <TableCell>{suggestion.productName}</TableCell>
                        <TableCell align="right">
                          {suggestion.purchaseUnitPrice != null
                            ? formatUnitPrice(suggestion.purchaseUnitPrice, suggestion.currency)
                            : '-'}
                        </TableCell>
                        <TableCell align="right">
                          {suggestion.salesUnitPrice != null
                            ? formatUnitPrice(suggestion.salesUnitPrice, suggestion.currency)
                            : '-'}
                        </TableCell>
                        <TableCell align="right">{formatQuantity(suggestion.maxQuantity)}</TableCell>
                        <TableCell align="right">
                          {suggestion.estimatedMargin != null ? (
                            <Typography
                              fontWeight="bold"
                              color={suggestion.estimatedMargin >= 0 ? 'success.main' : 'error.main'}
                            >
                              {suggestion.estimatedMargin >= 0 ? '+' : ''}
                              {formatCurrency(suggestion.estimatedMargin, suggestion.currency)}
                            </Typography>
                          ) : (
                            <Chip label="Floating" size="small" variant="outlined" />
                          )}
                        </TableCell>
                        <TableCell align="center">
                          <Button
                            size="small"
                            variant="contained"
                            onClick={() => handleQuickMatch(suggestion)}
                          >
                            Match
                          </Button>
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

      {/* Tab 3: Enhanced Net Position */}
      <TabPanel value={tabValue} index={3}>
        <Card>
          <CardHeader title="Enhanced Net Position with Natural Hedging" />
          <CardContent>
            {loading ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
                <CircularProgress />
              </Box>
            ) : (
              <TableContainer component={Paper}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Product</TableCell>
                      <TableCell>Type</TableCell>
                      <TableCell align="right">Total Purchased</TableCell>
                      <TableCell align="right">Total Sold</TableCell>
                      <TableCell align="right">Matched</TableCell>
                      <TableCell align="right">Net Position</TableCell>
                      <TableCell align="right">Natural Hedge</TableCell>
                      <TableCell align="right">Net Exposure</TableCell>
                      <TableCell sx={{ minWidth: 180 }}>Hedge Ratio</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {enhancedPositions.map((position) => {
                      const hedgeColor = position.hedgeRatio >= 80 ? 'success' : position.hedgeRatio >= 50 ? 'warning' : 'error';
                      return (
                        <TableRow key={position.productId}>
                          <TableCell><Typography fontWeight="medium">{position.productName}</Typography></TableCell>
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
                            <Typography color="primary.main" fontWeight="bold">
                              {formatQuantity(position.totalMatched)}
                            </Typography>
                          </TableCell>
                          <TableCell align="right">
                            <Typography
                              color={position.netPosition >= 0 ? 'success.main' : 'error.main'}
                              fontWeight="bold"
                            >
                              {position.netPosition >= 0 ? '+' : ''}{formatQuantity(position.netPosition)}
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
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Box sx={{ flexGrow: 1 }}>
                                <LinearProgress
                                  variant="determinate"
                                  value={Math.min(position.hedgeRatio, 100)}
                                  color={hedgeColor}
                                  sx={{ height: 8, borderRadius: 4 }}
                                />
                              </Box>
                              <Typography
                                variant="body2"
                                fontWeight="bold"
                                color={`${hedgeColor}.main`}
                                sx={{ minWidth: 45, textAlign: 'right' }}
                              >
                                {position.hedgeRatio.toFixed(1)}%
                              </Typography>
                            </Box>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                    {/* Portfolio Summary Row */}
                    {enhancedPositions.length > 0 && (() => {
                      const totals = enhancedPositions.reduce((acc, p) => ({
                        purchased: acc.purchased + p.totalPurchased,
                        sold: acc.sold + p.totalSold,
                        matched: acc.matched + p.totalMatched,
                        netPosition: acc.netPosition + p.netPosition,
                        naturalHedge: acc.naturalHedge + p.naturalHedge,
                        netExposure: acc.netExposure + p.netExposure,
                      }), { purchased: 0, sold: 0, matched: 0, netPosition: 0, naturalHedge: 0, netExposure: 0 });
                      const portfolioHedge = totals.purchased > 0 ? (totals.matched / totals.purchased * 100) : 0;
                      const hedgeColor = portfolioHedge >= 80 ? 'success' : portfolioHedge >= 50 ? 'warning' : 'error';
                      return (
                        <TableRow sx={{ bgcolor: 'action.hover', '& td': { fontWeight: 'bold', borderTop: 2, borderColor: 'divider' } }}>
                          <TableCell colSpan={2}><Typography fontWeight="bold">PORTFOLIO TOTAL</Typography></TableCell>
                          <TableCell align="right">{formatQuantity(totals.purchased)}</TableCell>
                          <TableCell align="right">{formatQuantity(totals.sold)}</TableCell>
                          <TableCell align="right"><Typography color="primary.main" fontWeight="bold">{formatQuantity(totals.matched)}</Typography></TableCell>
                          <TableCell align="right">
                            <Typography color={totals.netPosition >= 0 ? 'success.main' : 'error.main'} fontWeight="bold">
                              {totals.netPosition >= 0 ? '+' : ''}{formatQuantity(totals.netPosition)}
                            </Typography>
                          </TableCell>
                          <TableCell align="right"><Typography color="primary" fontWeight="bold">{formatQuantity(totals.naturalHedge)}</Typography></TableCell>
                          <TableCell align="right"><Typography color={Math.abs(totals.netExposure) < Math.abs(totals.netPosition) ? 'success.main' : 'warning.main'} fontWeight="bold">{formatQuantity(totals.netExposure)}</Typography></TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <Box sx={{ flexGrow: 1 }}>
                                <LinearProgress variant="determinate" value={Math.min(portfolioHedge, 100)} color={hedgeColor} sx={{ height: 8, borderRadius: 4 }} />
                              </Box>
                              <Typography variant="body2" fontWeight="bold" color={`${hedgeColor}.main`} sx={{ minWidth: 45, textAlign: 'right' }}>
                                {portfolioHedge.toFixed(1)}%
                              </Typography>
                            </Box>
                          </TableCell>
                        </TableRow>
                      );
                    })()}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Create Matching Dialog - with P&L Preview (T3.1) */}
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
                    {selectedPurchase.unitPrice != null && (
                      <Typography fontWeight="bold">
                        Buy Price: {formatUnitPrice(selectedPurchase.unitPrice, selectedPurchase.currency)}/unit
                      </Typography>
                    )}
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
                    <Typography>Available: {formatQuantity(selectedSales.availableQuantity)}</Typography>
                    {selectedSales.unitPrice != null && (
                      <Typography fontWeight="bold">
                        Sell Price: {formatUnitPrice(selectedSales.unitPrice, selectedSales.currency)}/unit
                      </Typography>
                    )}
                  </Box>
                ) : (
                  <Typography color="text.secondary">Select a sales contract</Typography>
                )}
              </Grid>

              {/* P&L Preview Card (T3.1) */}
              {matchingPnLPreview && matchingRequest.quantity && matchingRequest.quantity > 0 && (
                <Grid item xs={12}>
                  <Paper
                    variant="outlined"
                    sx={{
                      p: 2,
                      bgcolor: matchingPnLPreview.grossMargin >= 0 ? 'success.50' : 'error.50',
                      borderColor: matchingPnLPreview.grossMargin >= 0 ? 'success.main' : 'error.main',
                    }}
                  >
                    <Grid container spacing={2} alignItems="center">
                      <Grid item xs={3}>
                        <Typography variant="caption" color="text.secondary">Buy Price</Typography>
                        <Typography variant="body1" fontWeight="bold">
                          {formatUnitPrice(matchingPnLPreview.buyPrice, matchingPnLPreview.currency)}
                        </Typography>
                      </Grid>
                      <Grid item xs={3}>
                        <Typography variant="caption" color="text.secondary">Sell Price</Typography>
                        <Typography variant="body1" fontWeight="bold">
                          {formatUnitPrice(matchingPnLPreview.sellPrice, matchingPnLPreview.currency)}
                        </Typography>
                      </Grid>
                      <Grid item xs={3}>
                        <Typography variant="caption" color="text.secondary">Margin/Unit</Typography>
                        <Typography
                          variant="body1"
                          fontWeight="bold"
                          color={matchingPnLPreview.marginPerUnit >= 0 ? 'success.main' : 'error.main'}
                        >
                          {matchingPnLPreview.marginPerUnit >= 0 ? '+' : ''}
                          {formatUnitPrice(matchingPnLPreview.marginPerUnit, matchingPnLPreview.currency)}
                        </Typography>
                      </Grid>
                      <Grid item xs={3}>
                        <Typography variant="caption" color="text.secondary">Est. Gross Margin</Typography>
                        <Typography
                          variant="h6"
                          fontWeight="bold"
                          color={matchingPnLPreview.grossMargin >= 0 ? 'success.main' : 'error.main'}
                        >
                          {matchingPnLPreview.grossMargin >= 0 ? '+' : ''}
                          {formatCurrency(matchingPnLPreview.grossMargin, matchingPnLPreview.currency)}
                        </Typography>
                      </Grid>
                    </Grid>
                  </Paper>
                </Grid>
              )}

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

      {/* View Matchings Dialog - with Unmatch button (T3.3) */}
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
                  <TableCell align="center">Actions</TableCell>
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
                    <TableCell align="center">
                      <Tooltip title="Remove Matching">
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => handleUnmatch(matching.id)}
                        >
                          <UnlinkIcon />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
                {selectedPurchaseMatchings.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center">
                      <Typography variant="body2" color="text.secondary" py={2}>
                        No matchings found for this contract
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
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
