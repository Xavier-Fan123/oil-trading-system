import React, { useState, useEffect, useMemo } from 'react';
import {
  Box,
  Typography,
  Grid,
  Card,
  CardContent,
  CardHeader,
  Chip,
  CircularProgress,
  Button,
  Tabs,
  Tab,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import {
  Edit as EditIcon,
  ArrowBack as ArrowBackIcon,
  Assignment,
  Timeline,
  LocalShipping,
  Receipt,
  Info,
  Download,
  Share,
  Print,
  Note,
  AccountBalance as SettlementIcon,
  History as HistoryIcon,
  Circle as CircleIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useNavigate } from 'react-router-dom';
import { usePurchaseContract } from '@/hooks/useContracts';
import { ContractTagSelector } from '@/components/Tags/ContractTagSelector';
import { ContractWorkflow } from './ContractWorkflow';
import { contractMatchingApi } from '@/services/contractMatchingApi';
import { shippingApi } from '@/services/shippingApi';
import { settlementApi } from '@/services/settlementApi';
import type { PurchaseMatching } from '@/services/contractMatchingApi';
import type { ShippingOperationDto } from '@/types/shipping';
import type { ContractSettlementDto } from '@/types/settlement';
import {
  ContractStatus,
  QuantityUnit,
  PricingType,
  DeliveryTerms,
  SettlementType,
} from '@/types/contracts';

interface EnhancedContractDetailProps {
  contractId: string;
  onEdit: () => void;
  onBack: () => void;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index, ...other }) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`contract-tabpanel-${index}`}
      aria-labelledby={`contract-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  );
};

const getStatusColor = (status: ContractStatus): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
  switch (status) {
    case ContractStatus.Draft: return 'default';
    case ContractStatus.PendingApproval: return 'warning';
    case ContractStatus.Active: return 'success';
    case ContractStatus.Completed: return 'info';
    case ContractStatus.Cancelled: return 'error';
    default: return 'default';
  }
};

const getStatusLabel = (status: ContractStatus): string => {
  switch (status) {
    case ContractStatus.Draft: return 'Draft';
    case ContractStatus.PendingApproval: return 'Pending Approval';
    case ContractStatus.Active: return 'Active';
    case ContractStatus.Completed: return 'Completed';
    case ContractStatus.Cancelled: return 'Cancelled';
    default: return 'Unknown';
  }
};

const getQuantityUnitLabel = (unit: QuantityUnit): string => {
  switch (unit) {
    case QuantityUnit.MT: return 'MT';
    case QuantityUnit.BBL: return 'BBL';
    case QuantityUnit.GAL: return 'GAL';
    default: return 'Unknown';
  }
};

const getPricingTypeLabel = (type: PricingType): string => {
  switch (type) {
    case PricingType.Fixed: return 'Fixed Price';
    case PricingType.Floating: return 'Floating Price';
    case PricingType.Formula: return 'Formula-based';
    default: return 'Unknown';
  }
};

const getDeliveryTermsLabel = (terms: DeliveryTerms): string => {
  switch (terms) {
    case DeliveryTerms.FOB: return 'FOB (Free on Board)';
    case DeliveryTerms.CIF: return 'CIF (Cost, Insurance & Freight)';
    case DeliveryTerms.CFR: return 'CFR (Cost and Freight)';
    case DeliveryTerms.DAP: return 'DAP (Delivered at Place)';
    case DeliveryTerms.DDP: return 'DDP (Delivered Duty Paid)';
    default: return 'Unknown';
  }
};

const getSettlementTypeLabel = (type: SettlementType): string => {
  switch (type) {
    case SettlementType.TT: return 'TT (Telegraphic Transfer)';
    case SettlementType.LC: return 'LC (Letter of Credit)';
    case SettlementType.CAD: return 'CAD (Cash Against Documents)';
    default: return 'Unknown';
  }
};

const getSettlementStatusColor = (status: string): 'default' | 'info' | 'warning' | 'success' => {
  switch (status) {
    case 'Draft': return 'default';
    case 'Calculated': return 'info';
    case 'Approved': return 'warning';
    case 'Finalized': return 'success';
    default: return 'default';
  }
};

interface TimelineEvent {
  date: Date;
  label: string;
  detail: string;
  type: 'contract' | 'matching' | 'shipping' | 'settlement';
  color: string;
}

const EVENT_COLORS: Record<TimelineEvent['type'], string> = {
  contract: '#42a5f5',
  matching: '#ab47bc',
  shipping: '#26a69a',
  settlement: '#ff7043',
};

const HistoryTimeline: React.FC<{
  contract: any;
  matchings: PurchaseMatching[];
  shippingOps: ShippingOperationDto[];
  settlements: ContractSettlementDto[];
}> = ({ contract, matchings, shippingOps, settlements }) => {
  const events = useMemo(() => {
    const items: TimelineEvent[] = [];

    // Contract creation
    if (contract.createdAt) {
      items.push({
        date: new Date(contract.createdAt),
        label: 'Contract Created',
        detail: `${contract.contractNumber?.value || 'New Contract'} created${contract.createdBy ? ` by ${contract.createdBy}` : ''}`,
        type: 'contract',
        color: EVENT_COLORS.contract,
      });
    }

    // Contract last updated (if different from created)
    if (contract.updatedAt && contract.createdAt && new Date(contract.updatedAt).getTime() !== new Date(contract.createdAt).getTime()) {
      items.push({
        date: new Date(contract.updatedAt),
        label: `Contract Updated`,
        detail: `Status: ${getStatusLabel(contract.status)}${contract.updatedBy ? ` | By: ${contract.updatedBy}` : ''}`,
        type: 'contract',
        color: EVENT_COLORS.contract,
      });
    }

    // Matchings
    matchings.forEach(m => {
      items.push({
        date: new Date(m.matchedDate),
        label: 'Contract Matched',
        detail: `Matched ${m.matchedQuantity.toLocaleString()} MT with ${m.salesContractNumber} (${m.salesTradingPartner})`,
        type: 'matching',
        color: EVENT_COLORS.matching,
      });
    });

    // Shipping operations
    shippingOps.forEach(op => {
      const opDate = op.createdAt ? new Date(op.createdAt) : new Date();
      items.push({
        date: opDate,
        label: `Shipping: ${shippingApi.getStatusLabel(Number(op.status))}`,
        detail: `${op.vesselName || 'TBN'} | ${op.loadPort} → ${op.dischargePort}`,
        type: 'shipping',
        color: EVENT_COLORS.shipping,
      });
    });

    // Settlements
    settlements.forEach(s => {
      const sDate = s.createdDate ? new Date(s.createdDate) : (s.documentDate ? new Date(s.documentDate) : new Date());
      items.push({
        date: sDate,
        label: `Settlement: ${s.status || 'Draft'}`,
        detail: `${s.documentNumber || 'Pending'}${s.totalSettlementAmount ? ` | $${s.totalSettlementAmount.toLocaleString()}` : ''}${s.createdBy ? ` | By: ${s.createdBy}` : ''}`,
        type: 'settlement',
        color: EVENT_COLORS.settlement,
      });

      // Settlement finalized event
      if (s.finalizedDate) {
        items.push({
          date: new Date(s.finalizedDate),
          label: 'Settlement Finalized',
          detail: `${s.documentNumber || ''}${s.finalizedBy ? ` | Finalized by: ${s.finalizedBy}` : ''}`,
          type: 'settlement',
          color: EVENT_COLORS.settlement,
        });
      }
    });

    return items.sort((a, b) => a.date.getTime() - b.date.getTime());
  }, [contract, matchings, shippingOps, settlements]);

  if (events.length === 0) {
    return (
      <Alert severity="info">No history events found for this contract.</Alert>
    );
  }

  return (
    <Box>
      {/* Legend */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        {Object.entries(EVENT_COLORS).map(([type, color]) => (
          <Box key={type} sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            <CircleIcon sx={{ fontSize: 10, color }} />
            <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'capitalize' }}>
              {type}
            </Typography>
          </Box>
        ))}
      </Box>

      {/* Timeline */}
      {events.map((event, index) => (
        <Box key={index} sx={{ display: 'flex', mb: 0 }}>
          {/* Timeline line + dot */}
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mr: 2, minWidth: 20 }}>
            <CircleIcon sx={{ fontSize: 12, color: event.color, zIndex: 1 }} />
            {index < events.length - 1 && (
              <Box sx={{ width: 2, flexGrow: 1, backgroundColor: '#2a2d3a', minHeight: 40 }} />
            )}
          </Box>

          {/* Content */}
          <Box sx={{ pb: 2.5, flex: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, flexWrap: 'wrap' }}>
              <Typography variant="subtitle2" sx={{ color: event.color }}>
                {event.label}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {format(event.date, 'MMM dd, yyyy HH:mm')}
              </Typography>
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
              {event.detail}
            </Typography>
          </Box>
        </Box>
      ))}
    </Box>
  );
};

export const EnhancedContractDetail: React.FC<EnhancedContractDetailProps> = ({
  contractId,
  onEdit,
  onBack,
}) => {
  const navigate = useNavigate();
  const { data: contract, isLoading, error, refetch } = usePurchaseContract(contractId);
  const [activeTab, setActiveTab] = useState(0);
  const [noteDialog, setNoteDialog] = useState({ open: false, note: '' });

  // Real data states
  const [matchings, setMatchings] = useState<PurchaseMatching[]>([]);
  const [matchingsLoading, setMatchingsLoading] = useState(false);
  const [shippingOps, setShippingOps] = useState<ShippingOperationDto[]>([]);
  const [shippingLoading, setShippingLoading] = useState(false);
  const [settlements, setSettlements] = useState<ContractSettlementDto[]>([]);
  const [settlementsLoading, setSettlementsLoading] = useState(false);

  // Load linked data when contract is available
  useEffect(() => {
    if (!contractId) return;

    // Load contract matchings
    setMatchingsLoading(true);
    contractMatchingApi.getPurchaseMatchings(contractId)
      .then(data => setMatchings(data))
      .catch(() => setMatchings([]))
      .finally(() => setMatchingsLoading(false));

    // Load shipping operations
    setShippingLoading(true);
    shippingApi.getByContractId(contractId)
      .then(data => setShippingOps(data))
      .catch(() => setShippingOps([]))
      .finally(() => setShippingLoading(false));

    // Load settlements
    setSettlementsLoading(true);
    settlementApi.getByContractId(contractId)
      .then(data => setSettlements(data))
      .catch(() => setSettlements([]))
      .finally(() => setSettlementsLoading(false));
  }, [contractId]);

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleStatusChange = async (newStatus: ContractStatus, notes?: string) => {
    try {
      console.log('Changing contract status:', { contractId, newStatus, notes });
      await refetch();
    } catch (error) {
      console.error('Failed to update contract status:', error);
      throw error;
    }
  };

  const handleAddNote = async () => {
    try {
      console.log('Adding note to contract:', { contractId, note: noteDialog.note });
      setNoteDialog({ open: false, note: '' });
      await refetch();
    } catch (error) {
      console.error('Failed to add note:', error);
    }
  };

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (error || !contract) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <Alert severity="error">
          Error loading contract details
          <Button onClick={() => refetch()} sx={{ ml: 2 }}>
            Retry
          </Button>
        </Alert>
      </Box>
    );
  }

  const canEdit = contract.status === ContractStatus.Draft || contract.status === ContractStatus.PendingApproval;

  return (
    <Box>
      {/* Header */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Box display="flex" alignItems="center">
          <Button
            startIcon={<ArrowBackIcon />}
            onClick={onBack}
            sx={{ mr: 2 }}
          >
            Back to List
          </Button>
          <Box>
            <Typography variant="h4" component="h1">
              Contract Details
            </Typography>
            <Typography variant="h6" color="primary" fontWeight="bold">
              {contract.contractNumber.value}
            </Typography>
            {contract.externalContractNumber && (
              <Typography variant="body2" color="text.secondary">
                External: {contract.externalContractNumber}
              </Typography>
            )}
          </Box>
        </Box>
        <Box display="flex" alignItems="center" gap={1}>
          <Chip
            label={getStatusLabel(contract.status)}
            color={getStatusColor(contract.status)}
            size="medium"
          />
          <Tooltip title="Download Contract">
            <IconButton>
              <Download />
            </IconButton>
          </Tooltip>
          <Tooltip title="Share Contract">
            <IconButton>
              <Share />
            </IconButton>
          </Tooltip>
          <Tooltip title="Print Contract">
            <IconButton>
              <Print />
            </IconButton>
          </Tooltip>
          {canEdit && (
            <Button
              variant="contained"
              startIcon={<EditIcon />}
              onClick={onEdit}
            >
              Edit Contract
            </Button>
          )}
        </Box>
      </Box>

      {/* Navigation Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange} aria-label="contract detail tabs">
          <Tab label="Overview" icon={<Info />} />
          <Tab label="Workflow" icon={<Timeline />} />
          <Tab label={`Matched (${matchings.length})`} icon={<Assignment />} />
          <Tab label={`Shipping (${shippingOps.length})`} icon={<LocalShipping />} />
          <Tab label={`Settlements (${settlements.length})`} icon={<SettlementIcon />} />
          <Tab label="Documents" icon={<Receipt />} />
          <Tab label="Notes" icon={<Note />} />
          <Tab label="History" icon={<HistoryIcon />} />
        </Tabs>
      </Box>

      {/* Tab Panels */}
      <TabPanel value={activeTab} index={0}>
        {/* Overview Tab */}
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Grid container spacing={3}>
              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Basic Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Supplier</Typography>
                        <Typography variant="body1">{contract.supplier.name} ({contract.supplier.code})</Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Product</Typography>
                        <Typography variant="body1">{contract.product.name} ({contract.product.code})</Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Quantity</Typography>
                        <Typography variant="body1">{contract.quantity.toLocaleString()} {getQuantityUnitLabel(contract.quantityUnit)}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Created</Typography>
                        <Typography variant="body1">{format(new Date(contract.createdAt), 'MMM dd, yyyy HH:mm')}</Typography>
                      </Grid>
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Pricing Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Pricing Type</Typography>
                        <Typography variant="body1">{getPricingTypeLabel(contract.pricingType)}</Typography>
                      </Grid>
                      {contract.fixedPrice && (
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2" color="text.secondary">Fixed Price</Typography>
                          <Typography variant="body1">${contract.fixedPrice.toLocaleString()} USD</Typography>
                        </Grid>
                      )}
                      {contract.pricingFormula && (
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" color="text.secondary">Pricing Formula</Typography>
                          <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>{contract.pricingFormula}</Typography>
                        </Grid>
                      )}
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Delivery Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Delivery Terms</Typography>
                        <Typography variant="body1">{getDeliveryTermsLabel(contract.deliveryTerms)}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Laycan Period</Typography>
                        <Typography variant="body1">
                          {format(new Date(contract.laycanStart), 'MMM dd')} - {format(new Date(contract.laycanEnd), 'MMM dd, yyyy')}
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Load Port</Typography>
                        <Typography variant="body1">{contract.loadPort}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Discharge Port</Typography>
                        <Typography variant="body1">{contract.dischargePort}</Typography>
                      </Grid>
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Payment Terms" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Settlement Type</Typography>
                        <Typography variant="body1">{getSettlementTypeLabel(contract.settlementType)}</Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">Credit Period</Typography>
                        <Typography variant="body1">{contract.creditPeriodDays} days</Typography>
                      </Grid>
                      {contract.paymentTerms && (
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" color="text.secondary">Payment Terms</Typography>
                          <Typography variant="body1">{contract.paymentTerms}</Typography>
                        </Grid>
                      )}
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Grid>

          <Grid item xs={12} md={4}>
            <ContractTagSelector contractId={contractId} contractType="PurchaseContract" />
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        <ContractWorkflow
          contract={contract}
          onStatusChange={handleStatusChange}
          onEdit={onEdit}
          onView={() => setActiveTab(0)}
        />
      </TabPanel>

      {/* Linked Contracts (Matchings) Tab */}
      <TabPanel value={activeTab} index={2}>
        <Card>
          <CardHeader
            title="Matched Sales Contracts"
            subheader={contract && matchings.length > 0 ? (() => {
              const totalMatched = matchings.reduce((sum, m) => sum + m.matchedQuantity, 0);
              const contractQty = contract.quantity || 0;
              const pct = contractQty > 0 ? (totalMatched / contractQty * 100) : 0;
              return `${totalMatched.toLocaleString()} / ${contractQty.toLocaleString()} MT matched (${pct.toFixed(1)}%)`;
            })() : undefined}
            action={
              <Button variant="outlined" size="small" onClick={() => navigate('/contract-matching')}>
                Go to Matching
              </Button>
            }
          />
          <CardContent>
            {/* Matched quantity progress bar */}
            {contract && matchings.length > 0 && (() => {
              const totalMatched = matchings.reduce((sum, m) => sum + m.matchedQuantity, 0);
              const contractQty = contract.quantity || 0;
              const pct = contractQty > 0 ? Math.min(totalMatched / contractQty * 100, 100) : 0;
              return (
                <Box sx={{ mb: 3 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                    <Typography variant="caption" color="text.secondary">Hedge Coverage</Typography>
                    <Typography variant="caption" fontWeight="bold" color={pct >= 80 ? 'success.main' : pct >= 50 ? 'warning.main' : 'error.main'}>
                      {pct.toFixed(1)}%
                    </Typography>
                  </Box>
                  <Box sx={{ height: 8, bgcolor: 'grey.200', borderRadius: 4, overflow: 'hidden' }}>
                    <Box sx={{
                      height: '100%',
                      width: `${pct}%`,
                      bgcolor: pct >= 80 ? 'success.main' : pct >= 50 ? 'warning.main' : 'error.main',
                      borderRadius: 4,
                      transition: 'width 0.5s ease',
                    }} />
                  </Box>
                </Box>
              );
            })()}

            {matchingsLoading ? (
              <Box display="flex" justifyContent="center" py={3}><CircularProgress size={24} /></Box>
            ) : matchings.length > 0 ? (
              <Box>
                {/* Timeline view */}
                {[...matchings].sort((a, b) => new Date(a.matchedDate).getTime() - new Date(b.matchedDate).getTime()).map((matching, index) => (
                  <Box key={matching.id} sx={{ display: 'flex', mb: 2 }}>
                    {/* Timeline line */}
                    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mr: 2 }}>
                      <CircleIcon sx={{ fontSize: 12, color: 'primary.main', zIndex: 1 }} />
                      {index < matchings.length - 1 && (
                        <Box sx={{ width: 2, flexGrow: 1, bgcolor: 'divider', mt: 0.5 }} />
                      )}
                    </Box>
                    {/* Content */}
                    <Box sx={{ flexGrow: 1, pb: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="subtitle2">{matching.salesContractNumber}</Typography>
                          <Chip label={`${matching.matchedQuantity.toLocaleString()} MT`} color="primary" size="small" />
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                          <Typography variant="caption" color="text.secondary">
                            {format(new Date(matching.matchedDate), 'MMM dd, yyyy')}
                          </Typography>
                          <Tooltip title="Unmatch">
                            <IconButton
                              size="small"
                              color="error"
                              onClick={async () => {
                                if (!confirm(`Remove matching of ${matching.matchedQuantity.toLocaleString()} MT with ${matching.salesContractNumber}?`)) return;
                                try {
                                  await contractMatchingApi.deleteMatching(matching.id);
                                  setMatchings(prev => prev.filter(m => m.id !== matching.id));
                                } catch (err) {
                                  console.error('Failed to unmatch:', err);
                                }
                              }}
                            >
                              <HistoryIcon sx={{ fontSize: 16 }} />
                            </IconButton>
                          </Tooltip>
                        </Box>
                      </Box>
                      <Typography variant="body2" color="text.secondary">
                        Customer: {matching.salesTradingPartner}
                        {matching.notes && ` | ${matching.notes}`}
                      </Typography>
                    </Box>
                  </Box>
                ))}
              </Box>
            ) : (
              <Alert severity="info">
                No matched sales contracts. Go to Contract Matching to create matches for natural hedging.
              </Alert>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Shipping Tab */}
      <TabPanel value={activeTab} index={3}>
        <Card>
          <CardHeader
            title="Shipping Operations"
            action={
              <Button variant="outlined" size="small" onClick={() => navigate('/shipping')}>
                Go to Shipping
              </Button>
            }
          />
          <CardContent>
            {shippingLoading ? (
              <Box display="flex" justifyContent="center" py={3}><CircularProgress size={24} /></Box>
            ) : shippingOps.length > 0 ? (
              <List>
                {shippingOps.map((operation, index) => (
                  <ListItem key={operation.id} divider={index < shippingOps.length - 1}>
                    <ListItemIcon><LocalShipping /></ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="subtitle1">{operation.shippingNumber || `SHP-${operation.id?.substring(0, 8)}`}</Typography>
                          <Chip
                            label={shippingApi.getStatusLabel(Number(operation.status))}
                            color={shippingApi.getStatusColor(Number(operation.status)) as any}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Typography variant="body2" color="text.secondary">
                          {operation.vesselName || 'TBN'} | {operation.loadPort} → {operation.dischargePort}
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Alert severity="info">
                No shipping operations found for this contract.
              </Alert>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Settlements Tab */}
      <TabPanel value={activeTab} index={4}>
        <Card>
          <CardHeader
            title="Settlements"
            action={
              <Button
                variant="contained"
                size="small"
                onClick={() => navigate(`/settlements?contractId=${contractId}`)}
              >
                Create Settlement
              </Button>
            }
          />
          <CardContent>
            {settlementsLoading ? (
              <Box display="flex" justifyContent="center" py={3}><CircularProgress size={24} /></Box>
            ) : settlements.length > 0 ? (
              <List>
                {settlements.map((settlement, index) => (
                  <ListItem key={settlement.id} divider={index < settlements.length - 1}>
                    <ListItemIcon><SettlementIcon /></ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="subtitle1">
                            {settlement.documentNumber || `Settlement #${index + 1}`}
                          </Typography>
                          <Chip
                            label={settlement.status || 'Draft'}
                            color={getSettlementStatusColor(settlement.status || 'Draft')}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Typography variant="body2" color="text.secondary">
                          {settlement.actualQuantityMT ? `${settlement.actualQuantityMT.toLocaleString()} MT` : ''}
                          {settlement.totalSettlementAmount ? ` | $${settlement.totalSettlementAmount.toLocaleString()}` : ''}
                          {settlement.documentDate ? ` | ${format(new Date(settlement.documentDate), 'MMM dd, yyyy')}` : ''}
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Alert severity="info">
                No settlements created for this contract yet.
              </Alert>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      {/* Documents Tab */}
      <TabPanel value={activeTab} index={5}>
        <Card>
          <CardHeader
            title="Contract Documents"
            action={
              <Button variant="outlined" size="small" disabled>
                Upload Document
              </Button>
            }
          />
          <CardContent>
            <Alert severity="info">
              Document management will be available in a future update.
            </Alert>
          </CardContent>
        </Card>
      </TabPanel>

      {/* Notes Tab */}
      <TabPanel value={activeTab} index={6}>
        <Card>
          <CardHeader
            title="Contract Notes"
            action={
              <Button
                variant="outlined"
                size="small"
                onClick={() => setNoteDialog({ open: true, note: '' })}
              >
                Add Note
              </Button>
            }
          />
          <CardContent>
            <Alert severity="info">
              Notes will be connected to the backend in a future update. Use the contract's "notes" field in the Overview tab for now.
            </Alert>
          </CardContent>
        </Card>
      </TabPanel>

      {/* History Tab */}
      <TabPanel value={activeTab} index={7}>
        <Card>
          <CardHeader title="Contract Lifecycle History" />
          <CardContent>
            <HistoryTimeline
              contract={contract}
              matchings={matchings}
              shippingOps={shippingOps}
              settlements={settlements}
            />
          </CardContent>
        </Card>
      </TabPanel>

      {/* Add Note Dialog */}
      <Dialog
        open={noteDialog.open}
        onClose={() => setNoteDialog({ open: false, note: '' })}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Add Contract Note</DialogTitle>
        <DialogContent>
          <TextField
            fullWidth
            multiline
            rows={4}
            label="Note"
            value={noteDialog.note}
            onChange={(e) => setNoteDialog(prev => ({ ...prev, note: e.target.value }))}
            placeholder="Enter your note about this contract..."
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setNoteDialog({ open: false, note: '' })}>
            Cancel
          </Button>
          <Button
            onClick={handleAddNote}
            variant="contained"
            disabled={!noteDialog.note.trim()}
          >
            Add Note
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
