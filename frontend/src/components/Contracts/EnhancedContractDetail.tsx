import React, { useState } from 'react';
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
} from '@mui/icons-material';
import { format } from 'date-fns';
import { usePurchaseContract } from '@/hooks/useContracts';
import { ContractTagSelector } from '@/components/Tags/ContractTagSelector';
import { ContractWorkflow } from './ContractWorkflow';
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

export const EnhancedContractDetail: React.FC<EnhancedContractDetailProps> = ({
  contractId,
  onEdit,
  onBack,
}) => {
  const { data: contract, isLoading, error, refetch } = usePurchaseContract(contractId);
  const [activeTab, setActiveTab] = useState(0);
  const [noteDialog, setNoteDialog] = useState({ open: false, note: '' });

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleStatusChange = async (newStatus: ContractStatus, notes?: string) => {
    try {
      console.log('Changing contract status:', { contractId, newStatus, notes });
      // Here you would call the API to update the contract status
      // await updateContractStatus(contractId, newStatus, notes);
      await refetch();
    } catch (error) {
      console.error('Failed to update contract status:', error);
      throw error;
    }
  };

  const handleAddNote = async () => {
    try {
      console.log('Adding note to contract:', { contractId, note: noteDialog.note });
      // Here you would call the API to add a note
      // await addContractNote(contractId, noteDialog.note);
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

  // Mock linked contracts and shipping operations for demonstration
  const mockLinkedSalesContracts = [
    { id: '1', contractNumber: 'SLS-2024-001', customerName: 'ABC Trading', quantity: 5000, status: ContractStatus.Active },
    { id: '2', contractNumber: 'SLS-2024-002', customerName: 'DEF Energy', quantity: 3000, status: ContractStatus.Draft },
  ];

  const mockShippingOperations = [
    { id: '1', operationNumber: 'SHP-2024-001', vesselName: 'MT Pacific Star', status: 'In Transit', eta: new Date() },
    { id: '2', operationNumber: 'SHP-2024-002', vesselName: 'MT Atlantic Wind', status: 'Loading', eta: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000) },
  ];

  const mockNotes = [
    { id: '1', content: 'Quality specifications confirmed with supplier', createdBy: 'John Smith', createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000) },
    { id: '2', content: 'Delivery schedule updated due to vessel availability', createdBy: 'Sarah Johnson', createdAt: new Date(Date.now() - 48 * 60 * 60 * 1000) },
  ];

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
          <Tab label="Linked Contracts" icon={<Assignment />} />
          <Tab label="Shipping" icon={<LocalShipping />} />
          <Tab label="Documents" icon={<Receipt />} />
          <Tab label="Notes" icon={<Note />} />
        </Tabs>
      </Box>

      {/* Tab Panels */}
      <TabPanel value={activeTab} index={0}>
        {/* Overview Tab */}
        <Grid container spacing={3}>
          {/* Contract Information */}
          <Grid item xs={12} md={8}>
            <Grid container spacing={3}>
              {/* Basic Information */}
              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Basic Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Supplier
                        </Typography>
                        <Typography variant="body1">
                          {contract.supplier.name} ({contract.supplier.code})
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Product
                        </Typography>
                        <Typography variant="body1">
                          {contract.product.name} ({contract.product.code})
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Quantity
                        </Typography>
                        <Typography variant="body1">
                          {contract.quantity.toLocaleString()} {getQuantityUnitLabel(contract.quantityUnit)}
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Created
                        </Typography>
                        <Typography variant="body1">
                          {format(new Date(contract.createdAt), 'MMM dd, yyyy HH:mm')}
                        </Typography>
                      </Grid>
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              {/* Pricing Information */}
              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Pricing Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Pricing Type
                        </Typography>
                        <Typography variant="body1">
                          {getPricingTypeLabel(contract.pricingType)}
                        </Typography>
                      </Grid>
                      {contract.fixedPrice && (
                        <Grid item xs={12} sm={6}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Fixed Price
                          </Typography>
                          <Typography variant="body1">
                            ${contract.fixedPrice.toLocaleString()} USD
                          </Typography>
                        </Grid>
                      )}
                      {contract.pricingFormula && (
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Pricing Formula
                          </Typography>
                          <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                            {contract.pricingFormula}
                          </Typography>
                        </Grid>
                      )}
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              {/* Delivery Information */}
              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Delivery Information" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Delivery Terms
                        </Typography>
                        <Typography variant="body1">
                          {getDeliveryTermsLabel(contract.deliveryTerms)}
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Laycan Period
                        </Typography>
                        <Typography variant="body1">
                          {format(new Date(contract.laycanStart), 'MMM dd')} - {format(new Date(contract.laycanEnd), 'MMM dd, yyyy')}
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Load Port
                        </Typography>
                        <Typography variant="body1">
                          {contract.loadPort}
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Discharge Port
                        </Typography>
                        <Typography variant="body1">
                          {contract.dischargePort}
                        </Typography>
                      </Grid>
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>

              {/* Payment Terms */}
              <Grid item xs={12}>
                <Card>
                  <CardHeader title="Payment Terms" />
                  <CardContent>
                    <Grid container spacing={2}>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Settlement Type
                        </Typography>
                        <Typography variant="body1">
                          {getSettlementTypeLabel(contract.settlementType)}
                        </Typography>
                      </Grid>
                      <Grid item xs={12} sm={6}>
                        <Typography variant="subtitle2" color="text.secondary">
                          Credit Period
                        </Typography>
                        <Typography variant="body1">
                          {contract.creditPeriodDays} days
                        </Typography>
                      </Grid>
                      {contract.paymentTerms && (
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" color="text.secondary">
                            Payment Terms
                          </Typography>
                          <Typography variant="body1">
                            {contract.paymentTerms}
                          </Typography>
                        </Grid>
                      )}
                    </Grid>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Grid>

          {/* Tags Section */}
          <Grid item xs={12} md={4}>
            <ContractTagSelector
              contractId={contractId}
              contractType="PurchaseContract"
            />
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        {/* Workflow Tab */}
        <ContractWorkflow
          contract={contract}
          onStatusChange={handleStatusChange}
          onEdit={onEdit}
          onView={() => setActiveTab(0)}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        {/* Linked Contracts Tab */}
        <Card>
          <CardHeader 
            title="Linked Sales Contracts" 
            action={
              <Button variant="outlined" size="small">
                Link Contract
              </Button>
            }
          />
          <CardContent>
            {mockLinkedSalesContracts.length > 0 ? (
              <List>
                {mockLinkedSalesContracts.map((salesContract, index) => (
                  <ListItem key={salesContract.id} divider={index < mockLinkedSalesContracts.length - 1}>
                    <ListItemIcon>
                      <Assignment />
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="subtitle1">
                            {salesContract.contractNumber}
                          </Typography>
                          <Chip
                            label={getStatusLabel(salesContract.status)}
                            color={getStatusColor(salesContract.status)}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Typography variant="body2" color="text.secondary">
                          {salesContract.customerName} • {salesContract.quantity.toLocaleString()} MT
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Alert severity="info">
                No linked sales contracts found. Link sales contracts to track related transactions.
              </Alert>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        {/* Shipping Tab */}
        <Card>
          <CardHeader 
            title="Shipping Operations" 
            action={
              <Button variant="outlined" size="small">
                Create Shipping
              </Button>
            }
          />
          <CardContent>
            {mockShippingOperations.length > 0 ? (
              <List>
                {mockShippingOperations.map((operation, index) => (
                  <ListItem key={operation.id} divider={index < mockShippingOperations.length - 1}>
                    <ListItemIcon>
                      <LocalShipping />
                    </ListItemIcon>
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="subtitle1">
                            {operation.operationNumber}
                          </Typography>
                          <Chip
                            label={operation.status}
                            color="primary"
                            size="small"
                          />
                        </Box>
                      }
                      secondary={
                        <Typography variant="body2" color="text.secondary">
                          {operation.vesselName} • ETA: {format(operation.eta, 'MMM dd, yyyy')}
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Alert severity="info">
                No shipping operations found. Create shipping operations to track deliveries.
              </Alert>
            )}
          </CardContent>
        </Card>
      </TabPanel>

      <TabPanel value={activeTab} index={4}>
        {/* Documents Tab */}
        <Card>
          <CardHeader 
            title="Contract Documents" 
            action={
              <Button variant="outlined" size="small">
                Upload Document
              </Button>
            }
          />
          <CardContent>
            <Alert severity="info">
              Document management feature will be implemented in the next phase.
            </Alert>
          </CardContent>
        </Card>
      </TabPanel>

      <TabPanel value={activeTab} index={5}>
        {/* Notes Tab */}
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
            {mockNotes.length > 0 ? (
              <List>
                {mockNotes.map((note, index) => (
                  <ListItem key={note.id} divider={index < mockNotes.length - 1} alignItems="flex-start">
                    <ListItemIcon>
                      <Note />
                    </ListItemIcon>
                    <ListItemText
                      primary={note.content}
                      secondary={
                        <Typography variant="caption" color="text.secondary">
                          by {note.createdBy} on {format(note.createdAt, 'MMM dd, yyyy HH:mm')}
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Alert severity="info">
                No notes found. Add notes to track important information about this contract.
              </Alert>
            )}
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