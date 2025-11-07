import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Container,
  Dialog,
  DialogTitle,
  Divider,
  Grid,
  LinearProgress,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
  Alert,
  Chip,
  IconButton,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Menu,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Share as ShareIcon,
  MoreVert as MoreVertIcon,
  FileDownload as DownloadIcon,
} from '@mui/icons-material';
import { useTemplateManagement } from '@/hooks/useTemplateManagement';
import { TemplateForm } from '@/components/SettlementTemplates/TemplateForm';
import { TemplatePreview } from '@/components/SettlementTemplates/TemplatePreview';
import { SettlementTemplateConfig } from '@/types/templates';
import { SettlementTemplate } from '@/services/templateApi';

type ViewMode = 'list' | 'create' | 'edit' | 'preview';

export const SettlementTemplatesPage: React.FC = () => {
  const {
    templates,
    selectedTemplate,
    loading,
    error,
    totalCount,
    currentPage,
    fetchTemplates,
    createTemplate,
    updateTemplate,
    deleteTemplate,
    getTemplate,
    setSelectedTemplate,
    clearError,
    setCurrentPage,
  } = useTemplateManagement({ autoFetch: true, pageSize: 10 });

  const [viewMode, setViewMode] = useState<ViewMode>('list');
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [templateToDelete, setTemplateToDelete] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [selectedMenuTemplate, setSelectedMenuTemplate] = useState<SettlementTemplate | null>(null);

  // Handle create
  const handleCreateTemplate = async (
    name: string,
    description: string,
    config: SettlementTemplateConfig,
    isPublic: boolean
  ) => {
    const result = await createTemplate(name, description, config, isPublic);
    if (result) {
      setViewMode('list');
      await fetchTemplates({ pageNumber: 1 });
    }
  };

  // Handle update
  const handleUpdateTemplate = async (
    name: string,
    description: string,
    config: SettlementTemplateConfig,
    isPublic: boolean
  ) => {
    if (!selectedTemplate) return;

    const result = await updateTemplate(
      selectedTemplate.id,
      name,
      description,
      config,
      isPublic
    );
    if (result) {
      setViewMode('list');
      setSelectedTemplate(null);
      await fetchTemplates({ pageNumber: currentPage });
    }
  };

  // Handle delete
  const handleDeleteConfirm = async () => {
    if (!templateToDelete) return;

    const success = await deleteTemplate(templateToDelete);
    if (success) {
      setDeleteConfirmOpen(false);
      setTemplateToDelete(null);
      setViewMode('list');
      await fetchTemplates({ pageNumber: currentPage });
    }
  };

  const handleViewTemplate = async (template: SettlementTemplate) => {
    setSelectedTemplate(template);
    setViewMode('preview');
  };

  const handleEditTemplate = async (template: SettlementTemplate) => {
    const fullTemplate = await getTemplate(template.id);
    if (fullTemplate) {
      setSelectedTemplate(fullTemplate);
      setViewMode('edit');
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, template: SettlementTemplate) => {
    setMenuAnchor(event.currentTarget);
    setSelectedMenuTemplate(template);
  };

  const handleMenuClose = () => {
    setMenuAnchor(null);
    setSelectedMenuTemplate(null);
  };

  const handleDeleteClick = (templateId: string) => {
    setTemplateToDelete(templateId);
    setDeleteConfirmOpen(true);
    handleMenuClose();
  };

  // Render list view
  if (viewMode === 'list') {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        {/* Header */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h4">Settlement Templates</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setViewMode('create')}
          >
            Create Template
          </Button>
        </Box>

        {/* Error Message */}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={clearError}>
            {error}
          </Alert>
        )}

        {/* Search */}
        <TextField
          fullWidth
          placeholder="Search templates by name or description..."
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            fetchTemplates({ searchTerm: e.target.value, pageNumber: 1 });
          }}
          sx={{ mb: 2 }}
        />

        {/* Loading */}
        {loading && <LinearProgress sx={{ mb: 2 }} />}

        {/* Templates Table */}
        {templates.length > 0 ? (
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow sx={{ backgroundColor: 'background.default' }}>
                  <TableCell sx={{ fontWeight: 600 }}>Name</TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>Description</TableCell>
                  <TableCell sx={{ fontWeight: 600 }} align="center">
                    Times Used
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600 }} align="center">
                    Visibility
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600 }} align="right">
                    Actions
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {templates.map((template) => (
                  <TableRow key={template.id} hover>
                    <TableCell>
                      <Typography
                        variant="body2"
                        sx={{ fontWeight: 600, cursor: 'pointer', color: 'primary.main' }}
                        onClick={() => handleViewTemplate(template)}
                      >
                        {template.name}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" color="textSecondary" noWrap>
                        {template.description}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">{template.timesUsed}</TableCell>
                    <TableCell align="center">
                      {template.isPublic ? (
                        <Chip label="Public" size="small" color="primary" variant="outlined" />
                      ) : (
                        <Chip label="Private" size="small" variant="outlined" />
                      )}
                    </TableCell>
                    <TableCell align="right">
                      <IconButton
                        size="small"
                        onClick={(e) => handleMenuOpen(e, template)}
                      >
                        <MoreVertIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Card>
            <CardContent sx={{ textAlign: 'center', py: 4 }}>
              <Typography color="textSecondary" gutterBottom>
                {searchTerm ? 'No templates found matching your search' : 'No templates created yet'}
              </Typography>
              {!searchTerm && (
                <Button
                  variant="contained"
                  startIcon={<AddIcon />}
                  onClick={() => setViewMode('create')}
                  sx={{ mt: 2 }}
                >
                  Create First Template
                </Button>
              )}
            </CardContent>
          </Card>
        )}

        {/* Context Menu */}
        <Menu
          anchorEl={menuAnchor}
          open={Boolean(menuAnchor)}
          onClose={handleMenuClose}
        >
          <MenuItem onClick={() => {
            if (selectedMenuTemplate) handleViewTemplate(selectedMenuTemplate);
            handleMenuClose();
          }}>
            <ListItemIcon><EditIcon fontSize="small" /></ListItemIcon>
            <ListItemText>View</ListItemText>
          </MenuItem>
          <MenuItem onClick={() => {
            if (selectedMenuTemplate) handleEditTemplate(selectedMenuTemplate);
            handleMenuClose();
          }}>
            <ListItemIcon><EditIcon fontSize="small" /></ListItemIcon>
            <ListItemText>Edit</ListItemText>
          </MenuItem>
          <MenuItem>
            <ListItemIcon><ShareIcon fontSize="small" /></ListItemIcon>
            <ListItemText>Share</ListItemText>
          </MenuItem>
          <Divider />
          <MenuItem
            onClick={() => {
              if (selectedMenuTemplate) handleDeleteClick(selectedMenuTemplate.id);
            }}
            sx={{ color: 'error.main' }}
          >
            <ListItemIcon><DeleteIcon fontSize="small" color="error" /></ListItemIcon>
            <ListItemText>Delete</ListItemText>
          </MenuItem>
        </Menu>

        {/* Delete Confirmation Dialog */}
        <Dialog open={deleteConfirmOpen} onClose={() => setDeleteConfirmOpen(false)}>
          <DialogTitle>Delete Template</DialogTitle>
          <Box sx={{ p: 2 }}>
            <Typography>
              Are you sure you want to delete this template? This action cannot be undone.
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1, p: 2 }}>
            <Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
            <Button
              variant="contained"
              color="error"
              onClick={handleDeleteConfirm}
            >
              Delete
            </Button>
          </Box>
        </Dialog>
      </Container>
    );
  }

  // Render create view
  if (viewMode === 'create') {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Button onClick={() => setViewMode('list')} sx={{ mb: 2 }}>
            ← Back to Templates
          </Button>
          <TemplateForm
            onSave={handleCreateTemplate}
            onCancel={() => setViewMode('list')}
          />
        </Box>
      </Container>
    );
  }

  // Render edit view
  if (viewMode === 'edit' && selectedTemplate) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Button onClick={() => setViewMode('list')} sx={{ mb: 2 }}>
            ← Back to Templates
          </Button>
          <TemplateForm
            template={selectedTemplate}
            onSave={handleUpdateTemplate}
            onCancel={() => setViewMode('list')}
          />
        </Box>
      </Container>
    );
  }

  // Render preview view
  if (viewMode === 'preview' && selectedTemplate) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ mb: 3 }}>
          <Button onClick={() => setViewMode('list')} sx={{ mb: 2 }}>
            ← Back to Templates
          </Button>
          <TemplatePreview
            template={selectedTemplate}
            onApply={() => {
              // TODO: Add to settlement form
              setViewMode('list');
            }}
            onEdit={() => handleEditTemplate(selectedTemplate)}
            onDelete={() => handleDeleteClick(selectedTemplate.id)}
          />
        </Box>
      </Container>
    );
  }

  return null;
};

export default SettlementTemplatesPage;
