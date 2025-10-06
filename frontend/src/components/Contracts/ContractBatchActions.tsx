import React, { useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  List,
  ListItem,
  ListItemText,
  Alert,
  Chip,
  Divider,
  Grid,
  Card,
  CardContent,
  CircularProgress,
} from '@mui/material';
import {
  Assignment,
  Archive,
  Check,
  Cancel,
  FileCopy,
  Send,
  Gavel,
} from '@mui/icons-material';
import { ContractStatus, PurchaseContractListDto } from '@/types/contracts';

interface BatchActionDialogProps {
  open: boolean;
  action: string;
  selectedContracts: PurchaseContractListDto[];
  onClose: () => void;
  onConfirm: (action: string, params?: any) => Promise<void>;
}

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

export const ContractBatchActions: React.FC<BatchActionDialogProps> = ({
  open,
  action,
  selectedContracts,
  onClose,
  onConfirm,
}) => {
  const [parameters, setParameters] = useState<any>({});
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const handleClose = () => {
    setParameters({});
    setErrors([]);
    onClose();
  };

  const handleConfirm = async () => {
    setLoading(true);
    setErrors([]);
    try {
      await onConfirm(action, parameters);
      handleClose();
    } catch (error: any) {
      setErrors([error.message || 'An error occurred']);
    } finally {
      setLoading(false);
    }
  };

  const validateAction = (): string[] => {
    const validationErrors: string[] = [];
    
    switch (action) {
      case 'submit-approval':
        const draftContracts = selectedContracts.filter(c => c.status !== ContractStatus.Draft);
        if (draftContracts.length > 0) {
          validationErrors.push(`${draftContracts.length} contracts are not in Draft status`);
        }
        break;
      
      case 'approve':
        const nonPendingContracts = selectedContracts.filter(c => c.status !== ContractStatus.PendingApproval);
        if (nonPendingContracts.length > 0) {
          validationErrors.push(`${nonPendingContracts.length} contracts are not pending approval`);
        }
        if (!parameters.approvalNotes || parameters.approvalNotes.trim().length === 0) {
          validationErrors.push('Approval notes are required');
        }
        break;
      
      case 'reject':
        const nonPendingRejectContracts = selectedContracts.filter(c => c.status !== ContractStatus.PendingApproval);
        if (nonPendingRejectContracts.length > 0) {
          validationErrors.push(`${nonPendingRejectContracts.length} contracts are not pending approval`);
        }
        if (!parameters.rejectionReason || parameters.rejectionReason.trim().length === 0) {
          validationErrors.push('Rejection reason is required');
        }
        break;
      
      case 'cancel':
        const nonCancellableContracts = selectedContracts.filter(c => 
          c.status === ContractStatus.Completed || c.status === ContractStatus.Cancelled
        );
        if (nonCancellableContracts.length > 0) {
          validationErrors.push(`${nonCancellableContracts.length} contracts cannot be cancelled`);
        }
        if (!parameters.cancellationReason || parameters.cancellationReason.trim().length === 0) {
          validationErrors.push('Cancellation reason is required');
        }
        break;
      
      case 'assign-trader':
        if (!parameters.traderId || parameters.traderId.trim().length === 0) {
          validationErrors.push('Trader selection is required');
        }
        break;
      
      case 'update-status':
        if (parameters.newStatus === undefined || parameters.newStatus === null) {
          validationErrors.push('New status selection is required');
        }
        break;
    }
    
    return validationErrors;
  };

  const renderActionForm = () => {
    switch (action) {
      case 'submit-approval':
        return (
          <Box>
            <Alert severity="info" sx={{ mb: 2 }}>
              Submit {selectedContracts.length} contract(s) for approval
            </Alert>
            <TextField
              fullWidth
              multiline
              rows={3}
              label="Submission Notes (Optional)"
              value={parameters.submissionNotes || ''}
              onChange={(e) => setParameters({ ...parameters, submissionNotes: e.target.value })}
              placeholder="Add any notes for the approval process..."
            />
          </Box>
        );

      case 'approve':
        return (
          <Box>
            <Alert severity="success" sx={{ mb: 2 }}>
              Approve {selectedContracts.length} contract(s)
            </Alert>
            <TextField
              fullWidth
              multiline
              rows={3}
              label="Approval Notes *"
              required
              value={parameters.approvalNotes || ''}
              onChange={(e) => setParameters({ ...parameters, approvalNotes: e.target.value })}
              placeholder="Enter approval notes and any conditions..."
              error={!parameters.approvalNotes}
              helperText={!parameters.approvalNotes ? 'Approval notes are required' : ''}
            />
          </Box>
        );

      case 'reject':
        return (
          <Box>
            <Alert severity="error" sx={{ mb: 2 }}>
              Reject {selectedContracts.length} contract(s)
            </Alert>
            <TextField
              fullWidth
              multiline
              rows={3}
              label="Rejection Reason *"
              required
              value={parameters.rejectionReason || ''}
              onChange={(e) => setParameters({ ...parameters, rejectionReason: e.target.value })}
              placeholder="Enter detailed reason for rejection..."
              error={!parameters.rejectionReason}
              helperText={!parameters.rejectionReason ? 'Rejection reason is required' : ''}
            />
          </Box>
        );

      case 'cancel':
        return (
          <Box>
            <Alert severity="warning" sx={{ mb: 2 }}>
              Cancel {selectedContracts.length} contract(s). This action cannot be undone.
            </Alert>
            <TextField
              fullWidth
              multiline
              rows={3}
              label="Cancellation Reason *"
              required
              value={parameters.cancellationReason || ''}
              onChange={(e) => setParameters({ ...parameters, cancellationReason: e.target.value })}
              placeholder="Enter reason for cancellation..."
              error={!parameters.cancellationReason}
              helperText={!parameters.cancellationReason ? 'Cancellation reason is required' : ''}
            />
          </Box>
        );

      case 'assign-trader':
        return (
          <Box>
            <Alert severity="info" sx={{ mb: 2 }}>
              Assign trader to {selectedContracts.length} contract(s)
            </Alert>
            <FormControl fullWidth required error={!parameters.traderId}>
              <InputLabel>Select Trader</InputLabel>
              <Select
                value={parameters.traderId || ''}
                label="Select Trader"
                onChange={(e) => setParameters({ ...parameters, traderId: e.target.value })}
              >
                <MenuItem value="trader1">John Smith</MenuItem>
                <MenuItem value="trader2">Sarah Johnson</MenuItem>
                <MenuItem value="trader3">Mike Chen</MenuItem>
                <MenuItem value="trader4">Anna Rodriguez</MenuItem>
              </Select>
            </FormControl>
            <TextField
              fullWidth
              multiline
              rows={2}
              label="Assignment Notes (Optional)"
              value={parameters.assignmentNotes || ''}
              onChange={(e) => setParameters({ ...parameters, assignmentNotes: e.target.value })}
              placeholder="Add notes about the assignment..."
              sx={{ mt: 2 }}
            />
          </Box>
        );

      case 'update-status':
        return (
          <Box>
            <Alert severity="info" sx={{ mb: 2 }}>
              Update status for {selectedContracts.length} contract(s)
            </Alert>
            <FormControl fullWidth required error={parameters.newStatus === undefined}>
              <InputLabel>New Status</InputLabel>
              <Select
                value={parameters.newStatus ?? ''}
                label="New Status"
                onChange={(e) => setParameters({ ...parameters, newStatus: e.target.value })}
              >
                <MenuItem value={ContractStatus.Draft}>Draft</MenuItem>
                <MenuItem value={ContractStatus.PendingApproval}>Pending Approval</MenuItem>
                <MenuItem value={ContractStatus.Active}>Active</MenuItem>
                <MenuItem value={ContractStatus.Completed}>Completed</MenuItem>
                <MenuItem value={ContractStatus.Cancelled}>Cancelled</MenuItem>
              </Select>
            </FormControl>
          </Box>
        );

      case 'archive':
        return (
          <Box>
            <Alert severity="warning" sx={{ mb: 2 }}>
              Archive {selectedContracts.length} contract(s). Archived contracts can be restored later.
            </Alert>
            <TextField
              fullWidth
              multiline
              rows={2}
              label="Archive Notes (Optional)"
              value={parameters.archiveNotes || ''}
              onChange={(e) => setParameters({ ...parameters, archiveNotes: e.target.value })}
              placeholder="Add notes about why these contracts are being archived..."
            />
          </Box>
        );

      case 'export':
        return (
          <Box>
            <Alert severity="info" sx={{ mb: 2 }}>
              Export {selectedContracts.length} contract(s) to Excel
            </Alert>
            <FormControl fullWidth>
              <InputLabel>Export Format</InputLabel>
              <Select
                value={parameters.exportFormat || 'excel'}
                label="Export Format"
                onChange={(e) => setParameters({ ...parameters, exportFormat: e.target.value })}
              >
                <MenuItem value="excel">Excel (.xlsx)</MenuItem>
                <MenuItem value="csv">CSV (.csv)</MenuItem>
                <MenuItem value="pdf">PDF Report</MenuItem>
              </Select>
            </FormControl>
            <FormControl fullWidth sx={{ mt: 2 }}>
              <InputLabel>Include Fields</InputLabel>
              <Select
                multiple
                value={parameters.includeFields || ['basic', 'pricing', 'delivery']}
                label="Include Fields"
                onChange={(e) => setParameters({ ...parameters, includeFields: e.target.value })}
              >
                <MenuItem value="basic">Basic Information</MenuItem>
                <MenuItem value="pricing">Pricing Details</MenuItem>
                <MenuItem value="delivery">Delivery Information</MenuItem>
                <MenuItem value="payment">Payment Terms</MenuItem>
                <MenuItem value="quality">Quality Specifications</MenuItem>
                <MenuItem value="notes">Notes and Comments</MenuItem>
              </Select>
            </FormControl>
          </Box>
        );

      default:
        return (
          <Alert severity="info">
            Perform action on {selectedContracts.length} selected contract(s)
          </Alert>
        );
    }
  };

  const getActionTitle = () => {
    switch (action) {
      case 'submit-approval': return 'Submit for Approval';
      case 'approve': return 'Approve Contracts';
      case 'reject': return 'Reject Contracts';
      case 'cancel': return 'Cancel Contracts';
      case 'assign-trader': return 'Assign Trader';
      case 'update-status': return 'Update Status';
      case 'archive': return 'Archive Contracts';
      case 'export': return 'Export Contracts';
      default: return 'Batch Action';
    }
  };

  const getActionIcon = () => {
    switch (action) {
      case 'submit-approval': return <Send />;
      case 'approve': return <Check />;
      case 'reject': return <Cancel />;
      case 'cancel': return <Cancel />;
      case 'assign-trader': return <Assignment />;
      case 'update-status': return <Gavel />;
      case 'archive': return <Archive />;
      case 'export': return <FileCopy />;
      default: return <Gavel />;
    }
  };

  const validationErrors = validateAction();
  const canProceed = validationErrors.length === 0 && !loading;

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center" gap={1}>
          {getActionIcon()}
          {getActionTitle()}
        </Box>
      </DialogTitle>
      
      <DialogContent>
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            {renderActionForm()}
            
            {validationErrors.length > 0 && (
              <Alert severity="error" sx={{ mt: 2 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Please fix the following issues:
                </Typography>
                <List dense>
                  {validationErrors.map((error, index) => (
                    <ListItem key={index} sx={{ py: 0 }}>
                      <ListItemText primary={`• ${error}`} />
                    </ListItem>
                  ))}
                </List>
              </Alert>
            )}

            {errors.length > 0 && (
              <Alert severity="error" sx={{ mt: 2 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Errors occurred:
                </Typography>
                <List dense>
                  {errors.map((error, index) => (
                    <ListItem key={index} sx={{ py: 0 }}>
                      <ListItemText primary={`• ${error}`} />
                    </ListItem>
                  ))}
                </List>
              </Alert>
            )}
          </Grid>
          
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Selected Contracts ({selectedContracts.length})
                </Typography>
                <Divider sx={{ mb: 2 }} />
                
                <List dense sx={{ maxHeight: 300, overflow: 'auto' }}>
                  {selectedContracts.map((contract) => (
                    <ListItem key={contract.id} sx={{ px: 0 }}>
                      <ListItemText
                        primary={
                          <Box display="flex" alignItems="center" gap={1}>
                            <Typography variant="body2" fontWeight="medium">
                              {contract.contractNumber}
                            </Typography>
                            <Chip
                              label={getStatusLabel(contract.status)}
                              color={getStatusColor(contract.status)}
                              size="small"
                            />
                          </Box>
                        }
                        secondary={
                          <Box>
                            <Typography variant="caption" display="block">
                              {contract.supplierName} • {contract.productName}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {contract.quantity.toLocaleString()} MT
                            </Typography>
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </DialogContent>
      
      <DialogActions>
        <Button onClick={handleClose} disabled={loading}>
          Cancel
        </Button>
        <Button
          onClick={handleConfirm}
          variant="contained"
          disabled={!canProceed}
          startIcon={loading ? <CircularProgress size={20} /> : getActionIcon()}
        >
          {loading ? 'Processing...' : getActionTitle()}
        </Button>
      </DialogActions>
    </Dialog>
  );
};