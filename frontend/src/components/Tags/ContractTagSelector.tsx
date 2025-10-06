import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  Alert,
  Autocomplete,
  Tooltip,
} from '@mui/material';
import {
  Add as AddIcon,
  Close as CloseIcon,
  LocalOffer as TagIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  TagSummary, 
  AddContractTagDto,
  TagCategory 
} from '@/types/contracts';
import { tagApi, tagCategoryHelpers } from '@/services/tagApi';

interface ContractTagSelectorProps {
  contractId: string;
  contractType: string;
  contractTags?: TagSummary[];
  onTagsChange?: () => void;
}

export const ContractTagSelector: React.FC<ContractTagSelectorProps> = ({
  contractId,
  contractType,
  contractTags = [],
  onTagsChange,
}) => {
  const [openAddDialog, setOpenAddDialog] = useState(false);
  const [selectedTag, setSelectedTag] = useState<TagSummary | null>(null);
  const [notes, setNotes] = useState('');

  const queryClient = useQueryClient();

  // Query hooks
  const { data: allTags, isLoading: loadingTags } = useQuery({
    queryKey: ['tags'],
    queryFn: tagApi.getTags,
  });

  const { data: contractTagsData } = useQuery({
    queryKey: ['contract-tags', contractId, contractType],
    queryFn: () => tagApi.getContractTags(contractId, contractType),
    enabled: !!contractId,
  });

  // Mutation hooks
  const addTagMutation = useMutation({
    mutationFn: (dto: AddContractTagDto) => 
      tagApi.addTagToContract(contractId, contractType, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract-tags', contractId, contractType] });
      queryClient.invalidateQueries({ queryKey: ['tags'] }); // Update usage counts
      setOpenAddDialog(false);
      setSelectedTag(null);
      setNotes('');
      onTagsChange?.();
    },
  });

  const removeTagMutation = useMutation({
    mutationFn: (tagId: string) => 
      tagApi.removeTagFromContract(contractId, contractType, tagId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract-tags', contractId, contractType] });
      queryClient.invalidateQueries({ queryKey: ['tags'] }); // Update usage counts
      onTagsChange?.();
    },
  });

  const handleAddTag = () => {
    if (!selectedTag) return;
    
    addTagMutation.mutate({
      tagId: selectedTag.id,
      notes: notes.trim() || undefined,
    });
  };

  const handleRemoveTag = (tagId: string) => {
    if (window.confirm('Are you sure you want to remove this tag?')) {
      removeTagMutation.mutate(tagId);
    }
  };

  const displayTags = contractTagsData || contractTags;
  const existingTagIds = displayTags.map(tag => tag.id);
  const availableTags = allTags?.filter(tag => 
    tag.isActive && !existingTagIds.includes(tag.id)
  ) || [];

  // Group tags by category for better display
  const tagsByCategory = displayTags.reduce((acc, tag) => {
    if (!acc[tag.category]) {
      acc[tag.category] = [];
    }
    acc[tag.category].push(tag);
    return acc;
  }, {} as Record<TagCategory, TagSummary[]>);

  const categories = tagCategoryHelpers.getAllCategories();

  return (
    <Paper sx={{ p: 3 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6" display="flex" alignItems="center">
          <TagIcon sx={{ mr: 1 }} />
          Contract Tags
        </Typography>
        <Button
          variant="outlined"
          size="small"
          startIcon={<AddIcon />}
          onClick={() => setOpenAddDialog(true)}
          disabled={availableTags.length === 0}
        >
          Add Tag
        </Button>
      </Box>

      {displayTags.length === 0 ? (
        <Alert severity="info">
          No tags assigned to this contract yet. Click "Add Tag" to assign tags.
        </Alert>
      ) : (
        <Box>
          {categories.map(category => {
            const categoryTags = tagsByCategory[category.value] || [];
            if (categoryTags.length === 0) return null;

            return (
              <Box key={category.value} mb={2}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  {category.label}
                </Typography>
                <Box display="flex" flexWrap="wrap" gap={1}>
                  {categoryTags.map((tag) => (
                    <Chip
                      key={tag.id}
                      label={tag.name}
                      sx={{ 
                        backgroundColor: tag.color,
                        color: 'white',
                        '& .MuiChip-deleteIcon': {
                          color: 'white',
                        }
                      }}
                      onDelete={() => handleRemoveTag(tag.id)}
                      deleteIcon={
                        <Tooltip title="Remove tag">
                          <CloseIcon />
                        </Tooltip>
                      }
                    />
                  ))}
                </Box>
              </Box>
            );
          })}
        </Box>
      )}

      {/* Add Tag Dialog */}
      <Dialog open={openAddDialog} onClose={() => setOpenAddDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add Tag to Contract</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <Autocomplete
                value={selectedTag}
                onChange={(_, newValue) => setSelectedTag(newValue)}
                options={availableTags}
                getOptionLabel={(option) => option.name}
                groupBy={(option) => tagCategoryHelpers.getCategoryDisplayName(option.category)}
                renderOption={(props, option) => (
                  <li {...props}>
                    <Box display="flex" alignItems="center" width="100%">
                      <Chip
                        label={option.name}
                        size="small"
                        sx={{ 
                          backgroundColor: option.color,
                          color: 'white',
                          mr: 1
                        }}
                      />
                      <Box>
                        <Typography variant="body2">{option.name}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {option.categoryDisplayName} • Used {option.usageCount} times
                        </Typography>
                      </Box>
                    </Box>
                  </li>
                )}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Select Tag"
                    placeholder="Search tags..."
                    required
                  />
                )}
                loading={loadingTags}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Notes (Optional)"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                multiline
                rows={2}
                placeholder="Add any notes about this tag assignment..."
              />
            </Grid>
          </Grid>

          {selectedTag && (
            <Alert severity="info" sx={{ mt: 2 }}>
              <Typography variant="body2">
                <strong>{selectedTag.name}</strong> - {selectedTag.categoryDisplayName}
              </Typography>
              {selectedTag.usageCount > 0 && (
                <Typography variant="caption" display="block">
                  This tag has been used {selectedTag.usageCount} times across other contracts.
                </Typography>
              )}
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenAddDialog(false)}>Cancel</Button>
          <Button 
            onClick={handleAddTag} 
            variant="contained"
            disabled={!selectedTag || addTagMutation.isPending}
          >
            {addTagMutation.isPending ? 'Adding...' : 'Add Tag'}
          </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
};