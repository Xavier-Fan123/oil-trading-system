import React, { useState } from 'react';
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
  LinearProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Close as CloseIcon,
  Refresh as RefreshIcon,
  TrendingUp,
  TrendingDown,
  Assessment as AssessmentIcon,
  AccountBalance as AccountBalanceIcon,
  Warning as WarningIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { 
  StrategyType,
  TradeGroupStatus,
  RiskLevel,
  CreateTradeGroupDto,
  UpdateTradeGroupDto,
  TradeGroupDto,
  strategyTypeHelpers,
  riskLevelHelpers,
  tradeGroupStatusHelpers
} from '@/types/tradeGroups';
import { useTradeGroups, useCreateTradeGroup, useUpdateTradeGroup, useCloseTradeGroup, usePortfolioRiskWithTradeGroups } from '@/hooks/useTradeGroups';
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
      id={`tradegroup-tabpanel-${index}`}
      aria-labelledby={`tradegroup-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const TradeGroupManagement: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState(0);
  const [openCreateDialog, setOpenCreateDialog] = useState(false);
  const [openEditDialog, setOpenEditDialog] = useState(false);
  const [editingTradeGroup, setEditingTradeGroup] = useState<TradeGroupDto | null>(null);
  const [formData, setFormData] = useState<CreateTradeGroupDto>({
    groupName: '',
    strategyType: 'Directional',
    description: '',
    expectedRiskLevel: 'Medium',
    maxAllowedLoss: undefined,
    targetProfit: undefined,
  });

  // API hooks
  const { data: tradeGroups, isLoading: loadingTradeGroups, refetch: refetchTradeGroups } = useTradeGroups();
  const { data: portfolioRisk, isLoading: loadingPortfolioRisk } = usePortfolioRiskWithTradeGroups();
  const createTradeGroupMutation = useCreateTradeGroup();
  const updateTradeGroupMutation = useUpdateTradeGroup();
  const closeTradeGroupMutation = useCloseTradeGroup();

  const resetForm = () => {
    setFormData({
      groupName: '',
      strategyType: 'Directional',
      description: '',
      expectedRiskLevel: 'Medium',
      maxAllowedLoss: undefined,
      targetProfit: undefined,
    });
  };

  const handleCreateTradeGroup = () => {
    createTradeGroupMutation.mutate(formData, {
      onSuccess: () => {
        setOpenCreateDialog(false);
        resetForm();
      }
    });
  };

  const handleUpdateTradeGroup = () => {
    if (!editingTradeGroup) return;
    
    const updateDto: UpdateTradeGroupDto = {
      groupName: formData.groupName !== editingTradeGroup.groupName ? formData.groupName : undefined,
      description: formData.description,
      expectedRiskLevel: formData.expectedRiskLevel,
      maxAllowedLoss: formData.maxAllowedLoss,
      targetProfit: formData.targetProfit,
    };

    updateTradeGroupMutation.mutate({ id: editingTradeGroup.id, dto: updateDto }, {
      onSuccess: () => {
        setOpenEditDialog(false);
        setEditingTradeGroup(null);
        resetForm();
      }
    });
  };

  const handleEditTradeGroup = (tradeGroup: TradeGroupDto) => {
    setEditingTradeGroup(tradeGroup);
    setFormData({
      groupName: tradeGroup.groupName,
      strategyType: StrategyType[tradeGroup.strategyType],
      description: tradeGroup.description || '',
      expectedRiskLevel: tradeGroup.expectedRiskLevel ? RiskLevel[tradeGroup.expectedRiskLevel] : 'Medium',
      maxAllowedLoss: tradeGroup.maxAllowedLoss,
      targetProfit: tradeGroup.targetProfit,
    });
    setOpenEditDialog(true);
  };

  const handleCloseTradeGroup = (tradeGroupId: string) => {
    if (window.confirm('Are you sure you want to close this trade group? This action cannot be undone.')) {
      closeTradeGroupMutation.mutate(tradeGroupId);
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
          Trade Group Management
        </Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => refetchTradeGroups()}
            sx={{ mr: 2 }}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setOpenCreateDialog(true)}
          >
            New Trade Group
          </Button>
        </Box>
      </Box>

      <Tabs value={selectedTab} onChange={(_, newValue) => setSelectedTab(newValue)} sx={{ mb: 3 }}>
        <Tab label="Manage Trade Groups" />
        <Tab label="Portfolio Risk Overview" />
        <Tab label="Strategy Analytics" />
      </Tabs>

      <TabPanel value={selectedTab} index={0}>
        {/* Trade Groups Table */}
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Strategy</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Risk Level</TableCell>
                <TableCell align="right">Contracts</TableCell>
                <TableCell align="right">P&L</TableCell>
                <TableCell align="right">Value</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="center">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {tradeGroups?.map((tradeGroup) => (
                <TableRow key={tradeGroup.id} hover>
                  <TableCell>
                    <Box display="flex" alignItems="center">
                      <Typography variant="body2" fontWeight="medium">
                        {tradeGroupUtils.getStrategyIcon(tradeGroup.strategyType)} {tradeGroup.groupName}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={strategyTypeHelpers.getDisplayName(tradeGroup.strategyType)}
                      size="small"
                      sx={{ 
                        backgroundColor: strategyTypeHelpers.getColor(tradeGroup.strategyType),
                        color: 'white'
                      }}
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={tradeGroupStatusHelpers.getDisplayName(tradeGroup.status)}
                      size="small"
                      color={tradeGroup.status === TradeGroupStatus.Active ? 'success' : 'default'}
                    />
                  </TableCell>
                  <TableCell>
                    {tradeGroup.expectedRiskLevel && (
                      <Chip
                        label={riskLevelHelpers.getDisplayName(tradeGroup.expectedRiskLevel)}
                        size="small"
                        sx={{ 
                          backgroundColor: riskLevelHelpers.getColor(tradeGroup.expectedRiskLevel),
                          color: 'white'
                        }}
                      />
                    )}
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2">
                      0 {/* This would come from the detailed DTO */}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography 
                      variant="body2"
                      sx={{ color: tradeGroupUtils.getPnLColor(0) }}
                    >
                      {tradeGroupUtils.formatCurrency(0)}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2">
                      {tradeGroupUtils.formatCurrency(0)}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {format(new Date(tradeGroup.createdAt), 'MMM dd, yyyy')}
                    </Typography>
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Edit">
                      <IconButton size="small" onClick={() => handleEditTradeGroup(tradeGroup)}>
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    {tradeGroup.status === TradeGroupStatus.Active && (
                      <Tooltip title="Close">
                        <IconButton 
                          size="small" 
                          color="error"
                          onClick={() => handleCloseTradeGroup(tradeGroup.id)}
                        >
                          <CloseIcon />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))}
              {(!tradeGroups || tradeGroups.length === 0) && (
                <TableRow>
                  <TableCell colSpan={9} align="center">
                    <Typography variant="body2" color="textSecondary" py={4}>
                      No trade groups found. Create your first trade group to start managing complex trading strategies.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </TabPanel>

      <TabPanel value={selectedTab} index={1}>
        {portfolioRisk && (
          <Grid container spacing={3}>
            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" mb={1}>
                    <AccountBalanceIcon color="primary" sx={{ mr: 1 }} />
                    <Typography variant="h6">Portfolio Overview</Typography>
                  </Box>
                  <Typography variant="h4" color="primary">
                    {portfolioRisk.totalTradeGroups}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    Total Trade Groups
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    Active: {portfolioRisk.activeTradeGroups}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" mb={1}>
                    <AssessmentIcon color="success" sx={{ mr: 1 }} />
                    <Typography variant="h6">Total Value</Typography>
                  </Box>
                  <Typography variant="h4" color="success.main">
                    {tradeGroupUtils.formatCurrency(portfolioRisk.totalValue)}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    Market Value
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" mb={1}>
                    {portfolioRisk.totalPnL >= 0 ? (
                      <TrendingUp color="success" sx={{ mr: 1 }} />
                    ) : (
                      <TrendingDown color="error" sx={{ mr: 1 }} />
                    )}
                    <Typography variant="h6">Total P&L</Typography>
                  </Box>
                  <Typography 
                    variant="h4" 
                    sx={{ color: tradeGroupUtils.getPnLColor(portfolioRisk.totalPnL) }}
                  >
                    {tradeGroupUtils.formatCurrency(portfolioRisk.totalPnL)}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    Unrealized P&L
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={3}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" mb={1}>
                    <WarningIcon color="warning" sx={{ mr: 1 }} />
                    <Typography variant="h6">Portfolio VaR</Typography>
                  </Box>
                  <Typography variant="h4" color="warning.main">
                    {tradeGroupUtils.formatCurrency(Math.abs(portfolioRisk.portfolioVaR95))}
                  </Typography>
                  <Typography variant="body2" color="textSecondary">
                    95% Confidence
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    99%: {tradeGroupUtils.formatCurrency(Math.abs(portfolioRisk.portfolioVaR99))}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Trade Group Summaries
                  </Typography>
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Name</TableCell>
                          <TableCell>Strategy</TableCell>
                          <TableCell>Status</TableCell>
                          <TableCell align="right">Contracts</TableCell>
                          <TableCell align="right">P&L</TableCell>
                          <TableCell align="right">VaR 95%</TableCell>
                          <TableCell>Last Updated</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {portfolioRisk.tradeGroupSummaries?.map((summary) => (
                          <TableRow key={summary.id}>
                            <TableCell>{summary.groupName}</TableCell>
                            <TableCell>
                              <Chip
                                label={strategyTypeHelpers.getDisplayName(summary.strategyType)}
                                size="small"
                                sx={{ 
                                  backgroundColor: strategyTypeHelpers.getColor(summary.strategyType),
                                  color: 'white'
                                }}
                              />
                            </TableCell>
                            <TableCell>
                              <Chip
                                label={tradeGroupStatusHelpers.getDisplayName(summary.status)}
                                size="small"
                                color={summary.status === TradeGroupStatus.Active ? 'success' : 'default'}
                              />
                            </TableCell>
                            <TableCell align="right">{summary.contractCount}</TableCell>
                            <TableCell align="right">
                              <Typography sx={{ color: tradeGroupUtils.getPnLColor(summary.netPnL) }}>
                                {tradeGroupUtils.formatCurrency(summary.netPnL)}
                              </Typography>
                            </TableCell>
                            <TableCell align="right">
                              {tradeGroupUtils.formatCurrency(Math.abs(summary.var95))}
                            </TableCell>
                            <TableCell>
                              {format(new Date(summary.lastUpdated), 'MMM dd HH:mm')}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        )}
      </TabPanel>

      <TabPanel value={selectedTab} index={2}>
        <Alert severity="info" sx={{ mb: 3 }}>
          Strategy analytics and performance metrics will be displayed here. This includes correlation analysis, 
          diversification scores, and strategy performance comparisons.
        </Alert>
        
        {tradeGroups && (
          <Grid container spacing={3}>
            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>Strategy Distribution</Typography>
                  {strategyTypeHelpers.getAllStrategies().map((strategy) => {
                    const count = tradeGroups.filter(tg => tg.strategyType === strategy.value).length;
                    const percentage = tradeGroups.length > 0 ? (count / tradeGroups.length) * 100 : 0;
                    
                    return (
                      <Box key={strategy.value} sx={{ mb: 2 }}>
                        <Box display="flex" justifyContent="space-between" mb={1}>
                          <Typography variant="body2">{strategy.label}</Typography>
                          <Typography variant="body2">{count} ({percentage.toFixed(1)}%)</Typography>
                        </Box>
                        <LinearProgress 
                          variant="determinate" 
                          value={percentage} 
                          sx={{ 
                            height: 8, 
                            borderRadius: 4,
                            backgroundColor: 'grey.200',
                            '& .MuiLinearProgress-bar': {
                              backgroundColor: strategy.color
                            }
                          }}
                        />
                      </Box>
                    );
                  })}
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12} md={6}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>Portfolio Diversification</Typography>
                  <Box textAlign="center" py={2}>
                    <Typography variant="h3" color="primary">
                      {tradeGroupUtils.calculateDiversificationScore(tradeGroups || []).toFixed(0)}%
                    </Typography>
                    <Typography variant="body2" color="textSecondary">
                      Diversification Score
                    </Typography>
                  </Box>
                  <Typography variant="body2" sx={{ mt: 2 }}>
                    Based on strategy variety and risk distribution across your trade groups.
                    Higher scores indicate better diversification.
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          </Grid>
        )}
      </TabPanel>

      {/* Create Trade Group Dialog */}
      <Dialog open={openCreateDialog} onClose={() => setOpenCreateDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>Create New Trade Group</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Group Name"
                value={formData.groupName}
                onChange={(e) => setFormData(prev => ({ ...prev, groupName: e.target.value }))}
                required
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Strategy Type</InputLabel>
                <Select
                  value={formData.strategyType}
                  label="Strategy Type"
                  onChange={(e) => setFormData(prev => ({ ...prev, strategyType: e.target.value }))}
                >
                  {strategyTypeHelpers.getAllStrategies().map(strategy => (
                    <MenuItem key={strategy.value} value={StrategyType[strategy.value]}>
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={strategy.color}
                          borderRadius="50%"
                          mr={1}
                        />
                        {strategy.label}
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Expected Risk Level</InputLabel>
                <Select
                  value={formData.expectedRiskLevel}
                  label="Expected Risk Level"
                  onChange={(e) => setFormData(prev => ({ ...prev, expectedRiskLevel: e.target.value }))}
                >
                  <MenuItem value="Low">
                    <Box display="flex" alignItems="center">
                      <Box width={12} height={12} bgcolor="#059669" borderRadius="50%" mr={1} />
                      Low Risk
                    </Box>
                  </MenuItem>
                  <MenuItem value="Medium">
                    <Box display="flex" alignItems="center">
                      <Box width={12} height={12} bgcolor="#D97706" borderRadius="50%" mr={1} />
                      Medium Risk
                    </Box>
                  </MenuItem>
                  <MenuItem value="High">
                    <Box display="flex" alignItems="center">
                      <Box width={12} height={12} bgcolor="#DC2626" borderRadius="50%" mr={1} />
                      High Risk
                    </Box>
                  </MenuItem>
                  <MenuItem value="VeryHigh">
                    <Box display="flex" alignItems="center">
                      <Box width={12} height={12} bgcolor="#7C2D12" borderRadius="50%" mr={1} />
                      Very High Risk
                    </Box>
                  </MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={formData.description}
                onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Max Allowed Loss (USD)"
                type="number"
                value={formData.maxAllowedLoss || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, maxAllowedLoss: e.target.value ? parseFloat(e.target.value) : undefined }))}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Target Profit (USD)"
                type="number"
                value={formData.targetProfit || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, targetProfit: e.target.value ? parseFloat(e.target.value) : undefined }))}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenCreateDialog(false)}>Cancel</Button>
          <Button 
            onClick={handleCreateTradeGroup} 
            variant="contained"
            disabled={!formData.groupName.trim() || createTradeGroupMutation.isPending}
          >
            {createTradeGroupMutation.isPending ? 'Creating...' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Trade Group Dialog */}
      <Dialog open={openEditDialog} onClose={() => setOpenEditDialog(false)} maxWidth="md" fullWidth>
        <DialogTitle>Edit Trade Group</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Group Name"
                value={formData.groupName}
                onChange={(e) => setFormData(prev => ({ ...prev, groupName: e.target.value }))}
                required
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                value={formData.description}
                onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                multiline
                rows={3}
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <FormControl fullWidth>
                <InputLabel>Expected Risk Level</InputLabel>
                <Select
                  value={formData.expectedRiskLevel}
                  label="Expected Risk Level"
                  onChange={(e) => setFormData(prev => ({ ...prev, expectedRiskLevel: e.target.value }))}
                >
                  <MenuItem value="Low">Low Risk</MenuItem>
                  <MenuItem value="Medium">Medium Risk</MenuItem>
                  <MenuItem value="High">High Risk</MenuItem>
                  <MenuItem value="VeryHigh">Very High Risk</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                label="Max Allowed Loss (USD)"
                type="number"
                value={formData.maxAllowedLoss || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, maxAllowedLoss: e.target.value ? parseFloat(e.target.value) : undefined }))}
              />
            </Grid>
            <Grid item xs={12} sm={4}>
              <TextField
                fullWidth
                label="Target Profit (USD)"
                type="number"
                value={formData.targetProfit || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, targetProfit: e.target.value ? parseFloat(e.target.value) : undefined }))}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenEditDialog(false)}>Cancel</Button>
          <Button 
            onClick={handleUpdateTradeGroup} 
            variant="contained"
            disabled={!formData.groupName.trim() || updateTradeGroupMutation.isPending}
          >
            {updateTradeGroupMutation.isPending ? 'Updating...' : 'Update'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};