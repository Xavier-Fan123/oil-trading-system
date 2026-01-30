import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Box,
  CircularProgress,
  Alert,
  Pagination,
  Stack,
  Tabs,
  Tab,
  Typography,
  FormControlLabel,
  Checkbox,
} from '@mui/material';
import { Search as SearchIcon } from '@mui/icons-material';
import { templateApi, SettlementTemplate } from '@/services/templateApi';
import { TemplatePreview } from './TemplatePreview';

interface TemplateSelectorProps {
  open: boolean;
  onClose: () => void;
  onSelect: (template: SettlementTemplate) => void;
  isLoading?: boolean;
}

type TabType = 'recent' | 'popular' | 'all' | 'public';

interface TabItem {
  label: string;
  value: TabType;
  description: string;
}

const tabs: TabItem[] = [
  { label: 'Recently Used', value: 'recent', description: 'Your recently used templates' },
  { label: 'Most Popular', value: 'popular', description: 'Most used templates' },
  { label: 'My Templates', value: 'all', description: 'All your templates' },
  { label: 'Public', value: 'public', description: 'Templates shared with you' },
];

export const TemplateSelector: React.FC<TemplateSelectorProps> = ({
  open,
  onClose,
  onSelect,
  isLoading = false,
}) => {
  const [activeTab, setActiveTab] = useState<TabType>('recent');
  const [searchTerm, setSearchTerm] = useState('');
  const [templates, setTemplates] = useState<SettlementTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize] = useState(5);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedTemplate, setSelectedTemplate] = useState<SettlementTemplate | null>(null);
  const [onlyActive, setOnlyActive] = useState(true);

  // Fetch templates based on active tab
  useEffect(() => {
    if (!open) return;

    const fetchTemplates = async () => {
      setLoading(true);
      setError(null);
      try {
        let data;

        switch (activeTab) {
          case 'recent': {
            const response = await templateApi.getRecentlyUsedTemplates(5);
            // Convert SettlementTemplateSummary[] to SettlementTemplate[] by casting
            setTemplates(response as SettlementTemplate[]);
            setTotalPages(1);
            break;
          }

          case 'popular': {
            const response = await templateApi.getMostUsedTemplates(5);
            // Convert SettlementTemplateSummary[] to SettlementTemplate[] by casting
            setTemplates(response as SettlementTemplate[]);
            setTotalPages(1);
            break;
          }

          case 'all': {
            const response = await templateApi.getAccessibleTemplates(pageNumber, pageSize);
            // Convert PagedResult<SettlementTemplateSummary> to SettlementTemplate[]
            setTemplates(response.data as unknown as SettlementTemplate[]);
            setTotalPages(response.totalPages);
            break;
          }

          case 'public': {
            const response = await templateApi.getPublicTemplates(pageNumber, pageSize);
            // Convert PagedResult<SettlementTemplateSummary> to SettlementTemplate[]
            setTemplates(response.data as unknown as SettlementTemplate[]);
            setTotalPages(response.totalPages);
            break;
          }
        }

        setPageNumber(1);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load templates');
        setTemplates([]);
      } finally {
        setLoading(false);
      }
    };

    fetchTemplates();
  }, [activeTab, open, pageNumber, pageSize]);

  // Handle search
  useEffect(() => {
    if (!open || !searchTerm) return;

    const performSearch = async () => {
      setLoading(true);
      setError(null);
      try {
        const response = await templateApi.searchTemplates(searchTerm, pageNumber, pageSize);
        // Convert PagedResult<SettlementTemplateSummary> to SettlementTemplate[]
        setTemplates(response.data as unknown as SettlementTemplate[]);
        setTotalPages(response.totalPages);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Search failed');
        setTemplates([]);
      } finally {
        setLoading(false);
      }
    };

    // Debounce search
    const timer = setTimeout(performSearch, 300);
    return () => clearTimeout(timer);
  }, [searchTerm, pageNumber, pageSize, open]);

  const handleSelectTemplate = (template: SettlementTemplate) => {
    setSelectedTemplate(template);
  };

  const handleConfirmSelection = () => {
    if (selectedTemplate) {
      onSelect(selectedTemplate);
      setSelectedTemplate(null);
      setSearchTerm('');
      handleClose();
    }
  };

  const handleClose = () => {
    setSelectedTemplate(null);
    setSearchTerm('');
    setPageNumber(1);
    setError(null);
    onClose();
  };

  const handlePageChange = (event: React.ChangeEvent<unknown>, value: number) => {
    setPageNumber(value);
  };

  const filteredTemplates =
    activeTab === 'recent' || activeTab === 'popular'
      ? templates
      : templates.filter(t => !onlyActive || t.isActive);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        Load Settlement Template
        <Typography variant="caption" display="block" color="textSecondary" sx={{ mt: 0.5 }}>
          Select a template to pre-populate settlement form fields
        </Typography>
      </DialogTitle>

      <DialogContent dividers>
        {/* Search Bar */}
        <TextField
          fullWidth
          size="small"
          placeholder="Search templates..."
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          InputProps={{
            startAdornment: <SearchIcon sx={{ mr: 1, color: 'action.disabled' }} />,
          }}
          sx={{ mb: 2 }}
        />

        {/* Tabs */}
        <Tabs
          value={activeTab}
          onChange={(e, newValue) => {
            setActiveTab(newValue);
            setPageNumber(1);
            setSelectedTemplate(null);
          }}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ mb: 2, borderBottom: '1px solid', borderColor: 'divider' }}
        >
          {tabs.map((tab) => (
            <Tab
              key={tab.value}
              label={tab.label}
              value={tab.value}
              title={tab.description}
            />
          ))}
        </Tabs>

        {/* Filter Options */}
        {(activeTab === 'all' || activeTab === 'public') && (
          <Box sx={{ mb: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={onlyActive}
                  onChange={(e) => setOnlyActive(e.target.checked)}
                />
              }
              label="Show only active templates"
            />
          </Box>
        )}

        {/* Error Message */}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {/* Loading State */}
        {loading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress />
          </Box>
        )}

        {/* Empty State */}
        {!loading && filteredTemplates.length === 0 && (
          <Box sx={{ textAlign: 'center', py: 3 }}>
            <Typography color="textSecondary">
              {searchTerm
                ? 'No templates found matching your search'
                : activeTab === 'recent'
                ? 'No recently used templates'
                : activeTab === 'popular'
                ? 'No templates yet'
                : 'No templates available'}
            </Typography>
          </Box>
        )}

        {/* Templates List */}
        {!loading && filteredTemplates.length > 0 && (
          <Stack spacing={2}>
            {filteredTemplates.map((template) => (
              <Box
                key={template.id}
                onClick={() => handleSelectTemplate(template)}
                sx={{
                  p: 2,
                  border: '1px solid',
                  borderColor:
                    selectedTemplate?.id === template.id
                      ? 'primary.main'
                      : 'divider',
                  borderRadius: 1,
                  cursor: 'pointer',
                  bgcolor:
                    selectedTemplate?.id === template.id
                      ? 'action.selected'
                      : 'transparent',
                  transition: 'all 0.2s ease',
                  '&:hover': {
                    borderColor: 'primary.main',
                    bgcolor: 'action.hover',
                  },
                }}
              >
                <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                  <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                    {template.name}
                  </Typography>
                  {template.isPublic && (
                    <Typography variant="caption" color="primary">
                      Shared
                    </Typography>
                  )}
                </Box>
                <Typography variant="caption" color="textSecondary" display="block" sx={{ mb: 1 }}>
                  {template.description}
                </Typography>
                <Typography variant="caption" color="textSecondary">
                  Used {template.timesUsed} times • By {template.createdByUserName}
                </Typography>
              </Box>
            ))}
          </Stack>
        )}

        {/* Pagination */}
        {!loading && filteredTemplates.length > 0 && totalPages > 1 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
            <Pagination
              count={totalPages}
              page={pageNumber}
              onChange={handlePageChange}
              size="small"
            />
          </Box>
        )}

        {/* Selected Template Preview */}
        {selectedTemplate && (
          <>
            <Box sx={{ my: 3, p: 2, bgcolor: 'info.light', borderRadius: 1 }}>
              <Typography variant="subtitle2" color="info.dark">
                Selected Template:
              </Typography>
              <Typography variant="body2" color="info.dark" sx={{ fontWeight: 600 }}>
                {selectedTemplate.name}
              </Typography>
            </Box>
            <TemplatePreview
              template={selectedTemplate}
              onApply={handleConfirmSelection}
              compact={true}
              isLoading={isLoading}
            />
          </>
        )}
      </DialogContent>

      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button
          variant="contained"
          onClick={handleConfirmSelection}
          disabled={!selectedTemplate || isLoading || loading}
        >
          Use Selected Template
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default TemplateSelector;
