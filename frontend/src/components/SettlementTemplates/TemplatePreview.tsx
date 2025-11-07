import React from 'react';
import {
  Box,
  Card,
  CardContent,
  CardActions,
  Typography,
  Button,
  Divider,
  Grid,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  Paper,
  Tooltip,
  IconButton,
} from '@mui/material';
import {
  ContentCopy as ContentCopyIcon,
  Info as InfoIcon,
  CheckCircle as CheckCircleIcon,
  Schedule as ScheduleIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import { SettlementTemplate } from '@/services/templateApi';
import { SettlementTemplateConfig, DefaultChargeItem } from '@/types/templates';
import { ChargeTypeLabels } from '@/types/settlement';
import { formatDistanceToNow } from 'date-fns';

interface TemplatePreviewProps {
  template: SettlementTemplate;
  onApply: () => void;
  onEdit?: () => void;
  onDelete?: () => void;
  isLoading?: boolean;
  compact?: boolean; // For compact preview in dialogs/cards
}

const formatCurrency = (amount: number, currency: string = 'USD'): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
  }).format(amount);
};

const formatDate = (date: string | Date | undefined): string => {
  if (!date) return 'Never';
  return formatDistanceToNow(new Date(date), { addSuffix: true });
};

const getPermissionLabel = (permissionLevel: number): string => {
  const levels: Record<number, string> = {
    0: 'View Only',
    1: 'Can Use',
    2: 'Can Edit',
    3: 'Admin',
  };
  return levels[permissionLevel] || 'Unknown';
};

