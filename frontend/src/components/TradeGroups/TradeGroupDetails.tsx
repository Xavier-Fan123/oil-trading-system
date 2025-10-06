import React, { useState } from 'react';
import {
  Box,
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
  Alert,
  CircularProgress,
  Tabs,
  Tab,
} from '@mui/material';
import {
  ArrowBack as ArrowBackIcon,
  Edit as EditIcon,
  Add as AddIcon,
  Remove as RemoveIcon,
  TrendingUp,
  TrendingDown,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  TradeGroupStatus,
  strategyTypeHelpers,
  riskLevelHelpers,
  tradeGroupStatusHelpers
} from '@/types/tradeGroups';
import { TagCategory } from '@/types/contracts';
import { 
  useTradeGroupManagement, 
  useTradeGroup
} from '@/hooks/useTradeGroups';
import { useTags } from '@/hooks/useTags';
import { tradeGroupUtils } from '@/services/tradeGroupApi';
import { tagCategoryHelpers } from '@/services/tagApi';

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
      id={`detail-tabpanel-${index}`}
      aria-labelledby={`detail-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

interface TradeGroupDetailsProps {
  tradeGroupId?: string;
}

export const TradeGroupDetails: React.FC<TradeGroupDetailsProps> = ({ 
  tradeGroupId: propTradeGroupId 
}) => {
  const { id: paramId } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const tradeGroupId = propTradeGroupId || paramId!;
  
  const [selectedTab, setSelectedTab] = useState(0);
  const [openAddTagDialog, setOpenAddTagDialog] = useState(false);
  const [selectedTagId, setSelectedTagId] = useState('');
  const [tagNotes, setTagNotes] = useState('');

  // API hooks
  const { data: tradeGroup, isLoading } = useTradeGroup(tradeGroupId);
  const {
    tags,
    isLoading: isLoadingTags,
    addTag,
    removeTag,
    isManagingTags
  } = useTradeGroupManagement(tradeGroupId);
  const { data: allTags } = useTags();

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (!tradeGroup) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        Trade group not found or failed to load.
      </Alert>
    );
  }

  const handleAddTag = () => {
    if (!selectedTagId.trim()) return;
    
    addTag(selectedTagId, tagNotes.trim() || undefined);
    setOpenAddTagDialog(false);
    setSelectedTagId('');
    setTagNotes('');
  };

  const handleRemoveTag = (tagId: string, tagName: string) => {
    if (window.confirm(`Are you sure you want to remove the tag "${tagName}" from this trade group?`)) {
      removeTag(tagId, 'Removed by user');
    }
  };

  // Filter available tags (exclude already assigned ones)
  const availableTags = allTags?.filter(tag => 
    !tags?.some(tradeGroupTag => tradeGroupTag.tagId === tag.id)
  ) || [];

  // Filter tags by category for better UX
  const tradingStrategyTags = availableTags.filter(tag => tag.category === TagCategory.TradingStrategy);
  const riskControlTags = availableTags.filter(tag => tag.category === TagCategory.RiskControl);
  const otherTags = availableTags.filter(tag => 
    tag.category !== TagCategory.TradingStrategy && 
    tag.category !== TagCategory.RiskControl
  );

  return (
    <Box>
      {/* Header */}
      <Box display="flex" alignItems="center" mb={3}>
        <IconButton onClick={() => navigate('/trade-groups')} sx={{ mr: 2 }}>
          <ArrowBackIcon />
        </IconButton>
        <Box flexGrow={1}>
          <Typography variant="h4" component="h1">
            {tradeGroupUtils.getStrategyIcon(tradeGroup.strategyType)} {tradeGroup.groupName}
          </Typography>
          <Typography variant="subtitle1" color="textSecondary">
            {strategyTypeHelpers.getDisplayName(tradeGroup.strategyType)} Strategy
          </Typography>
        </Box>
        <Button
          variant="outlined"
          startIcon={<EditIcon />}
          onClick={() => navigate(`/trade-groups/${tradeGroupId}/edit`)}
        >
          Edit
        </Button>
      </Box>

      {/* Status Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Status</Typography>
              <Chip
                label={tradeGroupStatusHelpers.getDisplayName(tradeGroup.status)}
                color={tradeGroup.status === TradeGroupStatus.Active ? 'success' : 'default'}
                sx={{ mb: 1 }}
              />
              {tradeGroup.expectedRiskLevel && (
                <Box>
                  <Chip
                    label={riskLevelHelpers.getDisplayName(tradeGroup.expectedRiskLevel)}
                    size="small"
                    sx={{ 
                      backgroundColor: riskLevelHelpers.getColor(tradeGroup.expectedRiskLevel),
                      color: 'white'
                    }}
                  />
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>P&L</Typography>
              <Box display="flex" alignItems="center">
                {tradeGroup.netPnL >= 0 ? (
                  <TrendingUp color="success" sx={{ mr: 1 }} />
                ) : (
                  <TrendingDown color="error" sx={{ mr: 1 }} />
                )}
                <Typography 
                  variant="h5" 
                  sx={{ color: tradeGroupUtils.getPnLColor(tradeGroup.netPnL) }}
                >
                  {tradeGroupUtils.formatCurrency(tradeGroup.netPnL)}
                </Typography>
              </Box>
              <Typography variant="body2" color="textSecondary">
                Unrealized P&L
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Portfolio Value</Typography>
              <Typography variant="h5" color="primary">
                {tradeGroupUtils.formatCurrency(tradeGroup.totalValue)}
              </Typography>
              <Typography variant="body2" color="textSecondary">
                Total Market Value
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>Contracts</Typography>
              <Typography variant="h5" color="primary">
                {tradeGroup.contractCount}
              </Typography>
              <Typography variant="body2" color="textSecondary">
                Active Positions
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Risk Warning */}
      {tradeGroupUtils.needsRiskWarning(tradeGroup) && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          <Box display="flex" alignItems="center">
            <WarningIcon sx={{ mr: 1 }} />
            <Typography variant="body2">
              This trade group has elevated risk levels. VaR: {tradeGroupUtils.formatCurrency(Math.abs(tradeGroup.riskMetrics.portfolioVaR95))} 
              {tradeGroup.maxAllowedLoss && tradeGroup.netPnL < -Math.abs(tradeGroup.maxAllowedLoss) && 
                ` | P&L below max allowed loss threshold`
              }
            </Typography>
          </Box>
        </Alert>
      )}

      {/* Tabs */}
      <Tabs value={selectedTab} onChange={(_, newValue) => setSelectedTab(newValue)} sx={{ mb: 3 }}>
        <Tab label="Contracts" />
        <Tab label="Risk Metrics" />
        <Tab label="Tags & Classification" />
        <Tab label="Performance" />
      </Tabs>

      <TabPanel value={selectedTab} index={0}>
        {/* Paper Contracts */}
        {tradeGroup.paperContracts.length > 0 && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>Paper Contracts</Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Contract Month</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell>Position</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="right">Entry Price</TableCell>
                      <TableCell align="right">Current Price</TableCell>
                      <TableCell align="right">Unrealized P&L</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {tradeGroup.paperContracts.map((contract) => (
                      <TableRow key={contract.id}>
                        <TableCell>{contract.contractMonth}</TableCell>
                        <TableCell>{contract.productType}</TableCell>
                        <TableCell>
                          <Chip
                            label={contract.position}
                            size="small"
                            color={contract.position === 'Long' ? 'success' : 'error'}
                          />
                        </TableCell>
                        <TableCell align="right">{tradeGroupUtils.formatQuantity(contract.quantity)}</TableCell>
                        <TableCell align="right">${contract.entryPrice.toFixed(2)}</TableCell>
                        <TableCell align="right">
                          {contract.currentPrice ? `$${contract.currentPrice.toFixed(2)}` : '—'}
                        </TableCell>
                        <TableCell align="right">
                          <Typography sx={{ color: tradeGroupUtils.getPnLColor(contract.unrealizedPnL) }}>
                            {tradeGroupUtils.formatCurrency(contract.unrealizedPnL)}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip label={contract.status} size="small" />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        )}

        {/* Purchase Contracts */}
        {tradeGroup.purchaseContracts.length > 0 && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>Purchase Contracts</Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Contract Number</TableCell>
                      <TableCell>Supplier</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell>Laycan</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {tradeGroup.purchaseContracts.map((contract) => (
                      <TableRow key={contract.id}>
                        <TableCell>{contract.contractNumber}</TableCell>
                        <TableCell>{contract.supplierName}</TableCell>
                        <TableCell>{contract.productName}</TableCell>
                        <TableCell align="right">{tradeGroupUtils.formatQuantity(contract.quantity)}</TableCell>
                        <TableCell>
                          {format(new Date(contract.laycanStart), 'MMM dd')} - {format(new Date(contract.laycanEnd), 'MMM dd')}
                        </TableCell>
                        <TableCell>
                          <Chip label={contract.status} size="small" />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        )}

        {/* Sales Contracts */}
        {tradeGroup.salesContracts.length > 0 && (
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>Sales Contracts</Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Contract Number</TableCell>
                      <TableCell>Customer</TableCell>
                      <TableCell>Product</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell>Laycan</TableCell>
                      <TableCell>Status</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {tradeGroup.salesContracts.map((contract) => (
                      <TableRow key={contract.id}>
                        <TableCell>{contract.contractNumber}</TableCell>
                        <TableCell>{contract.customerName}</TableCell>
                        <TableCell>{contract.productName}</TableCell>
                        <TableCell align="right">{tradeGroupUtils.formatQuantity(contract.quantity)}</TableCell>
                        <TableCell>
                          {format(new Date(contract.laycanStart), 'MMM dd')} - {format(new Date(contract.laycanEnd), 'MMM dd')}
                        </TableCell>
                        <TableCell>
                          <Chip label={contract.status} size="small" />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        )}

        {/* Empty state */}
        {tradeGroup.contractCount === 0 && (
          <Alert severity="info">
            No contracts assigned to this trade group yet. Use the contract assignment feature to add positions to this strategy.
          </Alert>
        )}
      </TabPanel>

      <TabPanel value={selectedTab} index={1}>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Value at Risk (VaR)</Typography>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="textSecondary">95% Confidence</Typography>
                    <Typography variant="h5" color="warning.main">
                      {tradeGroupUtils.formatCurrency(Math.abs(tradeGroup.riskMetrics.portfolioVaR95))}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="textSecondary">99% Confidence</Typography>
                    <Typography variant="h5" color="error.main">
                      {tradeGroupUtils.formatCurrency(Math.abs(tradeGroup.riskMetrics.portfolioVaR99))}
                    </Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Performance Metrics</Typography>
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="textSecondary">Sharpe Ratio</Typography>
                    <Typography variant="h5">
                      {tradeGroup.riskMetrics.sharpeRatio.toFixed(2)}
                    </Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="textSecondary">Max Drawdown</Typography>
                    <Typography variant="h5" color="error.main">
                      {tradeGroupUtils.formatPercentage(tradeGroup.riskMetrics.maxDrawdown)}
                    </Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Risk Breakdown</Typography>
                <Grid container spacing={3}>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Volatility</Typography>
                    <Typography variant="h6">
                      {tradeGroupUtils.formatPercentage(tradeGroup.riskMetrics.volatility)}
                    </Typography>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Beta</Typography>
                    <Typography variant="h6">{tradeGroup.riskMetrics.beta.toFixed(2)}</Typography>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Concentration Risk</Typography>
                    <Typography variant="h6" color="warning.main">
                      {tradeGroupUtils.formatPercentage(tradeGroup.riskMetrics.concentrationRisk)}
                    </Typography>
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <Typography variant="body2" color="textSecondary">Leverage Ratio</Typography>
                    <Typography variant="h6">{tradeGroup.riskMetrics.leverageRatio.toFixed(2)}</Typography>
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={selectedTab} index={2}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h6">Associated Tags</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setOpenAddTagDialog(true)}
            disabled={availableTags.length === 0}
          >
            Add Tag
          </Button>
        </Box>

        {isLoadingTags ? (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        ) : (
          <Grid container spacing={2}>
            {tags?.map((tradeGroupTag) => (
              <Grid item key={tradeGroupTag.id}>
                <Card variant="outlined">
                  <CardContent sx={{ pb: 2, '&:last-child': { pb: 2 } }}>
                    <Box display="flex" alignItems="center" justifyContent="space-between">
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={tradeGroupTag.tagColor}
                          borderRadius="50%"
                          mr={1}
                        />
                        <Box>
                          <Typography variant="body2" fontWeight="medium">
                            {tradeGroupTag.tagName}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {tradeGroupTag.tagCategory}
                          </Typography>
                        </Box>
                      </Box>
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleRemoveTag(tradeGroupTag.tagId, tradeGroupTag.tagName)}
                        disabled={isManagingTags}
                      >
                        <RemoveIcon />
                      </IconButton>
                    </Box>
                    {tradeGroupTag.notes && (
                      <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                        {tradeGroupTag.notes}
                      </Typography>
                    )}
                    <Typography variant="caption" color="textSecondary" display="block">
                      Added by {tradeGroupTag.assignedBy} on {format(new Date(tradeGroupTag.assignedAt), 'MMM dd, yyyy')}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
            
            {(!tags || tags.length === 0) && (
              <Grid item xs={12}>
                <Alert severity="info">
                  No tags assigned to this trade group. Add tags to classify strategy, risk level, and compliance status.
                </Alert>
              </Grid>
            )}
          </Grid>
        )}
      </TabPanel>

      <TabPanel value={selectedTab} index={3}>
        <Alert severity="info" sx={{ mb: 3 }}>
          Performance analytics including historical P&L, returns analysis, and strategy effectiveness metrics will be displayed here.
        </Alert>
        
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Target vs Actual</Typography>
                {tradeGroup.targetProfit && (
                  <Box>
                    <Typography variant="body2" color="textSecondary">Target Profit</Typography>
                    <Typography variant="h5" color="success.main">
                      {tradeGroupUtils.formatCurrency(tradeGroup.targetProfit)}
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Progress: {tradeGroupUtils.formatPercentage((tradeGroup.netPnL / tradeGroup.targetProfit) * 100)}
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Risk Limits</Typography>
                {tradeGroup.maxAllowedLoss && (
                  <Box>
                    <Typography variant="body2" color="textSecondary">Max Allowed Loss</Typography>
                    <Typography variant="h5" color="error.main">
                      {tradeGroupUtils.formatCurrency(Math.abs(tradeGroup.maxAllowedLoss))}
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      Used: {tradeGroupUtils.formatPercentage((Math.abs(Math.min(tradeGroup.netPnL, 0)) / Math.abs(tradeGroup.maxAllowedLoss)) * 100)}
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      {/* Add Tag Dialog */}
      <Dialog open={openAddTagDialog} onClose={() => setOpenAddTagDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Tag to Trade Group</DialogTitle>
        <DialogContent>
          <FormControl fullWidth sx={{ mt: 2, mb: 2 }}>
            <InputLabel>Select Tag</InputLabel>
            <Select
              value={selectedTagId}
              label="Select Tag"
              onChange={(e) => setSelectedTagId(e.target.value)}
            >
              {tradingStrategyTags.length > 0 && (
                <>
                  <MenuItem disabled>
                    <Typography variant="body2" fontWeight="bold" color="primary">
                      Trading Strategy Tags
                    </Typography>
                  </MenuItem>
                  {tradingStrategyTags.map(tag => (
                    <MenuItem key={tag.id} value={tag.id}>
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={tag.color}
                          borderRadius="50%"
                          mr={1}
                        />
                        {tag.name}
                      </Box>
                    </MenuItem>
                  ))}
                </>
              )}
              
              {riskControlTags.length > 0 && (
                <>
                  <MenuItem disabled>
                    <Typography variant="body2" fontWeight="bold" color="primary">
                      Risk Control Tags
                    </Typography>
                  </MenuItem>
                  {riskControlTags.map(tag => (
                    <MenuItem key={tag.id} value={tag.id}>
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={tag.color}
                          borderRadius="50%"
                          mr={1}
                        />
                        {tag.name}
                      </Box>
                    </MenuItem>
                  ))}
                </>
              )}
              
              {otherTags.length > 0 && (
                <>
                  <MenuItem disabled>
                    <Typography variant="body2" fontWeight="bold" color="primary">
                      Other Tags
                    </Typography>
                  </MenuItem>
                  {otherTags.map(tag => (
                    <MenuItem key={tag.id} value={tag.id}>
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={tag.color}
                          borderRadius="50%"
                          mr={1}
                        />
                        {tag.name} ({tagCategoryHelpers.getCategoryDisplayName(tag.category)})
                      </Box>
                    </MenuItem>
                  ))}
                </>
              )}
            </Select>
          </FormControl>
          
          <TextField
            fullWidth
            label="Notes (Optional)"
            value={tagNotes}
            onChange={(e) => setTagNotes(e.target.value)}
            multiline
            rows={2}
            placeholder="Add any notes about why this tag is being applied..."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenAddTagDialog(false)}>Cancel</Button>
          <Button 
            onClick={handleAddTag} 
            variant="contained"
            disabled={!selectedTagId || isManagingTags}
          >
            {isManagingTags ? 'Adding...' : 'Add Tag'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};