import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Box,
  Divider,
  Switch,
  FormControlLabel,
  InputAdornment,
  IconButton,
  Tooltip,
  Alert,
  Paper,
  Stack,
  Collapse
} from '@mui/material';
import {
  Business as BusinessIcon,
  Person as PersonIcon,
  Email as EmailIcon,
  Phone as PhoneIcon,
  CreditCard as CreditIcon,
  CalendarToday as CalendarIcon,
  Add as AddIcon,
  Assessment as AssessmentIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Save as SaveIcon,
  Cancel as CancelIcon
} from '@mui/icons-material';
import { CreateTradingPartnerRequest, UpdateTradingPartnerRequest } from '../../types/tradingPartner';
import { 
  FinancialReportFormData, 
  FinancialReportGridRow,
  FINANCIAL_REPORT_FIELD_LABELS
} from '../../types/financialReport';
import { financialReportService } from '../../services/financialReportService';

interface TradingPartnerFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreateTradingPartnerRequest | UpdateTradingPartnerRequest) => void;
  initialData?: any;
}

export const TradingPartnerForm: React.FC<TradingPartnerFormProps> = ({
  open,
  onClose,
  onSubmit,
  initialData
}) => {
  const [formData, setFormData] = useState({
    companyName: '',
    partnerType: 'Supplier',
    contactPerson: '',
    contactEmail: '',
    contactPhone: '',
    address: '',
    taxNumber: '',
    creditLimit: 0,
    creditLimitValidUntil: '',
    paymentTermDays: 30,
    isActive: true,
    isBlocked: false,
    blockReason: ''
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  
  // Financial Reports State
  const [financialReports, setFinancialReports] = useState<FinancialReportGridRow[]>([]);
  const [showFinancialReports, setShowFinancialReports] = useState(false);
  const [editingReportId, setEditingReportId] = useState<string | null>(null);
  const [newReportData, setNewReportData] = useState<Partial<FinancialReportFormData>>({});
  const [reportErrors, setReportErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (initialData) {
      setFormData({
        companyName: initialData.companyName || '',
        partnerType: initialData.partnerType || 'Supplier',
        contactPerson: initialData.contactPerson || '',
        contactEmail: initialData.contactEmail || '',
        contactPhone: initialData.contactPhone || '',
        address: initialData.address || '',
        taxNumber: initialData.taxNumber || '',
        creditLimit: initialData.creditLimit || 0,
        creditLimitValidUntil: initialData.creditLimitValidUntil ? 
          new Date(initialData.creditLimitValidUntil).toISOString().split('T')[0] : '',
        paymentTermDays: initialData.paymentTermDays || 30,
        isActive: initialData.isActive ?? true,
        isBlocked: initialData.isBlocked ?? false,
        blockReason: initialData.blockReason || ''
      });
    } else {
      // Reset form for new partner
      setFormData({
        companyName: '',
        partnerType: 'Supplier',
        contactPerson: '',
        contactEmail: '',
        contactPhone: '',
        address: '',
        taxNumber: '',
        creditLimit: 0,
        creditLimitValidUntil: '',
        paymentTermDays: 30,
        isActive: true,
        isBlocked: false,
        blockReason: ''
      });
    }
    setErrors({});

    // Load financial reports for existing trading partner
    if (initialData?.id && open) {
      loadFinancialReports(initialData.id);
      setShowFinancialReports(true);
    } else {
      setFinancialReports([]);
      setShowFinancialReports(false);
    }
  }, [initialData, open]);

  const handleInputChange = (field: string, value: any) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
    
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: ''
      }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.companyName.trim()) {
      newErrors.companyName = 'Company name is required';
    }

    if (!formData.partnerType) {
      newErrors.partnerType = 'Partner type is required';
    }

    if (formData.contactEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.contactEmail)) {
      newErrors.contactEmail = 'Invalid email format';
    }

    if (formData.creditLimit < 0) {
      newErrors.creditLimit = 'Credit limit cannot be negative';
    }

    if (!formData.creditLimitValidUntil) {
      newErrors.creditLimitValidUntil = 'Credit limit valid until date is required';
    }

    if (formData.paymentTermDays < 0) {
      newErrors.paymentTermDays = 'Payment terms cannot be negative';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = () => {
    if (!validateForm()) {
      return;
    }

    const submitData = {
      ...formData,
      creditLimitValidUntil: new Date(formData.creditLimitValidUntil).toISOString()
    };

    onSubmit(submitData);
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', { 
      style: 'currency', 
      currency: 'USD' 
    }).format(amount);
  };

  // Financial Reports Handlers
  const loadFinancialReports = async (tradingPartnerId: string) => {
    try {
      const reports = await financialReportService.getFinancialReports(tradingPartnerId);
      const gridRows: FinancialReportGridRow[] = reports.map(report => ({
        id: report.id,
        reportStartDate: report.reportStartDate,
        reportEndDate: report.reportEndDate,
        revenue: report.revenue ?? null,
        netIncome: report.netIncome ?? null,
        totalAssets: report.totalAssets ?? null,
        totalEquity: report.totalEquity ?? null,
        isAudited: report.isAudited || false,
        createdAt: report.createdAt
      }));
      setFinancialReports(gridRows);
    } catch (error) {
      console.error('Failed to load financial reports:', error);
    }
  };

  const initializeNewReport = () => {
    const currentYear = new Date().getFullYear();
    const startDate = new Date(currentYear, 0, 1); // Jan 1st
    const endDate = new Date(currentYear, 11, 31); // Dec 31st
    
    setNewReportData({
      reportStartDate: startDate.toISOString().split('T')[0],
      reportEndDate: endDate.toISOString().split('T')[0],
      revenue: '',
      costOfGoodsSold: '',
      grossProfit: '',
      operatingExpenses: '',
      operatingIncome: '',
      interestExpense: '',
      interestIncome: '',
      netIncome: '',
      totalAssets: '',
      currentAssets: '',
      nonCurrentAssets: '',
      totalLiabilities: '',
      currentLiabilities: '',
      longTermDebt: '',
      totalEquity: '',
      retainedEarnings: '',
      operatingCashFlow: '',
      investingCashFlow: '',
      financingCashFlow: '',
      netCashFlow: '',
      cashAndEquivalents: '',
      workingCapital: '',
      totalDebt: '',
      bookValue: '',
      notes: '',
      isAudited: false,
      auditFirm: ''
    });
  };

  const validateFinancialReport = (data: Partial<FinancialReportFormData>): boolean => {
    const newErrors: Record<string, string> = {};

    if (!data.reportStartDate) {
      newErrors.reportStartDate = 'Report start date is required';
    }

    if (!data.reportEndDate) {
      newErrors.reportEndDate = 'Report end date is required';
    }

    if (data.reportStartDate && data.reportEndDate) {
      const startDate = new Date(data.reportStartDate);
      const endDate = new Date(data.reportEndDate);
      
      if (startDate >= endDate) {
        newErrors.reportEndDate = 'End date must be after start date';
      }

      if (endDate > new Date()) {
        newErrors.reportEndDate = 'End date cannot be in the future';
      }

      const daysDiff = Math.abs(endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24);
      if (daysDiff > 366) {
        newErrors.reportEndDate = 'Report period cannot exceed 366 days';
      }
    }

    // Validate positive numbers for financial fields
    const financialFields = [
      'revenue', 'costOfGoodsSold', 'grossProfit', 'operatingExpenses', 'operatingIncome',
      'interestExpense', 'interestIncome', 'totalAssets', 'currentAssets', 'nonCurrentAssets',
      'totalLiabilities', 'currentLiabilities', 'longTermDebt', 'totalEquity', 'retainedEarnings',
      'operatingCashFlow', 'investingCashFlow', 'financingCashFlow', 'netCashFlow',
      'cashAndEquivalents', 'workingCapital', 'totalDebt', 'bookValue'
    ];

    financialFields.forEach(field => {
      const value = data[field as keyof FinancialReportFormData];
      if (value !== '' && value !== undefined) {
        const numValue = Number(value);
        if (isNaN(numValue) || numValue < 0) {
          newErrors[field] = `${FINANCIAL_REPORT_FIELD_LABELS[field] || field} must be a positive number`;
        }
      }
    });

    setReportErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleAddFinancialReport = () => {
    initializeNewReport();
    setEditingReportId('new');
  };


  const handleSaveFinancialReport = async () => {
    if (!validateFinancialReport(newReportData)) {
      return;
    }

    try {
      if (editingReportId === 'new') {
        // Create new report
        if (initialData?.id && newReportData.reportStartDate && newReportData.reportEndDate) {
          const createRequest = {
            tradingPartnerId: initialData.id,
            reportStartDate: new Date(newReportData.reportStartDate).toISOString(),
            reportEndDate: new Date(newReportData.reportEndDate).toISOString(),
            ...Object.fromEntries(
              Object.entries(newReportData)
                .filter(([key, value]) => key !== 'reportStartDate' && key !== 'reportEndDate' && value !== '')
                .map(([key, value]) => [key, typeof value === 'string' && !isNaN(Number(value)) ? Number(value) : value])
            )
          };
          
          await financialReportService.createFinancialReport(createRequest);
          await loadFinancialReports(initialData.id);
        }
      } else {
        // Update existing report
        if (editingReportId && newReportData.reportStartDate && newReportData.reportEndDate) {
          const updateRequest = {
            reportStartDate: new Date(newReportData.reportStartDate).toISOString(),
            reportEndDate: new Date(newReportData.reportEndDate).toISOString(),
            ...Object.fromEntries(
              Object.entries(newReportData)
                .filter(([key, value]) => key !== 'reportStartDate' && key !== 'reportEndDate' && value !== '')
                .map(([key, value]) => [key, typeof value === 'string' && !isNaN(Number(value)) ? Number(value) : value])
            )
          };
          
          await financialReportService.updateFinancialReport(editingReportId, updateRequest);
          if (initialData?.id) {
            await loadFinancialReports(initialData.id);
          }
        }
      }

      setEditingReportId(null);
      setNewReportData({});
      setReportErrors({});
    } catch (error) {
      console.error('Failed to save financial report:', error);
    }
  };

  const handleCancelEditFinancialReport = () => {
    setEditingReportId(null);
    setNewReportData({});
    setReportErrors({});
  };


  const handleReportFieldChange = (field: string, value: any) => {
    setNewReportData(prev => ({
      ...prev,
      [field]: value
    }));
    
    // Clear error when user starts typing
    if (reportErrors[field]) {
      setReportErrors(prev => ({
        ...prev,
        [field]: ''
      }));
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        <Box display="flex" alignItems="center">
          <BusinessIcon sx={{ mr: 1 }} />
          {initialData ? 'Edit Trading Partner' : 'Create New Trading Partner'}
        </Box>
      </DialogTitle>
      
      <DialogContent>
        <Grid container spacing={3} sx={{ mt: 1 }}>
          {/* Basic Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom>
              Basic Information
            </Typography>
            <Divider sx={{ mb: 2 }} />
          </Grid>

          <Grid item xs={12} sm={8}>
            <TextField
              fullWidth
              label="Company Name"
              value={formData.companyName}
              onChange={(e) => handleInputChange('companyName', e.target.value)}
              error={!!errors.companyName}
              helperText={errors.companyName}
              required
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <BusinessIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>

          <Grid item xs={12} sm={4}>
            <FormControl fullWidth error={!!errors.partnerType}>
              <InputLabel required>Partner Type</InputLabel>
              <Select
                value={formData.partnerType}
                label="Partner Type"
                onChange={(e) => handleInputChange('partnerType', e.target.value)}
              >
                <MenuItem value="Supplier">Supplier</MenuItem>
                <MenuItem value="Customer">Customer</MenuItem>
                <MenuItem value="Both">Both</MenuItem>
              </Select>
            </FormControl>
          </Grid>

          {/* Contact Information */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Contact Information
            </Typography>
            <Divider sx={{ mb: 2 }} />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Contact Person"
              value={formData.contactPerson}
              onChange={(e) => handleInputChange('contactPerson', e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <PersonIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Contact Email"
              type="email"
              value={formData.contactEmail}
              onChange={(e) => handleInputChange('contactEmail', e.target.value)}
              error={!!errors.contactEmail}
              helperText={errors.contactEmail}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <EmailIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Contact Phone"
              value={formData.contactPhone}
              onChange={(e) => handleInputChange('contactPhone', e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <PhoneIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Tax Number"
              value={formData.taxNumber}
              onChange={(e) => handleInputChange('taxNumber', e.target.value)}
            />
          </Grid>

          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Address"
              multiline
              rows={2}
              value={formData.address}
              onChange={(e) => handleInputChange('address', e.target.value)}
            />
          </Grid>

          {/* Credit Management */}
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Credit Management
            </Typography>
            <Divider sx={{ mb: 2 }} />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Credit Limit"
              type="number"
              value={formData.creditLimit}
              onChange={(e) => handleInputChange('creditLimit', parseFloat(e.target.value) || 0)}
              error={!!errors.creditLimit}
              helperText={errors.creditLimit || `Format: ${formatCurrency(formData.creditLimit)}`}
              required
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <CreditIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Credit Limit Valid Until"
              type="date"
              value={formData.creditLimitValidUntil}
              onChange={(e) => handleInputChange('creditLimitValidUntil', e.target.value)}
              error={!!errors.creditLimitValidUntil}
              helperText={errors.creditLimitValidUntil}
              required
              InputLabelProps={{ shrink: true }}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <CalendarIcon />
                  </InputAdornment>
                ),
              }}
            />
          </Grid>

          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Payment Terms (Days)"
              type="number"
              value={formData.paymentTermDays}
              onChange={(e) => handleInputChange('paymentTermDays', parseInt(e.target.value) || 0)}
              error={!!errors.paymentTermDays}
              helperText={errors.paymentTermDays}
            />
          </Grid>

          {/* Status Management (only for edit mode) */}
          {initialData && (
            <>
              <Grid item xs={12}>
                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                  Status Management
                </Typography>
                <Divider sx={{ mb: 2 }} />
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.isActive}
                      onChange={(e) => handleInputChange('isActive', e.target.checked)}
                    />
                  }
                  label="Active"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.isBlocked}
                      onChange={(e) => handleInputChange('isBlocked', e.target.checked)}
                    />
                  }
                  label="Blocked"
                />
              </Grid>

              {formData.isBlocked && (
                <Grid item xs={12}>
                  <TextField
                    fullWidth
                    label="Block Reason"
                    multiline
                    rows={2}
                    value={formData.blockReason}
                    onChange={(e) => handleInputChange('blockReason', e.target.value)}
                  />
                </Grid>
              )}
            </>
          )}

          {/* Financial Reports Section */}
          <Grid item xs={12}>
            <Box display="flex" alignItems="center" sx={{ mt: 2, mb: 1 }}>
              <AssessmentIcon sx={{ mr: 1 }} />
              <Typography variant="h6">
                Financial Reports History
              </Typography>
              <IconButton 
                size="small" 
                sx={{ ml: 1 }}
                onClick={() => setShowFinancialReports(!showFinancialReports)}
              >
                {showFinancialReports ? <ExpandLessIcon /> : <ExpandMoreIcon />}
              </IconButton>
              {initialData && (
                <Tooltip title="Add Financial Report">
                  <IconButton size="small" sx={{ ml: 'auto' }} onClick={handleAddFinancialReport}>
                    <AddIcon />
                  </IconButton>
                </Tooltip>
              )}
            </Box>
            <Divider sx={{ mb: 2 }} />
          </Grid>

          <Collapse in={showFinancialReports}>
            <Grid item xs={12}>
              {!initialData ? (
                <Alert severity="info" sx={{ mb: 2 }}>
                  Save the trading partner first to add financial reports.
                </Alert>
              ) : (
                <Box>
                  {/* Financial Report Form (when adding/editing) */}
                  {editingReportId && (
                    <Paper sx={{ p: 2, mb: 2 }} variant="outlined">
                      <Typography variant="subtitle1" gutterBottom>
                        {editingReportId === 'new' ? 'Add New Financial Report' : 'Edit Financial Report'}
                      </Typography>
                      
                      <Grid container spacing={2}>
                        {/* Report Period */}
                        <Grid item xs={12} sm={6}>
                          <TextField
                            fullWidth
                            label="Report Start Date"
                            type="date"
                            value={newReportData.reportStartDate || ''}
                            onChange={(e) => handleReportFieldChange('reportStartDate', e.target.value)}
                            error={!!reportErrors.reportStartDate}
                            helperText={reportErrors.reportStartDate}
                            required
                            InputLabelProps={{ shrink: true }}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">
                                  <CalendarIcon />
                                </InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                          <TextField
                            fullWidth
                            label="Report End Date"
                            type="date"
                            value={newReportData.reportEndDate || ''}
                            onChange={(e) => handleReportFieldChange('reportEndDate', e.target.value)}
                            error={!!reportErrors.reportEndDate}
                            helperText={reportErrors.reportEndDate}
                            required
                            InputLabelProps={{ shrink: true }}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">
                                  <CalendarIcon />
                                </InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        {/* Key Financial Metrics */}
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" gutterBottom>
                            Income Statement
                          </Typography>
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Revenue"
                            type="number"
                            value={newReportData.revenue || ''}
                            onChange={(e) => handleReportFieldChange('revenue', e.target.value)}
                            error={!!reportErrors.revenue}
                            helperText={reportErrors.revenue}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Cost of Goods Sold"
                            type="number"
                            value={newReportData.costOfGoodsSold || ''}
                            onChange={(e) => handleReportFieldChange('costOfGoodsSold', e.target.value)}
                            error={!!reportErrors.costOfGoodsSold}
                            helperText={reportErrors.costOfGoodsSold}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Operating Expenses"
                            type="number"
                            value={newReportData.operatingExpenses || ''}
                            onChange={(e) => handleReportFieldChange('operatingExpenses', e.target.value)}
                            error={!!reportErrors.operatingExpenses}
                            helperText={reportErrors.operatingExpenses}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Net Income"
                            type="number"
                            value={newReportData.netIncome || ''}
                            onChange={(e) => handleReportFieldChange('netIncome', e.target.value)}
                            error={!!reportErrors.netIncome}
                            helperText={reportErrors.netIncome}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        {/* Balance Sheet */}
                        <Grid item xs={12}>
                          <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                            Balance Sheet
                          </Typography>
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Total Assets"
                            type="number"
                            value={newReportData.totalAssets || ''}
                            onChange={(e) => handleReportFieldChange('totalAssets', e.target.value)}
                            error={!!reportErrors.totalAssets}
                            helperText={reportErrors.totalAssets}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Total Liabilities"
                            type="number"
                            value={newReportData.totalLiabilities || ''}
                            onChange={(e) => handleReportFieldChange('totalLiabilities', e.target.value)}
                            error={!!reportErrors.totalLiabilities}
                            helperText={reportErrors.totalLiabilities}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Total Equity"
                            type="number"
                            value={newReportData.totalEquity || ''}
                            onChange={(e) => handleReportFieldChange('totalEquity', e.target.value)}
                            error={!!reportErrors.totalEquity}
                            helperText={reportErrors.totalEquity}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6} md={3}>
                          <TextField
                            fullWidth
                            label="Cash and Equivalents"
                            type="number"
                            value={newReportData.cashAndEquivalents || ''}
                            onChange={(e) => handleReportFieldChange('cashAndEquivalents', e.target.value)}
                            error={!!reportErrors.cashAndEquivalents}
                            helperText={reportErrors.cashAndEquivalents}
                            InputProps={{
                              startAdornment: (
                                <InputAdornment position="start">$</InputAdornment>
                              ),
                            }}
                          />
                        </Grid>

                        {/* Additional fields */}
                        <Grid item xs={12} sm={6}>
                          <TextField
                            fullWidth
                            label="Notes"
                            multiline
                            rows={2}
                            value={newReportData.notes || ''}
                            onChange={(e) => handleReportFieldChange('notes', e.target.value)}
                          />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                          <Stack spacing={2}>
                            <FormControlLabel
                              control={
                                <Switch
                                  checked={newReportData.isAudited || false}
                                  onChange={(e) => handleReportFieldChange('isAudited', e.target.checked)}
                                />
                              }
                              label="Audited Report"
                            />
                            
                            {newReportData.isAudited && (
                              <TextField
                                fullWidth
                                label="Audit Firm"
                                value={newReportData.auditFirm || ''}
                                onChange={(e) => handleReportFieldChange('auditFirm', e.target.value)}
                                size="small"
                              />
                            )}
                          </Stack>
                        </Grid>

                        {/* Action buttons */}
                        <Grid item xs={12}>
                          <Stack direction="row" spacing={1} justifyContent="flex-end" sx={{ mt: 2 }}>
                            <Button 
                              onClick={handleCancelEditFinancialReport}
                              startIcon={<CancelIcon />}
                            >
                              Cancel
                            </Button>
                            <Button 
                              onClick={handleSaveFinancialReport}
                              variant="contained"
                              startIcon={<SaveIcon />}
                            >
                              Save Report
                            </Button>
                          </Stack>
                        </Grid>
                      </Grid>
                    </Paper>
                  )}

                  {/* Financial Reports Table */}
                  <Box>
                    <Typography variant="h6" gutterBottom>
                      Financial Reports
                    </Typography>
                    {financialReports.length > 0 ? (
                      <Alert severity="info">
                        Financial reports functionality is available but simplified for this demo.
                      </Alert>
                    ) : null}
                  </Box>

                  {financialReports.length === 0 && (
                    <Alert severity="info" sx={{ mt: 2 }}>
                      No financial reports added yet. Financial data helps improve credit analysis and risk assessment.
                    </Alert>
                  )}
                </Box>
              )}
            </Grid>
          </Collapse>
        </Grid>
      </DialogContent>

      <DialogActions sx={{ p: 3 }}>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSubmit} variant="contained">
          {initialData ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};