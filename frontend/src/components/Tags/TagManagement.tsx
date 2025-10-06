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
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { 
  TagCategory, 
  TagSummary, 
  CreateTagDto, 
  UpdateTagDto
} from '@/types/contracts';
import { tagApi, tagCategoryHelpers } from '@/services/tagApi';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

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
      id={`tag-tabpanel-${index}`}
      aria-labelledby={`tag-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

export const TagManagement: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState(0);
  const [selectedCategory, setSelectedCategory] = useState<TagCategory | 'all'>('all');
  const [openCreateDialog, setOpenCreateDialog] = useState(false);
  const [openEditDialog, setOpenEditDialog] = useState(false);
  const [editingTag, setEditingTag] = useState<TagSummary | null>(null);
  const [formData, setFormData] = useState<CreateTagDto>({
    name: '',
    description: '',
    category: TagCategory.Custom,
    priority: 0,
  });

  const queryClient = useQueryClient();

  // Query hooks
  const { data: tags, isLoading: loadingTags, refetch: refetchTags } = useQuery({
    queryKey: ['tags'],
    queryFn: tagApi.getTags,
  });

  const { data: predefinedTagInfo } = useQuery({
    queryKey: ['predefined-tag-info'],
    queryFn: tagApi.getPredefinedTagInfo,
  });

  const { data: tagStats } = useQuery({
    queryKey: ['tag-usage-statistics'],
    queryFn: tagApi.getTagUsageStatistics,
  });

  // Mutation hooks
  const createTagMutation = useMutation({
    mutationFn: tagApi.createTag,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
      setOpenCreateDialog(false);
      resetForm();
    },
  });

  const updateTagMutation = useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateTagDto }) => tagApi.updateTag(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      setOpenEditDialog(false);
      setEditingTag(null);
      resetForm();
    },
  });

  const deleteTagMutation = useMutation({
    mutationFn: tagApi.deleteTag,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
    },
  });

  const createPredefinedTagsMutation = useMutation({
    mutationFn: tagApi.createPredefinedTags,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
      queryClient.invalidateQueries({ queryKey: ['tag-usage-statistics'] });
    },
  });

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      category: TagCategory.Custom,
      priority: 0,
    });
  };

  const handleCreateTag = () => {
    createTagMutation.mutate(formData);
  };

  const handleUpdateTag = () => {
    if (!editingTag) return;
    
    const updateDto: UpdateTagDto = {
      name: formData.name !== editingTag.name ? formData.name : undefined,
      description: formData.description,
      priority: formData.priority,
    };

    updateTagMutation.mutate({ id: editingTag.id, dto: updateDto });
  };

  const handleEditTag = (tag: TagSummary) => {
    setEditingTag(tag);
    setFormData({
      name: tag.name,
      description: '',
      category: tag.category,
      priority: 0,
    });
    setOpenEditDialog(true);
  };

  const handleDeleteTag = (tagId: string) => {
    if (window.confirm('Are you sure you want to delete this tag?')) {
      deleteTagMutation.mutate(tagId);
    }
  };

  const filteredTags = tags?.filter(tag => 
    selectedCategory === 'all' || tag.category === selectedCategory
  ) || [];

  const categories = tagCategoryHelpers.getAllCategories();

  if (loadingTags) {
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
          Tag Management
        </Typography>
        <Box>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => refetchTags()}
            sx={{ mr: 2 }}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setOpenCreateDialog(true)}
          >
            New Tag
          </Button>
        </Box>
      </Box>

      <Tabs value={selectedTab} onChange={(_, newValue) => setSelectedTab(newValue)} sx={{ mb: 3 }}>
        <Tab label="Manage Tags" />
        <Tab label="Predefined Tags" />
        <Tab label="Usage Statistics" />
      </Tabs>

      <TabPanel value={selectedTab} index={0}>
        {/* Category Filter */}
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box display="flex" alignItems="center" gap={2} mb={2}>
              <FormControl size="small" sx={{ minWidth: 250 }}>
                <InputLabel>Filter by Category</InputLabel>
                <Select
                  value={selectedCategory}
                  label="Filter by Category"
                  onChange={(e) => setSelectedCategory(e.target.value as TagCategory | 'all')}
                >
                  <MenuItem value="all">All Categories</MenuItem>
                  {categories.map(category => (
                    <MenuItem key={category.value} value={category.value}>
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={category.color}
                          borderRadius="50%"
                          mr={1}
                        />
                        {category.label}
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              
              {selectedCategory !== 'all' && (
                <Chip
                  label={`${filteredTags.length} tags`}
                  size="small"
                  color="primary"
                  variant="outlined"
                />
              )}
            </Box>
            
            {selectedCategory !== 'all' && (
              <Alert severity="info" variant="outlined" sx={{ fontSize: '0.875rem' }}>
                <strong>{categories.find(c => c.value === selectedCategory)?.label}:</strong>{' '}
                {selectedCategory && tagCategoryHelpers.getCategoryDescription(selectedCategory as TagCategory)}
              </Alert>
            )}
          </CardContent>
        </Card>

        {/* Tags Table */}
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Category</TableCell>
                <TableCell>Color</TableCell>
                <TableCell align="right">Usage Count</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="center">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredTags.map((tag) => (
                <TableRow key={tag.id} hover>
                  <TableCell>
                    <Box display="flex" alignItems="center">
                      <Chip
                        label={tag.name}
                        size="small"
                        sx={{ 
                          backgroundColor: tag.color,
                          color: 'white',
                          mr: 1
                        }}
                      />
                    </Box>
                  </TableCell>
                  <TableCell>{tag.categoryDisplayName}</TableCell>
                  <TableCell>
                    <Box display="flex" alignItems="center">
                      <Box
                        width={20}
                        height={20}
                        bgcolor={tag.color}
                        borderRadius="4px"
                        mr={1}
                      />
                      {tag.color}
                    </Box>
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2" fontWeight="medium">
                      {tag.usageCount}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={tag.isActive ? 'Active' : 'Inactive'}
                      color={tag.isActive ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Edit">
                      <IconButton size="small" onClick={() => handleEditTag(tag)}>
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton 
                        size="small" 
                        color="error"
                        onClick={() => handleDeleteTag(tag.id)}
                        disabled={tag.usageCount > 0}
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </TabPanel>

      <TabPanel value={selectedTab} index={1}>
        <Alert severity="info" sx={{ mb: 3 }}>
          Predefined tags are business-relevant tags optimized for oil trading strategy management, risk control, and position management.
          Trading Strategy tags align with TradeGroup strategies for integrated futures-spot trading.
        </Alert>
        
        <Box mb={3}>
          <Button
            variant="contained"
            onClick={() => createPredefinedTagsMutation.mutate()}
            disabled={createPredefinedTagsMutation.isPending}
          >
            {createPredefinedTagsMutation.isPending ? 'Creating...' : 'Create All Predefined Tags'}
          </Button>
        </Box>

        <Grid container spacing={2}>
          {predefinedTagInfo?.map((info) => (
            <Grid item xs={12} md={6} lg={4} key={info.category}>
              <Card>
                <CardContent>
                  <Box display="flex" alignItems="center" mb={2}>
                    <Box
                      width={16}
                      height={16}
                      bgcolor={info.defaultColor}
                      borderRadius="50%"
                      mr={1}
                    />
                    <Typography variant="h6">{info.categoryDisplayName}</Typography>
                  </Box>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    {info.categoryDescription}
                  </Typography>
                  <Box mt={2}>
                    {info.predefinedNames.map((name) => (
                      <Chip
                        key={name}
                        label={name}
                        size="small"
                        sx={{ 
                          mr: 1, 
                          mb: 1,
                          backgroundColor: info.defaultColor,
                          color: 'white'
                        }}
                      />
                    ))}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      <TabPanel value={selectedTab} index={2}>
        {tagStats && (
          <Grid container spacing={3}>
            <Grid item xs={12} md={4}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>Overview</Typography>
                  <Typography variant="body2">Total Tags: {tagStats.totalTags}</Typography>
                  <Typography variant="body2">Active Tags: {tagStats.activeTags}</Typography>
                  <Typography variant="body2">Unused Tags: {tagStats.unusedTags}</Typography>
                </CardContent>
              </Card>
            </Grid>
            
            <Grid item xs={12} md={8}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>Tags by Category</Typography>
                  <Grid container spacing={2}>
                    {Object.entries(tagStats.tagsByCategory).map(([category, count]) => (
                      <Grid item xs={6} sm={4} key={category}>
                        <Box textAlign="center">
                          <Typography variant="h4" color="primary">{count}</Typography>
                          <Typography variant="body2">{category}</Typography>
                        </Box>
                      </Grid>
                    ))}
                  </Grid>
                </CardContent>
              </Card>
            </Grid>

            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>Most Used Tags</Typography>
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Tag Name</TableCell>
                          <TableCell>Category</TableCell>
                          <TableCell align="right">Usage Count</TableCell>
                          <TableCell>Last Used</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {tagStats.mostUsedTags.slice(0, 10).map((tag) => (
                          <TableRow key={tag.tagId}>
                            <TableCell>{tag.tagName}</TableCell>
                            <TableCell>{tag.categoryDisplayName}</TableCell>
                            <TableCell align="right">{tag.usageCount}</TableCell>
                            <TableCell>
                              {tag.lastUsedAt ? format(new Date(tag.lastUsedAt), 'MMM dd, yyyy') : '—'}
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

      {/* Create Tag Dialog */}
      <Dialog open={openCreateDialog} onClose={() => setOpenCreateDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Create New Tag</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Tag Name"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
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
                rows={2}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={formData.category}
                  label="Category"
                  onChange={(e) => setFormData(prev => ({ ...prev, category: e.target.value as TagCategory }))}
                >
                  {categories.map(category => (
                    <MenuItem key={category.value} value={category.value}>
                      <Box display="flex" alignItems="center">
                        <Box
                          width={12}
                          height={12}
                          bgcolor={category.color}
                          borderRadius="50%"
                          mr={1}
                        />
                        {category.label}
                      </Box>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Priority"
                type="number"
                value={formData.priority}
                onChange={(e) => setFormData(prev => ({ ...prev, priority: parseInt(e.target.value) || 0 }))}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenCreateDialog(false)}>Cancel</Button>
          <Button 
            onClick={handleCreateTag} 
            variant="contained"
            disabled={!formData.name.trim() || createTagMutation.isPending}
          >
            {createTagMutation.isPending ? 'Creating...' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Tag Dialog */}
      <Dialog open={openEditDialog} onClose={() => setOpenEditDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Tag</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Tag Name"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
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
                rows={2}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Priority"
                type="number"
                value={formData.priority}
                onChange={(e) => setFormData(prev => ({ ...prev, priority: parseInt(e.target.value) || 0 }))}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenEditDialog(false)}>Cancel</Button>
          <Button 
            onClick={handleUpdateTag} 
            variant="contained"
            disabled={!formData.name.trim() || updateTagMutation.isPending}
          >
            {updateTagMutation.isPending ? 'Updating...' : 'Update'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};