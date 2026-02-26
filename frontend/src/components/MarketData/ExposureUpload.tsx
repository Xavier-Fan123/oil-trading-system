import React, { useState, useRef, useCallback } from 'react';
import {
  Box,
  Paper,
  Typography,
  Card,
  CardContent,
  Button,
  Alert,
  Grid,
  Tab,
  Tabs,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
} from '@mui/material';
import {
  CloudUpload as UploadIcon,
  Assessment as AssessmentIcon,
  ViewList as ViewListIcon,
  History as HistoryIcon,
  Description as FileIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { useUploadExposure } from '@/hooks/useMarketData';
import type { ExposureVaRResultDto } from '@/types/marketData';

interface ExposureUploadProps {
  onTabChange: (tab: 'upload' | 'latest' | 'history' | 'exposure') => void;
}

const formatNumber = (value: number, decimals = 2): string => {
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);
};

const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
};

export const ExposureUpload: React.FC<ExposureUploadProps> = ({ onTabChange }) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isDragActive, setIsDragActive] = useState(false);
  const [result, setResult] = useState<ExposureVaRResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const uploadMutation = useUploadExposure();

  const handleFileSelect = useCallback((files: FileList | null) => {
    if (!files || files.length === 0) return;
    const file = files[0];

    // Validate file type
    const ext = file.name.toLowerCase();
    if (!ext.endsWith('.xlsx') && !ext.endsWith('.xls')) {
      setError('Only Excel files (.xlsx, .xls) are supported.');
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setError('File size must be less than 10MB.');
      return;
    }

    setSelectedFile(file);
    setError(null);
    setResult(null);
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

  const handleUpload = async () => {
    if (!selectedFile) return;

    setError(null);
    try {
      const res = await uploadMutation.mutateAsync(selectedFile);
      setResult(res);
    } catch (err: any) {
      const message = err?.response?.data?.error || err?.message || 'Upload failed';
      setError(message);
    }
  };

  const handleClear = () => {
    setSelectedFile(null);
    setResult(null);
    setError(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <Box>
      {/* Header with Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value="exposure" onChange={(_, value) => onTabChange(value)}>
          <Tab label="Upload Data" value="upload" icon={<UploadIcon />} iconPosition="start" />
          <Tab label="Latest Prices" value="latest" icon={<ViewListIcon />} iconPosition="start" />
          <Tab label="Price History" value="history" icon={<HistoryIcon />} iconPosition="start" />
          <Tab label="Exposure VaR" value="exposure" icon={<AssessmentIcon />} iconPosition="start" />
        </Tabs>
      </Box>

      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h5" gutterBottom>
          Exposure Table Upload (敞口表)
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Upload an Excel file with your exposure positions (spot and futures) to automatically calculate VaR and CVaR.
        </Typography>
      </Box>

      <Grid container spacing={3}>
        {/* Upload Area */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              {/* Format Info */}
              <Alert severity="info" sx={{ mb: 3 }}>
                <Typography variant="subtitle2" gutterBottom>
                  Expected Format (Excel)
                </Typography>
                <Typography variant="body2" component="div">
                  <strong>Columns:</strong> 品种 | 合约月份 | 现货持仓 | 期货持仓 | 单位
                  <br />
                  <strong>Products:</strong> SG180, SG380, MF 0.5, GO 10ppm, Brt Fut
                  <br />
                  <strong>Units:</strong> MT or BBL | Positive = Long, Negative = Short
                </Typography>
              </Alert>

              {/* Drag & Drop Area */}
              <Paper
                role="button"
                tabIndex={0}
                aria-label="File upload area"
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
                  '&:hover': { backgroundColor: 'action.hover' },
                  '&:focus': { outline: '2px solid', outlineColor: 'primary.main', outlineOffset: 2 },
                }}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".xlsx,.xls"
                  onChange={(e) => handleFileSelect(e.target.files)}
                  style={{ display: 'none' }}
                />
                <UploadIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  {isDragActive ? 'Drop file here...' : 'Drag & drop exposure file, or click to select'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Supports Excel (.xlsx, .xls) up to 10MB
                </Typography>
              </Paper>

              {/* Selected File Info */}
              {selectedFile && (
                <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                  <FileIcon color="primary" />
                  <Typography variant="body2" sx={{ flex: 1 }}>
                    {selectedFile.name} ({(selectedFile.size / 1024).toFixed(1)} KB)
                  </Typography>
                  <Button size="small" color="error" startIcon={<DeleteIcon />} onClick={handleClear}>
                    Clear
                  </Button>
                </Box>
              )}

              {/* Upload Button */}
              {selectedFile && (
                <Button
                  fullWidth
                  variant="contained"
                  startIcon={uploadMutation.isPending ? <CircularProgress size={20} color="inherit" /> : <AssessmentIcon />}
                  onClick={handleUpload}
                  disabled={uploadMutation.isPending}
                  sx={{ mt: 2 }}
                >
                  {uploadMutation.isPending ? 'Calculating VaR...' : 'Upload & Calculate VaR'}
                </Button>
              )}

              {/* Error */}
              {error && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {error}
                </Alert>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Results Summary */}
        <Grid item xs={12} lg={6}>
          {result && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Portfolio Risk Summary
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
                  <Chip label={`${result.rowsParsed} rows parsed`} size="small" />
                  <Chip label={`${result.positionsProcessed} positions calculated`} size="small" color="primary" />
                  <Chip label={`Total Value: ${formatCurrency(result.totalPositionValue)}`} size="small" color="success" />
                </Box>

                {/* Warnings */}
                {result.warnings.length > 0 && (
                  <Alert severity="warning" sx={{ mb: 2 }}>
                    {result.warnings.map((w, i) => (
                      <Typography key={i} variant="caption" display="block">
                        {w}
                      </Typography>
                    ))}
                  </Alert>
                )}

                {/* VaR/CVaR Summary Table */}
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell><strong>Metric</strong></TableCell>
                        <TableCell align="right"><strong>95% CI</strong></TableCell>
                        <TableCell align="right"><strong>99% CI</strong></TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      <TableRow>
                        <TableCell>VaR (1-day)</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioVar1Day95)}</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioVar1Day99)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell>CVaR (1-day)</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioCVaR1Day95)}</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioCVaR1Day99)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell>VaR (10-day)</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioVar10Day95)}</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioVar10Day99)}</TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell>CVaR (10-day)</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioCVaR10Day95)}</TableCell>
                        <TableCell align="right">{formatCurrency(result.portfolioCVaR10Day99)}</TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          )}
        </Grid>

        {/* Parsed Exposure Data */}
        {result && result.parsedExposures.length > 0 && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Parsed Exposures (敞口明细)
                </Typography>
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>品种 (Product)</TableCell>
                        <TableCell>合约月份 (Contract Month)</TableCell>
                        <TableCell align="right">现货持仓 (Spot)</TableCell>
                        <TableCell align="right">期货持仓 (Futures)</TableCell>
                        <TableCell>单位 (Unit)</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {result.parsedExposures.map((row, i) => (
                        <TableRow key={i}>
                          <TableCell>{row.productCode}</TableCell>
                          <TableCell>{row.contractMonth || '-'}</TableCell>
                          <TableCell align="right" sx={{ color: row.spotPosition > 0 ? 'success.main' : row.spotPosition < 0 ? 'error.main' : 'text.secondary' }}>
                            {row.spotPosition !== 0 ? formatNumber(row.spotPosition, 0) : '-'}
                          </TableCell>
                          <TableCell align="right" sx={{ color: row.futuresPosition > 0 ? 'success.main' : row.futuresPosition < 0 ? 'error.main' : 'text.secondary' }}>
                            {row.futuresPosition !== 0 ? formatNumber(row.futuresPosition, 0) : '-'}
                          </TableCell>
                          <TableCell>{row.unit}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
        )}

        {/* Position VaR Breakdown */}
        {result && result.positionVaRs.length > 0 && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Position VaR Breakdown (逐品种风险明细)
                </Typography>
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Product</TableCell>
                        <TableCell>Type</TableCell>
                        <TableCell>Month</TableCell>
                        <TableCell align="right">Quantity</TableCell>
                        <TableCell align="right">Price</TableCell>
                        <TableCell align="right">Value</TableCell>
                        <TableCell align="right">Vol (Daily)</TableCell>
                        <TableCell align="right">VaR 95%</TableCell>
                        <TableCell align="right">CVaR 95%</TableCell>
                        <TableCell align="right">VaR 99%</TableCell>
                        <TableCell align="right">CVaR 99%</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {result.positionVaRs.map((pos, i) => (
                        <TableRow key={i}>
                          <TableCell>{pos.productCode}</TableCell>
                          <TableCell>
                            <Chip
                              label={pos.positionType}
                              size="small"
                              color={pos.positionType === 'Spot' ? 'info' : 'warning'}
                              variant="outlined"
                            />
                          </TableCell>
                          <TableCell>{pos.contractMonth || '-'}</TableCell>
                          <TableCell align="right" sx={{ color: pos.quantity > 0 ? 'success.main' : 'error.main' }}>
                            {formatNumber(pos.quantity, 0)} {pos.unit}
                          </TableCell>
                          <TableCell align="right">{formatCurrency(pos.latestPrice)}</TableCell>
                          <TableCell align="right">{formatCurrency(pos.positionValue)}</TableCell>
                          <TableCell align="right">{(Number(pos.dailyVolatility) * 100).toFixed(2)}%</TableCell>
                          <TableCell align="right">{formatCurrency(pos.var1Day95)}</TableCell>
                          <TableCell align="right">{formatCurrency(pos.cVaR1Day95)}</TableCell>
                          <TableCell align="right">{formatCurrency(pos.var1Day99)}</TableCell>
                          <TableCell align="right">{formatCurrency(pos.cVaR1Day99)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};
