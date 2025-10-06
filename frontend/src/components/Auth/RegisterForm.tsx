import React, { useState } from 'react';
import {
  Box,
  TextField,
  Button,
  Typography,
  Link,
  Alert,
  InputAdornment,
  IconButton,
  FormControlLabel,
  Checkbox,
  Stepper,
  Step,
  StepLabel,
  Grid,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Paper,
  Avatar,
  CircularProgress,
  LinearProgress,
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  Person,
  Email,
  Lock,
  Business,
  Phone,
  LocationOn,
} from '@mui/icons-material';

interface RegisterFormProps {
  onRegister: (userData: RegisterData) => Promise<void>;
  onNavigateToLogin: () => void;
  loading?: boolean;
  error?: string;
}

interface RegisterData {
  // Personal Information
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  
  // Account Information
  password: string;
  confirmPassword: string;
  
  // Company Information
  companyName: string;
  companyType: string;
  department: string;
  position: string;
  
  // Address
  country: string;
  city: string;
  address: string;
  
  // Preferences
  role: string;
  agreeToTerms: boolean;
  subscribeToUpdates: boolean;
}

const steps = ['Personal Info', 'Account Setup', 'Company Details', 'Review'];

const companyTypes = [
  'Oil Refinery',
  'Trading Company',
  'Shipping Company',
  'Investment Firm',
  'Commodity Broker',
  'Energy Consultant',
  'Other',
];

const roles = [
  'Trader',
  'Risk Manager',
  'Operations Manager',
  'Financial Analyst',
  'Compliance Officer',
  'Senior Manager',
  'Executive',
];

const countries = [
  'United States',
  'United Kingdom',
  'Singapore',
  'United Arab Emirates',
  'Netherlands',
  'Norway',
  'Saudi Arabia',
  'Canada',
  'Other',
];

