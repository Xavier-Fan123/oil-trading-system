import React, { useState, useMemo } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Card,
  CardContent,
  Grid,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Alert,
  CircularProgress,
  Tabs,
  Tab,
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Add as AddIcon,
  TrendingUp,
  TrendingDown,
  SwapHoriz as SwapHorizIcon,
  Assessment as AssessmentIcon,
  Link as LinkIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ProductType, PositionType } from '@/types/positions';
import { ContractStatus } from '@/types/contracts';
import { useTradeGroups, usePortfolioRiskWithTradeGroups } from '@/hooks/useTradeGroups';
import { tradeGroupUtils } from '@/services/tradeGroupApi';

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
      id={`integration-tabpanel-${index}`}
      aria-labelledby={`integration-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

// Mock data interfaces for demonstration
interface FuturesPosition {
  id: string;
  contractMonth: string;
  productType: ProductType;
  position: PositionType;
  quantity: number;
  entryPrice: number;
  currentPrice: number;
  unrealizedPnL: number;
  tradeGroupId?: string;
  tradeGroupName?: string;
}

interface PhysicalPosition {
  id: string;
  contractNumber: string;
  contractType: 'Purchase' | 'Sales';
  counterparty: string;
  productType: ProductType;
  quantity: number;
  price: number;
  laycanStart: string;
  laycanEnd: string;
  status: ContractStatus;
  tradeGroupId?: string;
  tradeGroupName?: string;
}

interface BasisOpportunity {
  id: string;
  productType: ProductType;
  contractMonth: string;
  physicalPrice: number;
  futuresPrice: number;
  basis: number;
  basisPoints: number;
  historicalAverage: number;
  zScore: number;
  recommendedAction: 'BUY_PHYSICAL_SELL_FUTURES' | 'SELL_PHYSICAL_BUY_FUTURES' | 'HOLD';
  confidence: 'HIGH' | 'MEDIUM' | 'LOW';
}

// Mock data - in a real app, this would come from APIs
const mockFuturesPositions: FuturesPosition[] = [
  {
    id: '1',
    contractMonth: 'Jan-2025',
    productType: ProductType.Brent,
    position: PositionType.Long,
    quantity: 1000,
    entryPrice: 85.50,
    currentPrice: 87.20,
    unrealizedPnL: 17000,
    tradeGroupId: 'tg-1',
    tradeGroupName: 'Brent Calendar Spread Q1'
  },
  {
    id: '2',
    contractMonth: 'Feb-2025',
    productType: ProductType.Brent,
    position: PositionType.Short,
    quantity: 1000,
    entryPrice: 85.80,
    currentPrice: 87.50,
    unrealizedPnL: -17000,
    tradeGroupId: 'tg-1',
    tradeGroupName: 'Brent Calendar Spread Q1'
  },
  {
    id: '3',
    contractMonth: 'Mar-2025',
    productType: ProductType.WTI,
    position: PositionType.Long,
    quantity: 500,
    entryPrice: 82.30,
    currentPrice: 84.10,
    unrealizedPnL: 9000,
  }
];

const mockPhysicalPositions: PhysicalPosition[] = [
  {
    id: '1',
    contractNumber: 'PC-2025-001',
    contractType: 'Purchase',
    counterparty: 'Saudi Aramco',
    productType: ProductType.Brent,
    quantity: 2000,
    price: 86.50,
    laycanStart: '2025-01-15',
    laycanEnd: '2025-01-20',
    status: ContractStatus.Active,
    tradeGroupId: 'tg-2',
    tradeGroupName: 'North Sea Basis Hedge'
  },
  {
    id: '2',
    contractNumber: 'SC-2025-001',
    contractType: 'Sales',
    counterparty: 'Shell Trading',
    productType: ProductType.Brent,
    quantity: 1500,
    price: 87.80,
    laycanStart: '2025-02-10',
    laycanEnd: '2025-02-15',
    status: ContractStatus.Active,
  }
];

const mockBasisOpportunities: BasisOpportunity[] = [
  {
    id: '1',
    productType: ProductType.Brent,
    contractMonth: 'Mar-2025',
    physicalPrice: 86.20,
    futuresPrice: 84.90,
    basis: 1.30,
    basisPoints: 130,
    historicalAverage: 0.45,
    zScore: 2.1,
    recommendedAction: 'BUY_PHYSICAL_SELL_FUTURES',
    confidence: 'HIGH'
  },
  {
    id: '2',
    productType: ProductType.WTI,
    contractMonth: 'Apr-2025',
    physicalPrice: 83.50,
    futuresPrice: 84.20,
    basis: -0.70,
    basisPoints: -70,
    historicalAverage: -0.20,
    zScore: -1.8,
    recommendedAction: 'SELL_PHYSICAL_BUY_FUTURES',
    confidence: 'MEDIUM'
  }
];

export const FuturesSpotIntegration: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState(0);
  const [openHedgeDialog, setOpenHedgeDialog] = useState(false);

  // API hooks
  const { isLoading: loadingTradeGroups } = useTradeGroups();
  const { data: portfolioRisk, isLoading: loadingPortfolioRisk } = usePortfolioRiskWithTradeGroups();

  // Calculated metrics
  const futuresExposure = useMemo(() => {
    return mockFuturesPositions.reduce((acc, pos) => {
      const exposure = pos.quantity * pos.currentPrice;
      return acc + (pos.position === PositionType.Long ? exposure : -exposure);
    }, 0);
  }, []);

  const physicalExposure = useMemo(() => {
    return mockPhysicalPositions.reduce((acc, pos) => {
      const exposure = pos.quantity * pos.price;
      return acc + (pos.contractType === 'Purchase' ? exposure : -exposure);
    }, 0);
  }, []);

  const netExposure = futuresExposure + physicalExposure;

  const totalUnrealizedPnL = useMemo(() => {
    return mockFuturesPositions.reduce((acc, pos) => acc + pos.unrealizedPnL, 0);
  }, []);

  // Group positions by TradeGroup for integrated view
  const positionsByTradeGroup = useMemo(() => {
    const grouped: { [key: string]: { futures: FuturesPosition[], physical: PhysicalPosition[], tradeGroupName: string } } = {};
    
    // Group futures positions
    mockFuturesPositions.forEach(pos => {
      if (pos.tradeGroupId) {
        if (!grouped[pos.tradeGroupId]) {
          grouped[pos.tradeGroupId] = { futures: [], physical: [], tradeGroupName: pos.tradeGroupName || 'Unknown' };
        }
        grouped[pos.tradeGroupId].futures.push(pos);
      }
    });

    // Group physical positions
    mockPhysicalPositions.forEach(pos => {
      if (pos.tradeGroupId) {
        if (!grouped[pos.tradeGroupId]) {
          grouped[pos.tradeGroupId] = { futures: [], physical: [], tradeGroupName: pos.tradeGroupName || 'Unknown' };
        }
        grouped[pos.tradeGroupId].physical.push(pos);
      }
    });

    return grouped;
  }, []);

  const getActionColor = (action: string) => {
    switch (action) {
      case 'BUY_PHYSICAL_SELL_FUTURES': return 'success';
      case 'SELL_PHYSICAL_BUY_FUTURES': return 'warning';
      default: return 'default';
    }
  };

  const getActionLabel = (action: string) => {
    switch (action) {
      case 'BUY_PHYSICAL_SELL_FUTURES': return 'Buy Physical / Sell Futures';
      case 'SELL_PHYSICAL_BUY_FUTURES': return 'Sell Physical / Buy Futures';
      default: return 'Hold';
    }
  };

  const getConfidenceColor = (confidence: string) => {
    switch (confidence) {
      case 'HIGH': return 'success';
      case 'MEDIUM': return 'warning';
      case 'LOW': return 'error';
      default: return 'default';
    }
  };

  if (loadingTradeGroups || loadingPortfolioRisk) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Futures-Spot Integration
        </Typography>
        <Box>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setOpenHedgeDialog(true)}
            sx={{ mr: 2 }}
          >
            Create Hedge Strategy
          </Button>
          <Button
            variant="outlined"
            startIcon={<AssessmentIcon />}
          >
            Risk Analysis
          </Button>
        </Box>
      </Box>

      {/* Exposure Summary Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Futures Exposure</Typography>
              <Box display="flex" alignItems="center">
                <Typography 
                  variant="h5" 
                  sx={{ color: tradeGroupUtils.getPnLColor(futuresExposure) }}
                >
                  {tradeGroupUtils.formatCurrency(futuresExposure)}
                </Typography>
              </Box>
              <Typography variant="body2" color="textSecondary">
                Paper Positions
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Physical Exposure</Typography>
              <Typography 
                variant="h5" 
                sx={{ color: tradeGroupUtils.getPnLColor(physicalExposure) }}
              >
                {tradeGroupUtils.formatCurrency(physicalExposure)}
              </Typography>
              <Typography variant="body2" color="textSecondary">
                Physical Contracts
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Net Exposure</Typography>
              <Box display="flex" alignItems="center">
                {netExposure >= 0 ? (
                  <TrendingUp color="success" sx={{ mr: 1 }} />
                ) : (
                  <TrendingDown color="error" sx={{ mr: 1 }} />
                )}
                <Typography 
                  variant="h5" 
                  sx={{ color: tradeGroupUtils.getPnLColor(netExposure) }}
                >
                  {tradeGroupUtils.formatCurrency(netExposure)}
                </Typography>
              </Box>
              <Typography variant="body2" color="textSecondary">
                Combined Position
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Unrealized P&L</Typography>
              <Box display="flex" alignItems="center">
                {totalUnrealizedPnL >= 0 ? (
                  <TrendingUp color="success" sx={{ mr: 1 }} />
                ) : (
                  <TrendingDown color="error" sx={{ mr: 1 }} />
                )}
                <Typography 
                  variant="h5" 
                  sx={{ color: tradeGroupUtils.getPnLColor(totalUnrealizedPnL) }}
                >
                  {tradeGroupUtils.formatCurrency(totalUnrealizedPnL)}
                </Typography>
              </Box>
              <Typography variant="body2" color="textSecondary">
                Futures P&L
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Tabs value={selectedTab} onChange={(_, newValue) => setSelectedTab(newValue)} sx={{ mb: 3 }}>
        <Tab label="Integrated Positions" />
        <Tab label="Basis Opportunities" />
        <Tab label="Risk Management" />
        <Tab label="Strategy Performance" />
      </Tabs>

      <TabPanel value={selectedTab} index={0}>
        {/* Trade Group Integrated View */}
        <Typography variant="h6" gutterBottom>Positions by Trade Group</Typography>
        
        {Object.entries(positionsByTradeGroup).map(([tradeGroupId, positions]) => (
          <Accordion key={tradeGroupId} sx={{ mb: 2 }}>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Box display="flex" alignItems="center" width="100%">
                <Box flexGrow={1}>
                  <Typography variant="subtitle1" fontWeight="medium">
                    {positions.tradeGroupName}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    {positions.futures.length} futures, {positions.physical.length} physical contracts
                  </Typography>
                </Box>
                <Box display="flex" alignItems="center" gap={1} mr={2}>
                  {positions.futures.length > 0 && positions.physical.length > 0 && (
                    <Chip label="Integrated Strategy" size="small" color="success" />
                  )}
                  {positions.futures.length > 0 && (
                    <Chip label="Futures" size="small" color="primary" />
                  )}
                  {positions.physical.length > 0 && (
                    <Chip label="Physical" size="small" color="secondary" />
                  )}
                </Box>
              </Box>
            </AccordionSummary>
            <AccordionDetails>
              <Grid container spacing={2}>
                {/* Futures Positions */}
                {positions.futures.length > 0 && (
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>Futures Positions</Typography>
                    <TableContainer component={Paper} variant="outlined">
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell>Contract</TableCell>
                            <TableCell>Position</TableCell>
                            <TableCell align="right">Qty</TableCell>
                            <TableCell align="right">Price</TableCell>
                            <TableCell align="right">P&L</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {positions.futures.map((pos) => (
                            <TableRow key={pos.id}>
                              <TableCell>{pos.contractMonth}</TableCell>
                              <TableCell>
                                <Chip
                                  label={PositionType[pos.position]}
                                  size="small"
                                  color={pos.position === PositionType.Long ? 'success' : 'error'}
                                />
                              </TableCell>
                              <TableCell align="right">{tradeGroupUtils.formatQuantity(pos.quantity)}</TableCell>
                              <TableCell align="right">${pos.currentPrice.toFixed(2)}</TableCell>
                              <TableCell align="right">
                                <Typography sx={{ color: tradeGroupUtils.getPnLColor(pos.unrealizedPnL) }}>
                                  {tradeGroupUtils.formatCurrency(pos.unrealizedPnL)}
                                </Typography>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </Grid>
                )}

                {/* Physical Positions */}
                {positions.physical.length > 0 && (
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" gutterBottom>Physical Contracts</Typography>
                    <TableContainer component={Paper} variant="outlined">
                      <Table size="small">
                        <TableHead>
                          <TableRow>
                            <TableCell>Contract</TableCell>
                            <TableCell>Type</TableCell>
                            <TableCell align="right">Qty</TableCell>
                            <TableCell align="right">Price</TableCell>
                            <TableCell>Laycan</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {positions.physical.map((pos) => (
                            <TableRow key={pos.id}>
                              <TableCell>{pos.contractNumber}</TableCell>
                              <TableCell>
                                <Chip
                                  label={pos.contractType}
                                  size="small"
                                  color={pos.contractType === 'Purchase' ? 'primary' : 'secondary'}
                                />
                              </TableCell>
                              <TableCell align="right">{tradeGroupUtils.formatQuantity(pos.quantity)}</TableCell>
                              <TableCell align="right">${pos.price.toFixed(2)}</TableCell>
                              <TableCell>
                                {format(new Date(pos.laycanStart), 'MMM dd')} - {format(new Date(pos.laycanEnd), 'MMM dd')}
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </Grid>
                )}
              </Grid>
            </AccordionDetails>
          </Accordion>
        ))}

        {/* Unassigned Positions */}
        <Accordion sx={{ mb: 2 }}>
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box display="flex" alignItems="center" width="100%">
              <Box flexGrow={1}>
                <Typography variant="subtitle1" fontWeight="medium">
                  Unassigned Positions
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Positions not assigned to any trade group
                </Typography>
              </Box>
              <Chip label="Needs Classification" size="small" color="warning" />
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TableContainer component={Paper} variant="outlined">
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Contract</TableCell>
                        <TableCell>Type</TableCell>
                        <TableCell>Product</TableCell>
                        <TableCell>Position</TableCell>
                        <TableCell align="right">Quantity</TableCell>
                        <TableCell align="right">Current Price</TableCell>
                        <TableCell align="right">P&L</TableCell>
                        <TableCell align="center">Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {mockFuturesPositions.filter(pos => !pos.tradeGroupId).map((pos) => (
                        <TableRow key={pos.id}>
                          <TableCell>{pos.contractMonth}</TableCell>
                          <TableCell>
                            <Chip label="Futures" size="small" color="primary" />
                          </TableCell>
                          <TableCell>{ProductType[pos.productType]}</TableCell>
                          <TableCell>
                            <Chip
                              label={PositionType[pos.position]}
                              size="small"
                              color={pos.position === PositionType.Long ? 'success' : 'error'}
                            />
                          </TableCell>
                          <TableCell align="right">{tradeGroupUtils.formatQuantity(pos.quantity)}</TableCell>
                          <TableCell align="right">${pos.currentPrice.toFixed(2)}</TableCell>
                          <TableCell align="right">
                            <Typography sx={{ color: tradeGroupUtils.getPnLColor(pos.unrealizedPnL) }}>
                              {tradeGroupUtils.formatCurrency(pos.unrealizedPnL)}
                            </Typography>
                          </TableCell>
                          <TableCell align="center">
                            <Tooltip title="Assign to Trade Group">
                              <IconButton size="small">
                                <LinkIcon />
                              </IconButton>
                            </Tooltip>
                          </TableCell>
                        </TableRow>
                      ))}
                      {mockPhysicalPositions.filter(pos => !pos.tradeGroupId).map((pos) => (
                        <TableRow key={pos.id}>
                          <TableCell>{pos.contractNumber}</TableCell>
                          <TableCell>
                            <Chip label="Physical" size="small" color="secondary" />
                          </TableCell>
                          <TableCell>{ProductType[pos.productType]}</TableCell>
                          <TableCell>
                            <Chip
                              label={pos.contractType}
                              size="small"
                              color={pos.contractType === 'Purchase' ? 'primary' : 'secondary'}
                            />
                          </TableCell>
                          <TableCell align="right">{tradeGroupUtils.formatQuantity(pos.quantity)}</TableCell>
                          <TableCell align="right">${pos.price.toFixed(2)}</TableCell>
                          <TableCell align="right">—</TableCell>
                          <TableCell align="center">
                            <Tooltip title="Assign to Trade Group">
                              <IconButton size="small">
                                <LinkIcon />
                              </IconButton>
                            </Tooltip>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </Grid>
            </Grid>
          </AccordionDetails>
        </Accordion>
      </TabPanel>

      <TabPanel value={selectedTab} index={1}>
        <Typography variant="h6" gutterBottom>Basis Trading Opportunities</Typography>
        <Alert severity="info" sx={{ mb: 3 }}>
          These opportunities are identified by analyzing the spread between physical and futures prices,
          considering historical patterns and current market conditions.
        </Alert>

        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Product</TableCell>
                <TableCell>Contract Month</TableCell>
                <TableCell align="right">Physical Price</TableCell>
                <TableCell align="right">Futures Price</TableCell>
                <TableCell align="right">Basis</TableCell>
                <TableCell align="right">Z-Score</TableCell>
                <TableCell>Recommended Action</TableCell>
                <TableCell>Confidence</TableCell>
                <TableCell align="center">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {mockBasisOpportunities.map((opp) => (
                <TableRow key={opp.id}>
                  <TableCell>{ProductType[opp.productType]}</TableCell>
                  <TableCell>{opp.contractMonth}</TableCell>
                  <TableCell align="right">${opp.physicalPrice.toFixed(2)}</TableCell>
                  <TableCell align="right">${opp.futuresPrice.toFixed(2)}</TableCell>
                  <TableCell align="right">
                    <Typography sx={{ color: tradeGroupUtils.getPnLColor(opp.basis) }}>
                      ${opp.basis.toFixed(2)} ({opp.basisPoints}bp)
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography 
                      sx={{ 
                        color: Math.abs(opp.zScore) > 1.5 ? 'warning.main' : 'text.primary',
                        fontWeight: Math.abs(opp.zScore) > 2 ? 'bold' : 'normal'
                      }}
                    >
                      {opp.zScore.toFixed(2)}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={getActionLabel(opp.recommendedAction)}
                      size="small"
                      color={getActionColor(opp.recommendedAction)}
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={opp.confidence}
                      size="small"
                      color={getConfidenceColor(opp.confidence)}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Button
                      size="small"
                      variant="outlined"
                      startIcon={<SwapHorizIcon />}
                      disabled={opp.recommendedAction === 'HOLD'}
                    >
                      Execute
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </TabPanel>

      <TabPanel value={selectedTab} index={2}>
        <Alert severity="info" sx={{ mb: 3 }}>
          Risk management view showing correlation between futures and physical positions,
          hedge effectiveness, and integrated VaR calculations.
        </Alert>
        
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Hedge Effectiveness</Typography>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Correlation between futures and physical positions
                </Typography>
                <Typography variant="h4" color="success.main">
                  87.3%
                </Typography>
                <Typography variant="body2">
                  High correlation indicates effective hedging
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Basis Risk</Typography>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Standard deviation of basis over time
                </Typography>
                <Typography variant="h4" color="warning.main">
                  $0.82
                </Typography>
                <Typography variant="body2">
                  Per barrel basis volatility
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Integrated Risk Metrics</Typography>
                <Grid container spacing={3}>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Portfolio VaR (95%)</Typography>
                    <Typography variant="h6" color="error.main">
                      {portfolioRisk ? tradeGroupUtils.formatCurrency(Math.abs(portfolioRisk.portfolioVaR95)) : '—'}
                    </Typography>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Futures VaR</Typography>
                    <Typography variant="h6">$245,000</Typography>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Physical VaR</Typography>
                    <Typography variant="h6">$180,000</Typography>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Diversification Benefit</Typography>
                    <Typography variant="h6" color="success.main">-$125,000</Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={selectedTab} index={3}>
        <Alert severity="info" sx={{ mb: 3 }}>
          Performance analytics showing the effectiveness of integrated futures-spot strategies
          compared to standalone positions.
        </Alert>

        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Strategy Performance</Typography>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Last 30 days performance comparison
                </Typography>
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2">Integrated Strategies</Typography>
                  <Typography variant="h5" color="success.main">+2.8%</Typography>
                  <Typography variant="body2" sx={{ mt: 1 }}>Standalone Positions</Typography>
                  <Typography variant="h5" color="error.main">-1.2%</Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Risk-Adjusted Returns</Typography>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Sharpe ratio comparison
                </Typography>
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2">Integrated Portfolio</Typography>
                  <Typography variant="h5" color="success.main">1.34</Typography>
                  <Typography variant="body2" sx={{ mt: 1 }}>Market Benchmark</Typography>
                  <Typography variant="h5">0.89</Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      {/* Create Hedge Strategy Dialog */}
      <Dialog open={openHedgeDialog} onClose={() => setOpenHedgeDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>Create Integrated Hedge Strategy</DialogTitle>
        <DialogContent>
          <Alert severity="info" sx={{ mb: 3 }}>
            This dialog would allow creation of new hedge strategies that automatically link
            futures and physical positions within a TradeGroup.
          </Alert>
          
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Strategy Type</InputLabel>
                <Select defaultValue="">
                  <MenuItem value="BasisHedge">Basis Hedge</MenuItem>
                  <MenuItem value="CrossHedge">Cross Hedge</MenuItem>
                  <MenuItem value="CalendarSpread">Calendar Spread</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Product</InputLabel>
                <Select defaultValue="">
                  <MenuItem value="Brent">Brent Crude</MenuItem>
                  <MenuItem value="WTI">WTI Crude</MenuItem>
                  <MenuItem value="Gasoil">Gasoil</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Strategy Name"
                placeholder="e.g., Q1 2025 Brent Basis Hedge"
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                multiline
                rows={3}
                placeholder="Describe the hedge strategy objectives and risk management goals..."
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenHedgeDialog(false)}>Cancel</Button>
          <Button variant="contained">Create Strategy</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};