export const TemplatePreview: React.FC<TemplatePreviewProps> = ({
  template,
  onApply,
  onEdit,
  onDelete,
  isLoading = false,
  compact = false,
}) => {
  const config: SettlementTemplateConfig = template.templateConfiguration
    ? JSON.parse(template.templateConfiguration)
    : {};

  const handleCopyToClipboard = () => {
    const configJson = JSON.stringify(config, null, 2);
    navigator.clipboard.writeText(configJson);
  };

  if (compact) {
    return (
      <Card variant="outlined" sx={{ mb: 2 }}>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 2 }}>
            <Box>
              <Typography variant="h6" component="div">
                {template.name}
              </Typography>
              <Typography variant="body2" color="textSecondary" sx={{ mt: 0.5 }}>
                {template.description}
              </Typography>
            </Box>
            {template.isPublic && (
              <Chip label="Public" size="small" color="primary" variant="outlined" />
            )}
          </Box>

          <Box sx={{ display: 'flex', gap: 2, mt: 2, mb: 2, flexWrap: 'wrap' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <ScheduleIcon fontSize="small" />
              <Typography variant="caption">
                Used {template.timesUsed} times
              </Typography>
            </Box>
            {template.lastUsedAt && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                <CheckCircleIcon fontSize="small" color="success" />
                <Typography variant="caption">
                  Last used {formatDate(template.lastUsedAt)}
                </Typography>
              </Box>
            )}
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <PersonIcon fontSize="small" />
              <Typography variant="caption">
                By {template.createdByUserName}
              </Typography>
            </Box>
          </Box>

          {config.defaultCharges && config.defaultCharges.length > 0 && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Default Charges ({config.defaultCharges.length}):
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {config.defaultCharges.slice(0, 3).map((charge, idx) => (
                  <Chip
                    key={idx}
                    label={`${charge.chargeTypeLabel}: ${formatCurrency(charge.amount, config.defaultCurrency)}`}
                    size="small"
                    variant="outlined"
                  />
                ))}
                {config.defaultCharges.length > 3 && (
                  <Chip label={`+${config.defaultCharges.length - 3} more`} size="small" />
                )}
              </Box>
            </Box>
          )}
        </CardContent>
        <CardActions>
          <Button size="small" onClick={onApply} disabled={isLoading}>
            Use Template
          </Button>
          {onEdit && <Button size="small" onClick={onEdit}>Edit</Button>}
        </CardActions>
      </Card>
    );
  }

  // Full preview layout
  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        {/* Header */}
        <Box sx={{ mb: 3 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 1 }}>
            <Box>
              <Typography variant="h5" component="div" gutterBottom>
                {template.name}
              </Typography>
              <Typography variant="body1" color="textSecondary">
                {template.description}
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', gap: 1 }}>
              {template.isPublic && (
                <Tooltip title="This template is shared with other users">
                  <Chip label="Public" size="small" color="primary" />
                </Tooltip>
              )}
              {template.isActive && (
                <Chip label="Active" size="small" color="success" variant="outlined" />
              )}
            </Box>
          </Box>

          {/* Metadata */}
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={6}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Created by
                </Typography>
                <Typography variant="body2">
                  {template.createdByUserName}
                </Typography>
              </Box>
            </Grid>
            <Grid item xs={6}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Version
                </Typography>
                <Typography variant="body2">
                  {template.version}
                </Typography>
              </Box>
            </Grid>
            <Grid item xs={6}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Times Used
                </Typography>
                <Typography variant="body2">
                  {template.timesUsed} settlements
                </Typography>
              </Box>
            </Grid>
            <Grid item xs={6}>
              <Box>
                <Typography variant="caption" color="textSecondary">
                  Last Used
                </Typography>
                <Typography variant="body2">
                  {template.lastUsedAt ? formatDate(template.lastUsedAt) : 'Never'}
                </Typography>
              </Box>
            </Grid>
          </Grid>
        </Box>

        <Divider sx={{ my: 2 }} />

        {/* Template Configuration */}
        <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <InfoIcon fontSize="small" />
          Configuration
        </Typography>

        <Grid container spacing={2} sx={{ mb: 3 }}>
          <Grid item xs={6}>
            <Box sx={{ p: 2, bgcolor: 'background.default', borderRadius: 1 }}>
              <Typography variant="caption" color="textSecondary">
                Default Currency
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {config.defaultCurrency || 'USD'}
              </Typography>
            </Box>
          </Grid>
          <Grid item xs={6}>
            <Box sx={{ p: 2, bgcolor: 'background.default', borderRadius: 1 }}>
              <Typography variant="caption" color="textSecondary">
                Auto-Calculate Prices
              </Typography>
              <Typography variant="body1" sx={{ fontWeight: 600 }}>
                {config.autoCalculatePrices ? 'Yes' : 'No'}
              </Typography>
            </Box>
          </Grid>
        </Grid>

        {/* Default Charges */}
        {config.defaultCharges && config.defaultCharges.length > 0 && (
          <>
            <Typography variant="h6" gutterBottom sx={{ mt: 3, mb: 2 }}>
              Default Charges ({config.defaultCharges.length})
            </Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableBody>
                  {config.defaultCharges.map((charge: DefaultChargeItem, idx: number) => (
                    <React.Fragment key={idx}>
                      <TableCell sx={{ fontWeight: 600, width: '30%' }}>
                        {charge.chargeTypeLabel}
                      </TableCell>
                      <TableCell align="right" sx={{ width: '20%' }}>
                        {formatCurrency(charge.amount, config.defaultCurrency)}
                      </TableCell>
                      <TableCell sx={{ width: '50%' }}>
                        {charge.description}
                        {charge.isFixed && (
                          <Chip label="Fixed" size="small" variant="outlined" sx={{ ml: 1 }} />
                        )}
                      </TableCell>
                    </React.Fragment>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </>
        )}

        {/* Notes */}
        {config.notes && (
          <>
            <Divider sx={{ my: 3 }} />
            <Typography variant="h6" gutterBottom>
              Notes
            </Typography>
            <Typography variant="body2" color="textSecondary">
              {config.notes}
            </Typography>
          </>
        )}

        {/* Tags */}
        {config.tags && config.tags.length > 0 && (
          <>
            <Box sx={{ mt: 3 }}>
              <Typography variant="subtitle2" gutterBottom>
                Tags
              </Typography>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {config.tags.map((tag: string, idx: number) => (
                  <Chip key={idx} label={tag} size="small" variant="outlined" />
                ))}
              </Box>
            </Box>
          </>
        )}
      </CardContent>

      {/* Actions */}
      <CardActions sx={{ justifyContent: 'space-between', borderTop: '1px solid', borderColor: 'divider' }}>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            size="small"
            startIcon={<ContentCopyIcon />}
            onClick={handleCopyToClipboard}
            title="Copy configuration to clipboard"
          >
            Copy Config
          </Button>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {onEdit && (
            <Button size="small" onClick={onEdit}>
              Edit
            </Button>
          )}
          <Button
            size="medium"
            variant="contained"
            onClick={onApply}
            disabled={isLoading}
          >
            Use This Template
          </Button>
          {onDelete && (
            <Button
              size="small"
              color="error"
              onClick={onDelete}
            >
              Delete
            </Button>
          )}
        </Box>
      </CardActions>
    </Card>
  );
};

export default TemplatePreview;