export const RegisterForm: React.FC<RegisterFormProps> = ({
  onRegister,
  onNavigateToLogin,
  loading = false,
  error,
}) => {
  const [activeStep, setActiveStep] = useState(0);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [userData, setUserData] = useState<RegisterData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
    companyName: '',
    companyType: '',
    department: '',
    position: '',
    country: '',
    city: '',
    address: '',
    role: '',
    agreeToTerms: false,
    subscribeToUpdates: true,
  });
  
  const [validationErrors, setValidationErrors] = useState<{
    [key: string]: string;
  }>({});

  const handleInputChange = (field: keyof RegisterData, value: string | boolean) => {
    setUserData(prev => ({ ...prev, [field]: value }));
    
    // Clear validation error when user starts typing
    if (validationErrors[field]) {
      setValidationErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const validateStep = (step: number): boolean => {
    const errors: { [key: string]: string } = {};

    switch (step) {
      case 0: // Personal Info
        if (!userData.firstName.trim()) errors.firstName = 'First name is required';
        if (!userData.lastName.trim()) errors.lastName = 'Last name is required';
        if (!userData.email.trim()) {
          errors.email = 'Email is required';
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(userData.email)) {
          errors.email = 'Please enter a valid email address';
        }
        if (!userData.phone.trim()) errors.phone = 'Phone number is required';
        break;

      case 1: // Account Setup
        if (!userData.password.trim()) {
          errors.password = 'Password is required';
        } else if (userData.password.length < 8) {
          errors.password = 'Password must be at least 8 characters';
        } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(userData.password)) {
          errors.password = 'Password must contain uppercase, lowercase, and number';
        }
        
        if (!userData.confirmPassword.trim()) {
          errors.confirmPassword = 'Please confirm your password';
        } else if (userData.password !== userData.confirmPassword) {
          errors.confirmPassword = 'Passwords do not match';
        }
        
        if (!userData.role) errors.role = 'Please select your role';
        break;

      case 2: // Company Details
        if (!userData.companyName.trim()) errors.companyName = 'Company name is required';
        if (!userData.companyType) errors.companyType = 'Company type is required';
        if (!userData.position.trim()) errors.position = 'Position is required';
        if (!userData.country) errors.country = 'Country is required';
        if (!userData.city.trim()) errors.city = 'City is required';
        break;

      case 3: // Review
        if (!userData.agreeToTerms) errors.agreeToTerms = 'You must agree to the terms and conditions';
        break;
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleNext = () => {
    if (validateStep(activeStep)) {
      setActiveStep(prev => prev + 1);
    }
  };

  const handleBack = () => {
    setActiveStep(prev => prev - 1);
  };

  const handleSubmit = async () => {
    if (validateStep(activeStep)) {
      try {
        await onRegister(userData);
      } catch (error) {
        console.error('Registration failed:', error);
      }
    }
  };

  const getPasswordStrength = (password: string): number => {
    let score = 0;
    if (password.length >= 8) score += 25;
    if (/[a-z]/.test(password)) score += 25;
    if (/[A-Z]/.test(password)) score += 25;
    if (/\d/.test(password)) score += 25;
    return score;
  };

  const getPasswordStrengthColor = (strength: number): 'error' | 'warning' | 'info' | 'success' => {
    if (strength < 50) return 'error';
    if (strength < 75) return 'warning';
    if (strength < 100) return 'info';
    return 'success';
  };

  const renderStepContent = (step: number) => {
    switch (step) {
      case 0:
        return (
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="First Name"
                value={userData.firstName}
                onChange={(e) => handleInputChange('firstName', e.target.value)}
                error={!!validationErrors.firstName}
                helperText={validationErrors.firstName}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Last Name"
                value={userData.lastName}
                onChange={(e) => handleInputChange('lastName', e.target.value)}
                error={!!validationErrors.lastName}
                helperText={validationErrors.lastName}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Email Address"
                type="email"
                value={userData.email}
                onChange={(e) => handleInputChange('email', e.target.value)}
                error={!!validationErrors.email}
                helperText={validationErrors.email}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Email color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Phone Number"
                value={userData.phone}
                onChange={(e) => handleInputChange('phone', e.target.value)}
                error={!!validationErrors.phone}
                helperText={validationErrors.phone}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Phone color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
          </Grid>
        );

      case 1:
        const passwordStrength = getPasswordStrength(userData.password);
        return (
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Password"
                type={showPassword ? 'text' : 'password'}
                value={userData.password}
                onChange={(e) => handleInputChange('password', e.target.value)}
                error={!!validationErrors.password}
                helperText={validationErrors.password}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Lock color="action" />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setShowPassword(!showPassword)}
                        edge="end"
                      >
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />
              {userData.password && (
                <Box sx={{ mt: 1 }}>
                  <Typography variant="caption" color="text.secondary">
                    Password strength:
                  </Typography>
                  <LinearProgress
                    variant="determinate"
                    value={passwordStrength}
                    color={getPasswordStrengthColor(passwordStrength)}
                    sx={{ mt: 0.5 }}
                  />
                </Box>
              )}
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Confirm Password"
                type={showConfirmPassword ? 'text' : 'password'}
                value={userData.confirmPassword}
                onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
                error={!!validationErrors.confirmPassword}
                helperText={validationErrors.confirmPassword}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Lock color="action" />
                    </InputAdornment>
                  ),
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                        edge="end"
                      >
                        {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12}>
              <FormControl fullWidth error={!!validationErrors.role}>
                <InputLabel>Your Role</InputLabel>
                <Select
                  value={userData.role}
                  label="Your Role"
                  onChange={(e) => handleInputChange('role', e.target.value)}
                  disabled={loading}
                >
                  {roles.map((role) => (
                    <MenuItem key={role} value={role}>
                      {role}
                    </MenuItem>
                  ))}
                </Select>
                {validationErrors.role && (
                  <Typography variant="caption" color="error">
                    {validationErrors.role}
                  </Typography>
                )}
              </FormControl>
            </Grid>
          </Grid>
        );

      case 2:
        return (
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Company Name"
                value={userData.companyName}
                onChange={(e) => handleInputChange('companyName', e.target.value)}
                error={!!validationErrors.companyName}
                helperText={validationErrors.companyName}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Business color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth error={!!validationErrors.companyType}>
                <InputLabel>Company Type</InputLabel>
                <Select
                  value={userData.companyType}
                  label="Company Type"
                  onChange={(e) => handleInputChange('companyType', e.target.value)}
                  disabled={loading}
                >
                  {companyTypes.map((type) => (
                    <MenuItem key={type} value={type}>
                      {type}
                    </MenuItem>
                  ))}
                </Select>
                {validationErrors.companyType && (
                  <Typography variant="caption" color="error">
                    {validationErrors.companyType}
                  </Typography>
                )}
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="Department"
                value={userData.department}
                onChange={(e) => handleInputChange('department', e.target.value)}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Position"
                value={userData.position}
                onChange={(e) => handleInputChange('position', e.target.value)}
                error={!!validationErrors.position}
                helperText={validationErrors.position}
                disabled={loading}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <FormControl fullWidth error={!!validationErrors.country}>
                <InputLabel>Country</InputLabel>
                <Select
                  value={userData.country}
                  label="Country"
                  onChange={(e) => handleInputChange('country', e.target.value)}
                  disabled={loading}
                >
                  {countries.map((country) => (
                    <MenuItem key={country} value={country}>
                      {country}
                    </MenuItem>
                  ))}
                </Select>
                {validationErrors.country && (
                  <Typography variant="caption" color="error">
                    {validationErrors.country}
                  </Typography>
                )}
              </FormControl>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                fullWidth
                label="City"
                value={userData.city}
                onChange={(e) => handleInputChange('city', e.target.value)}
                error={!!validationErrors.city}
                helperText={validationErrors.city}
                disabled={loading}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <LocationOn color="action" />
                    </InputAdornment>
                  ),
                }}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Address"
                multiline
                rows={2}
                value={userData.address}
                onChange={(e) => handleInputChange('address', e.target.value)}
                disabled={loading}
              />
            </Grid>
          </Grid>
        );

      case 3:
        return (
          <Box>
            <Typography variant="h6" gutterBottom>
              Review Your Information
            </Typography>
            
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <Paper variant="outlined" sx={{ p: 2 }}>
                  <Typography variant="subtitle2" color="primary" gutterBottom>
                    Personal Information
                  </Typography>
                  <Typography variant="body2">
                    <strong>Name:</strong> {userData.firstName} {userData.lastName}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Email:</strong> {userData.email}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Phone:</strong> {userData.phone}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Role:</strong> {userData.role}
                  </Typography>
                </Paper>
              </Grid>
              
              <Grid item xs={12} sm={6}>
                <Paper variant="outlined" sx={{ p: 2 }}>
                  <Typography variant="subtitle2" color="primary" gutterBottom>
                    Company Information
                  </Typography>
                  <Typography variant="body2">
                    <strong>Company:</strong> {userData.companyName}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Type:</strong> {userData.companyType}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Position:</strong> {userData.position}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Location:</strong> {userData.city}, {userData.country}
                  </Typography>
                </Paper>
              </Grid>
            </Grid>

            <Box sx={{ mt: 3 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={userData.agreeToTerms}
                    onChange={(e) => handleInputChange('agreeToTerms', e.target.checked)}
                    disabled={loading}
                    color="primary"
                  />
                }
                label={
                  <Typography variant="body2">
                    I agree to the{' '}
                    <Link href="#" color="primary">
                      Terms and Conditions
                    </Link>{' '}
                    and{' '}
                    <Link href="#" color="primary">
                      Privacy Policy
                    </Link>
                  </Typography>
                }
              />
              {validationErrors.agreeToTerms && (
                <Typography variant="caption" color="error" display="block">
                  {validationErrors.agreeToTerms}
                </Typography>
              )}
            </Box>

            <FormControlLabel
              control={
                <Checkbox
                  checked={userData.subscribeToUpdates}
                  onChange={(e) => handleInputChange('subscribeToUpdates', e.target.checked)}
                  disabled={loading}
                  color="primary"
                />
              }
              label={
                <Typography variant="body2">
                  Subscribe to product updates and market insights
                </Typography>
              }
            />
          </Box>
        );

      default:
        return null;
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        p: 2,
      }}
    >
      <Paper
        sx={{
          width: '100%',
          maxWidth: 800,
          borderRadius: 2,
          overflow: 'hidden',
        }}
      >
        <Box sx={{ p: 4 }}>
          <Box textAlign="center" mb={4}>
            <Avatar sx={{ bgcolor: 'primary.main', mx: 'auto', mb: 2, width: 56, height: 56 }}>
              <Business sx={{ fontSize: 32 }} />
            </Avatar>
            <Typography variant="h4" component="h1" fontWeight="bold">
              Create Account
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Join the Oil Trading Platform
            </Typography>
          </Box>

          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          {/* Stepper */}
          <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
            {steps.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>

          {/* Step Content */}
          <Box sx={{ mb: 4 }}>
            {renderStepContent(activeStep)}
          </Box>

          {/* Navigation Buttons */}
          <Box display="flex" justifyContent="space-between">
            <Button
              disabled={activeStep === 0 || loading}
              onClick={handleBack}
            >
              Back
            </Button>
            
            <Box>
              {activeStep === steps.length - 1 ? (
                <Button
                  variant="contained"
                  onClick={handleSubmit}
                  disabled={loading || !userData.agreeToTerms}
                  size="large"
                >
                  {loading ? (
                    <>
                      <CircularProgress size={20} sx={{ mr: 1 }} />
                      Creating Account...
                    </>
                  ) : (
                    'Create Account'
                  )}
                </Button>
              ) : (
                <Button
                  variant="contained"
                  onClick={handleNext}
                  disabled={loading}
                  size="large"
                >
                  Next
                </Button>
              )}
            </Box>
          </Box>

          <Box textAlign="center" mt={3}>
            <Typography variant="body2" color="text.secondary">
              Already have an account?{' '}
              <Link
                component="button"
                type="button"
                variant="body2"
                onClick={onNavigateToLogin}
                sx={{ textDecoration: 'none', fontWeight: 'bold' }}
              >
                Sign in here
              </Link>
            </Typography>
          </Box>
        </Box>
      </Paper>
    </Box>
  );
};