import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Stepper,
  Step,
  StepLabel,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  Grid,
  IconButton,
  Tooltip,
  LinearProgress,
} from '@mui/material';
// Timeline components temporarily disabled - @mui/lab not available
import {
  Drafts as Draft,
  Send,
  CheckCircle,
  Cancel,
  Edit,
  Visibility,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ContractStatus, PurchaseContract } from '@/types/contracts';

interface ContractWorkflowProps {
  contract: PurchaseContract;
  onStatusChange: (newStatus: ContractStatus, notes?: string) => Promise<void>;
  onEdit: () => void;
  onView: () => void;
}

interface WorkflowAction {
  key: string;
  label: string;
  icon: React.ReactNode;
  color: 'primary' | 'secondary' | 'success' | 'error' | 'warning' | 'info';
  requiresNotes: boolean;
  confirmationMessage?: string;
  nextStatus: ContractStatus;
}

interface WorkflowStep {
  status: ContractStatus;
  label: string;
  description: string;
  icon: React.ReactNode;
  color: 'primary' | 'secondary' | 'success' | 'error' | 'warning' | 'info';
}

const workflowSteps: WorkflowStep[] = [
  {
    status: ContractStatus.Draft,
    label: 'Draft',
    description: 'Contract is being prepared and edited',
    icon: <Draft />,
    color: 'secondary',
  },
  {
    status: ContractStatus.PendingApproval,
    label: 'Pending Approval',
    description: 'Contract is submitted and awaiting approval',
    icon: <Send />,
    color: 'warning',
  },
  {
    status: ContractStatus.Active,
    label: 'Active',
    description: 'Contract is approved and active',
    icon: <CheckCircle />,
    color: 'success',
  },
  {
    status: ContractStatus.Completed,
    label: 'Completed',
    description: 'Contract has been successfully completed',
    icon: <CheckCircle />,
    color: 'info',
  },
  {
    status: ContractStatus.Cancelled,
    label: 'Cancelled',
    description: 'Contract has been cancelled',
    icon: null,  // No icon for cancelled status
    color: 'error',
  },
];

const getAvailableActions = (currentStatus: ContractStatus): WorkflowAction[] => {
  const actions: WorkflowAction[] = [];

  switch (currentStatus) {
    case ContractStatus.Draft:
      actions.push({
        key: 'submit-approval',
        label: 'Submit for Approval',
        icon: <Send />,
        color: 'primary',
        requiresNotes: false,
        nextStatus: ContractStatus.PendingApproval,
      });
      actions.push({
        key: 'cancel',
        label: 'Cancel Contract',
        icon: <Cancel />,
        color: 'error',
        requiresNotes: true,
        confirmationMessage: 'Are you sure you want to cancel this draft contract?',
        nextStatus: ContractStatus.Cancelled,
      });
      break;

    case ContractStatus.PendingApproval:
      actions.push({
        key: 'approve',
        label: 'Approve Contract',
        icon: <CheckCircle />,
        color: 'success',
        requiresNotes: true,
        nextStatus: ContractStatus.Active,
      });
      actions.push({
        key: 'reject',
        label: 'Reject Contract',
        icon: <Cancel />,
        color: 'error',
        requiresNotes: true,
        confirmationMessage: 'Are you sure you want to reject this contract?',
        nextStatus: ContractStatus.Draft,
      });
      break;

    case ContractStatus.Active:
      actions.push({
        key: 'complete',
        label: 'Mark as Completed',
        icon: <CheckCircle />,
        color: 'info',
        requiresNotes: false,
        nextStatus: ContractStatus.Completed,
      });
      actions.push({
        key: 'cancel',
        label: 'Cancel Contract',
        icon: <Cancel />,
        color: 'error',
        requiresNotes: true,
        confirmationMessage: 'Are you sure you want to cancel this active contract?',
        nextStatus: ContractStatus.Cancelled,
      });
      break;

    case ContractStatus.Completed:
    case ContractStatus.Cancelled:
      // Terminal states - no actions available
      break;
  }

  return actions;
};

