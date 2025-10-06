import React, { useState, useRef, useCallback } from 'react';
import {
  Box,
  Paper,
  Typography,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  LinearProgress,
  Alert,
  Chip,
  Grid,
  List,
  ListItem,
  ListItemIcon,
  Divider,
  Tab,
  Tabs,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  Description as FileIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
  ViewList as ViewListIcon,
  History as HistoryIcon,
} from '@mui/icons-material';
import {
  Checkbox,
  FormControlLabel,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import {
  useUploadMarketData,
  useImportStatus,
  useBulkImport,
  useDeleteAllMarketData,
} from '@/hooks/useMarketData';
import { FILE_TYPES, type FileType } from '@/types/marketData';
import { marketDataApi } from '@/services/marketDataApi';

interface MarketDataUploadProps {
  onTabChange: (tab: 'upload' | 'latest' | 'history') => void;
}

interface FileWithPreview extends File {
  uploadStatus?: 'pending' | 'uploading' | 'success' | 'error';
  uploadProgress?: number;
  result?: any;
  error?: string;
  preview?: string;
}

export const MarketDataUpload: React.FC<MarketDataUploadProps> = ({ onTabChange }) => {
  const [selectedFiles, setSelectedFiles] = useState<FileWithPreview[]>([]);
  const [fileType, setFileType] = useState<FileType>('Spot');
  const [uploadResults, setUploadResults] = useState<any[]>([]);
  const [isDragActive, setIsDragActive] = useState(false);
  const [overwriteExisting, setOverwriteExisting] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteReason, setDeleteReason] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Mutations
  const uploadMutation = useUploadMarketData();
  const bulkImportMutation = useBulkImport();
  const deleteAllMutation = useDeleteAllMarketData();

  // Queries
  const { data: importStatus, refetch: refetchStatus } = useImportStatus();

  const handleFileSelect = useCallback((files: FileList | null) => {
    if (!files) return;

    const newFiles: FileWithPreview[] = Array.from(files).map(file => {
      // Create a new object that extends the File object without spreading
      const fileWithPreview = Object.assign(file, {
        uploadStatus: 'pending' as const,
        uploadProgress: undefined,
        result: undefined,
        error: undefined,
      });
      return fileWithPreview as FileWithPreview;
    });

    setSelectedFiles(prev => [...prev, ...newFiles]);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(false);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(false);
    handleFileSelect(e.dataTransfer.files);
  }, [handleFileSelect]);

  const handleFileInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    handleFileSelect(e.target.files);
  }, [handleFileSelect]);

  const removeFile = (index: number) => {
    setSelectedFiles(prev => {
      const newFiles = [...prev];
      newFiles.splice(index, 1);
      return newFiles;
    });
  };

  const uploadSingleFile = async (file: FileWithPreview, index: number) => {
    console.log('uploadSingleFile called for:', file.name, 'fileType:', fileType);
    
    setSelectedFiles(prev => 
      prev.map((f, i) => 
        i === index ? { ...f, uploadStatus: 'uploading', uploadProgress: 0 } : f
      )
    );

    try {
      console.log('Validating file:', file.name);
      const validation = marketDataApi.validateFile(file);
      if (!validation.isValid) {
        console.error('File validation failed:', validation.error);
        throw new Error(validation.error);
      }

      console.log('Starting upload mutation for:', file.name);
      const result = await uploadMutation.mutateAsync({ file, fileType, overwriteExisting });
      console.log('Upload successful:', result);
      
      setSelectedFiles(prev => 
        prev.map((f, i) => 
          i === index ? { 
            ...f, 
            uploadStatus: 'success', 
            uploadProgress: 100,
            result 
          } : f
        )
      );

      setUploadResults(prev => [...prev, { file: file.name, result }]);

    } catch (error: any) {
      console.error('Upload failed for:', file.name, error);
      setSelectedFiles(prev => 
        prev.map((f, i) => 
          i === index ? { 
            ...f, 
            uploadStatus: 'error',
            error: error.message || 'Upload failed'
          } : f
        )
      );
    }
  };

  const uploadAllFiles = async () => {
    console.log('Starting upload for', selectedFiles.length, 'files');
    
    for (let i = 0; i < selectedFiles.length; i++) {
      if (selectedFiles[i].uploadStatus === 'pending') {
        console.log('Uploading file', i, ':', selectedFiles[i].name);
        await uploadSingleFile(selectedFiles[i], i);
        // Small delay between uploads
        await new Promise(resolve => setTimeout(resolve, 500));
      }
    }
    
    console.log('Upload process completed');
  };

  const clearAll = () => {
    setSelectedFiles([]);
    setUploadResults([]);
  };

  const handleDeleteAllData = () => {
    setDeleteDialogOpen(true);
  };

  const confirmDeleteAllData = async () => {
    try {
      await deleteAllMutation.mutateAsync(deleteReason || undefined);
      setDeleteDialogOpen(false);
      setDeleteReason('');
      // Show success message or handle success
    } catch (error) {
      console.error('Failed to delete all market data:', error);
      // Handle error
    }
  };

  const handleBulkImport = async () => {
    try {
      await bulkImportMutation.mutateAsync();
      refetchStatus();
    } catch (error) {
      console.error('Bulk import failed:', error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'success': return 'success';
      case 'error': return 'error';
      case 'uploading': return 'info';
      default: return 'default';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'success': return <SuccessIcon />;
      case 'error': return <ErrorIcon />;
      case 'uploading': return <UploadIcon />;
      default: return <FileIcon />;
    }
  };

  return (
    <Box>
      {/* Header with Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value="upload" onChange={(_, value) => onTabChange(value)}>
          <Tab 
            label="Upload Data" 
            value="upload" 
            icon={<UploadIcon />}
            iconPosition="start"
          />
          <Tab 
            label="Latest Prices" 
            value="latest" 
            icon={<ViewListIcon />}
            iconPosition="start"
          />
          <Tab 
            label="Price History" 
            value="history" 
            icon={<HistoryIcon />}
            iconPosition="start"
          />
        </Tabs>
      </Box>

      {/* Header and Actions */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box>
          <Typography variant="h5" gutterBottom>
            Market Data Upload
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Upload Excel or CSV files containing market price data. Supported formats: XLSX, XLS, CSV (max 10MB)
          </Typography>
        </Box>
        <Box>
          <Button
            variant="outlined"
            color="error"
            startIcon={<DeleteIcon />}
            onClick={handleDeleteAllData}
            disabled={deleteAllMutation.isPending}
            sx={{ ml: 2 }}
          >
            Delete All Data
          </Button>
        </Box>
      </Box>

      <Grid container spacing={3}>
        {/* Upload Area */}
        <Grid item xs={12} lg={8}>
          <Card>
            <CardContent>
              <Box sx={{ mb: 3 }}>
                <FormControl fullWidth>
                  <InputLabel id="file-type-label">File Type</InputLabel>
                  <Select
                    labelId="file-type-label"
                    id="file-type-select"
                    value={fileType}
                    label="File Type"
                    onChange={(e) => setFileType(e.target.value as FileType)}
                    aria-describedby="file-type-helper-text"
                  >
                    {FILE_TYPES.map((type) => (
                      <MenuItem key={type.value} value={type.value}>
                        <Box component="span">
                          <Box component="span" fontSize="1rem" display="block">{type.label}</Box>
                          <Box component="span" fontSize="0.75rem" color="text.secondary" display="block">
                            {type.description}
                          </Box>
                        </Box>
                      </MenuItem>
                    ))}
                  </Select>
                  <Box id="file-type-helper-text" sx={{ mt: 1, fontSize: '0.75rem', color: 'text.secondary' }}>
                    Select the type of market data file you want to upload
                  </Box>
                </FormControl>
              </Box>

              {/* Overwrite Option */}
              <Box sx={{ mb: 2 }}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={overwriteExisting}
                      onChange={(e) => setOverwriteExisting(e.target.checked)}
                      color="warning"
                    />
                  }
                  label="Overwrite existing data"
                />
                {overwriteExisting && (
                  <Alert severity="warning" sx={{ mt: 1 }}>
                    <strong>Warning:</strong> This will delete all existing market data before uploading new data. This action cannot be undone.
                  </Alert>
                )}
              </Box>

              {/* File Upload Area */}
              <Paper
                role="button"
                tabIndex={0}
                aria-label="File upload area. Click to select files or drag and drop files here"
                aria-describedby="upload-instructions"
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    fileInputRef.current?.click();
                  }
                }}
                sx={{
                  p: 4,
                  textAlign: 'center',
                  backgroundColor: isDragActive ? 'action.hover' : 'background.default',
                  border: 2,
                  borderStyle: 'dashed',
                  borderColor: isDragActive ? 'primary.main' : 'divider',
                  cursor: 'pointer',
                  '&:hover': {
                    backgroundColor: 'action.hover',
                  },
                  '&:focus': {
                    outline: '2px solid',
                    outlineColor: 'primary.main',
                    outlineOffset: 2,
                  },
                }}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  multiple
                  accept=".xlsx,.xls,.csv"
                  onChange={handleFileInputChange}
                  style={{ display: 'none' }}
                  aria-label="Upload market data files (Excel or CSV format)"
                  title="Choose files to upload"
                  id="market-data-file-input"
                />
                <UploadIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom id="upload-instructions">
                  {isDragActive ? 'Drop files here...' : 'Drag & drop files here, or click to select'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Supports Excel (.xlsx, .xls) and CSV files up to 10MB
                </Typography>
              </Paper>

              {/* File List */}
              {selectedFiles.length > 0 && (
                <Box sx={{ mt: 3 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Typography variant="h6">Selected Files ({selectedFiles.length})</Typography>
                    <Box>
                      <Button onClick={clearAll} startIcon={<DeleteIcon />} color="error">
                        Clear All
                      </Button>
                      <Button 
                        onClick={uploadAllFiles} 
                        variant="contained" 
                        startIcon={<UploadIcon />}
                        disabled={uploadMutation.isPending}
                        sx={{ ml: 1 }}
                      >
                        Upload All
                      </Button>
                    </Box>
                  </Box>

                  <List>
                    {selectedFiles.map((file, index) => (
                      <ListItem key={index} divider>
                        <ListItemIcon>
                          {getStatusIcon(file.uploadStatus || 'pending')}
                        </ListItemIcon>
                        <Box sx={{ flex: 1, minWidth: 0 }}>
                          <Typography variant="body2" noWrap>
                            {file.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary" display="block">
                            {marketDataApi.formatFileSize(file.size)}
                          </Typography>
                          {file.uploadProgress !== undefined && (
                            <LinearProgress 
                              variant="determinate" 
                              value={file.uploadProgress} 
                              sx={{ mt: 0.5, height: 4 }}
                              aria-label={`Upload progress: ${file.uploadProgress}%`}
                            />
                          )}
                          {file.error && (
                            <Typography variant="caption" color="error" display="block" sx={{ mt: 0.5 }}>
                              {file.error}
                            </Typography>
                          )}
                        </Box>
                        <Chip
                          label={file.uploadStatus || 'pending'}
                          color={getStatusColor(file.uploadStatus || 'pending')}
                          size="small"
                        />
                        <Tooltip title="Remove file">
                          <span>
                            <IconButton 
                              onClick={() => removeFile(index)}
                              size="small"
                              sx={{ ml: 1 }}
                              aria-label="Remove file"
                            >
                              <DeleteIcon />
                            </IconButton>
                          </span>
                        </Tooltip>
                      </ListItem>
                    ))}
                  </List>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Status Panel */}
        <Grid item xs={12} lg={4}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                <Typography variant="h6">Import Status</Typography>
                <Tooltip title="Refresh status">
                  <span>
                    <IconButton 
                      onClick={() => refetchStatus()} 
                      size="small"
                      aria-label="Refresh import status"
                    >
                      <RefreshIcon />
                    </IconButton>
                  </span>
                </Tooltip>
              </Box>

              {importStatus && (
                <Box>
                  <Box sx={{ mb: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Import Status
                    </Typography>
                    <Chip
                      label={importStatus.isImporting ? 'Importing...' : 'Ready'}
                      color={importStatus.isImporting ? 'warning' : 'success'}
                      size="small"
                    />
                  </Box>

                  {importStatus.isImporting && (
                    <Box sx={{ mb: 2 }}>
                      <Typography variant="body2" color="text.secondary">
                        Progress: {importStatus.completedFiles}/{importStatus.totalFiles} files
                      </Typography>
                      <LinearProgress 
                        variant="determinate" 
                        value={importStatus.progress} 
                        sx={{ mt: 1 }}
                      />
                    </Box>
                  )}

                  <Box sx={{ mb: 2 }}>
                    <Typography variant="body2" color="text.secondary">
                      Last Import
                    </Typography>
                    <Typography variant="body2">
                      {importStatus.lastImport 
                        ? new Date(importStatus.lastImport).toLocaleString()
                        : 'Never'
                      }
                    </Typography>
                  </Box>

                  {importStatus.errors && importStatus.errors.length > 0 && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                      <Typography variant="body2" fontWeight="medium">
                        Recent Errors:
                      </Typography>
                      {importStatus.errors.slice(0, 3).map((error, index) => (
                        <Typography key={index} variant="caption" display="block">
                          • {error}
                        </Typography>
                      ))}
                    </Alert>
                  )}
                </Box>
              )}

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" gutterBottom>
                Quick Actions
              </Typography>
              
              <Button
                fullWidth
                variant="outlined"
                onClick={handleBulkImport}
                disabled={bulkImportMutation.isPending}
                sx={{ mb: 1 }}
              >
                Run Bulk Import
              </Button>
              
              <Button
                fullWidth
                variant="outlined"
                onClick={() => onTabChange('latest')}
              >
                View Latest Prices
              </Button>
            </CardContent>
          </Card>

          {/* Upload Results */}
          {uploadResults.length > 0 && (
            <Card sx={{ mt: 2 }}>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Upload Results
                </Typography>
                {uploadResults.map((result, index) => (
                  <Box key={index} sx={{ mb: 2 }}>
                    <Typography variant="body2" fontWeight="medium">
                      {result.file}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Processed: {result.result.recordsProcessed} | 
                      Inserted: {result.result.recordsInserted} | 
                      Updated: {result.result.recordsUpdated}
                    </Typography>
                    {result.result.errors?.length > 0 && (
                      <Alert severity="warning" sx={{ mt: 1 }}>
                        {result.result.errors.slice(0, 2).map((error: string, i: number) => (
                          <Typography key={i} variant="caption" display="block">
                            • {error}
                          </Typography>
                        ))}
                      </Alert>
                    )}
                  </Box>
                ))}
              </CardContent>
            </Card>
          )}
        </Grid>
      </Grid>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Delete All Market Data</DialogTitle>
        <DialogContent>
          <Alert severity="error" sx={{ mb: 2 }}>
            <strong>Warning:</strong> This action will permanently delete all market data from the system. This cannot be undone.
          </Alert>
          <TextField
            autoFocus
            margin="dense"
            label="Reason for deletion (optional)"
            fullWidth
            multiline
            rows={3}
            variant="outlined"
            value={deleteReason}
            onChange={(e) => setDeleteReason(e.target.value)}
            placeholder="Please provide a reason for this deletion..."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button 
            onClick={confirmDeleteAllData} 
            variant="contained" 
            color="error"
            disabled={deleteAllMutation.isPending}
          >
            {deleteAllMutation.isPending ? 'Deleting...' : 'Delete All Data'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};