const getCurrentStepIndex = (status: ContractStatus): number => {
  return workflowSteps.findIndex(step => step.status === status);
};

export const ContractWorkflow: React.FC<ContractWorkflowProps> = ({
  contract,
  onStatusChange,
  onEdit,
  onView,
}) => {
  const [actionDialog, setActionDialog] = useState<{
    open: boolean;
    action?: WorkflowAction;
    notes: string;
  }>({ open: false, notes: '' });
  const [loading, setLoading] = useState(false);

  const currentStepIndex = getCurrentStepIndex(contract.status);
  const availableActions = getAvailableActions(contract.status);

  const handleActionClick = (action: WorkflowAction) => {
    setActionDialog({
      open: true,
      action,
      notes: '',
    });
  };

  const handleActionConfirm = async () => {
    if (!actionDialog.action) return;

    setLoading(true);
    try {
      await onStatusChange(
        actionDialog.action.nextStatus,
        actionDialog.notes || undefined
      );
      setActionDialog({ open: false, notes: '' });
    } catch (error) {
      console.error('Failed to change contract status:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleActionCancel = () => {
    setActionDialog({ open: false, notes: '' });
  };

  const getStatusChip = (status: ContractStatus) => {
    const step = workflowSteps.find(s => s.status === status);
    if (!step) return null;

    return (
      <Chip
        label={step.label}
        color={step.color}
        size="small"
      />
    );
  };

  const mockWorkflowHistory = [
    {
      timestamp: new Date(contract.createdAt),
      status: ContractStatus.Draft,
      action: 'Contract Created',
      user: contract.createdBy,
      notes: 'Initial contract draft created',
    },
    ...(contract.status !== ContractStatus.Draft ? [{
      timestamp: new Date(contract.updatedAt || contract.createdAt),
      status: ContractStatus.PendingApproval,
      action: 'Submitted for Approval',
      user: contract.updatedBy || contract.createdBy,
      notes: 'Contract submitted for management review',
    }] : []),
    ...(contract.status === ContractStatus.Active ? [{
      timestamp: new Date(contract.updatedAt || contract.createdAt),
      status: ContractStatus.Active,
      action: 'Contract Approved',
      user: 'John Smith (Approver)',
      notes: 'Contract approved and activated',
    }] : []),
  ];

  return (
    <Box>
      <Grid container spacing={3}>
        {/* Current Status and Actions */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
              <Typography variant="h6">Contract Workflow</Typography>
              <Box display="flex" gap={1}>
                <Tooltip title="Edit Contract">
                  <span>
                    <IconButton 
                      onClick={onEdit}
                      disabled={contract.status !== ContractStatus.Draft && contract.status !== ContractStatus.PendingApproval}
                    >
                      <Edit />
                    </IconButton>
                  </span>
                </Tooltip>
                <Tooltip title="View Details">
                  <IconButton onClick={onView}>
                    <Visibility />
                  </IconButton>
                </Tooltip>
              </Box>
            </Box>

            {/* Status Progress */}
            <Stepper activeStep={currentStepIndex} orientation="horizontal" sx={{ mb: 4 }}>
              {workflowSteps.map((step, index) => (
                <Step key={step.status} completed={index < currentStepIndex}>
                  <StepLabel 
                    {...(step.icon && { icon: step.icon })}
                    error={step.status === ContractStatus.Cancelled && contract.status === ContractStatus.Cancelled}
                  >
                    {step.label}
                  </StepLabel>
                </Step>
              ))}
            </Stepper>

            {/* Current Status Info */}
            <Card sx={{ mb: 3 }}>
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                  <Typography variant="h6">Current Status</Typography>
                  {getStatusChip(contract.status)}
                </Box>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                  {workflowSteps.find(s => s.status === contract.status)?.description}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Last updated: {format(new Date(contract.updatedAt || contract.createdAt), 'PPpp')}
                </Typography>
              </CardContent>
            </Card>

            {/* Available Actions */}
            {availableActions.length > 0 && (
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Available Actions
                  </Typography>
                  <Box display="flex" gap={2} flexWrap="wrap">
                    {availableActions.map((action) => (
                      <Button
                        key={action.key}
                        variant="contained"
                        color={action.color}
                        startIcon={action.icon}
                        onClick={() => handleActionClick(action)}
                        disabled={loading}
                      >
                        {action.label}
                      </Button>
                    ))}
                  </Box>
                </CardContent>
              </Card>
            )}

            {/* Terminal Status Info */}
            {(contract.status === ContractStatus.Completed || contract.status === ContractStatus.Cancelled) && (
              <Alert 
                severity={contract.status === ContractStatus.Completed ? 'success' : 'warning'}
                sx={{ mt: 2 }}
              >
                <Typography variant="subtitle2">
                  {contract.status === ContractStatus.Completed 
                    ? 'Contract Completed' 
                    : 'Contract Cancelled'
                  }
                </Typography>
                <Typography variant="body2">
                  {contract.status === ContractStatus.Completed 
                    ? 'This contract has been successfully completed and no further actions are available.'
                    : 'This contract has been cancelled and no further actions are available.'
                  }
                </Typography>
              </Alert>
            )}
          </Paper>
        </Grid>

        {/* Workflow History */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Workflow History
            </Typography>
            <Box>
              {mockWorkflowHistory.map((event, index) => (
                <Box key={index} sx={{ mb: 2, p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box display="flex" alignItems="center" gap={1} mb={1}>
                    {workflowSteps.find(s => s.status === event.status)?.icon}
                    <Typography variant="h6" component="span">
                      {event.action}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      - {format(event.timestamp, 'MMM dd, HH:mm')}
                    </Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary">
                    by {event.user}
                  </Typography>
                  {event.notes && (
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      {event.notes}
                    </Typography>
                  )}
                </Box>
              ))}
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Action Confirmation Dialog */}
      <Dialog 
        open={actionDialog.open} 
        onClose={handleActionCancel}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box display="flex" alignItems="center" gap={1}>
            {actionDialog.action?.icon}
            {actionDialog.action?.label}
          </Box>
        </DialogTitle>
        <DialogContent>
          {actionDialog.action?.confirmationMessage && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              {actionDialog.action.confirmationMessage}
            </Alert>
          )}
          
          <Typography variant="body1" gutterBottom>
            Contract: <strong>{contract.contractNumber.value}</strong>
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            This will change the status from <strong>{workflowSteps.find(s => s.status === contract.status)?.label}</strong> to{' '}
            <strong>{workflowSteps.find(s => s.status === actionDialog.action?.nextStatus)?.label}</strong>
          </Typography>

          {actionDialog.action?.requiresNotes && (
            <TextField
              fullWidth
              multiline
              rows={4}
              label={`${actionDialog.action.label} Notes ${actionDialog.action.requiresNotes ? '*' : ''}`}
              value={actionDialog.notes}
              onChange={(e) => setActionDialog(prev => ({ ...prev, notes: e.target.value }))}
              sx={{ mt: 2 }}
              required={actionDialog.action.requiresNotes}
              placeholder={`Enter notes for ${actionDialog.action.label.toLowerCase()}...`}
            />
          )}

          {loading && <LinearProgress sx={{ mt: 2 }} />}
        </DialogContent>
        <DialogActions>
          <Button onClick={handleActionCancel} disabled={loading}>
            Cancel
          </Button>
          <Button
            onClick={handleActionConfirm}
            variant="contained"
            color={actionDialog.action?.color}
            disabled={loading || (actionDialog.action?.requiresNotes && !actionDialog.notes.trim())}
            startIcon={actionDialog.action?.icon}
          >
            {loading ? 'Processing...' : actionDialog.action?.label}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};