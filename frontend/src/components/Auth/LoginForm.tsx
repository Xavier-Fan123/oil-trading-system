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
  Divider,
  Grid,
  Paper,
  Avatar,
  CircularProgress,
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  Person,
  Lock,
  Business,
  Security,
  TrendingUp,
  BarChart,
  Assessment,
} from '@mui/icons-material';

interface LoginFormProps {
  onLogin: (credentials: LoginCredentials) => Promise<void>;
  onNavigateToRegister: () => void;
  onNavigateToForgotPassword: () => void;
  loading?: boolean;
  error?: string;
}

interface LoginCredentials {
  email: string;
  password: string;
  rememberMe: boolean;
}

export const LoginForm: React.FC<LoginFormProps> = ({
  onLogin,
  onNavigateToRegister,
  onNavigateToForgotPassword,
  loading = false,
  error,
}) => {
  const [credentials, setCredentials] = useState<LoginCredentials>({
    email: '',
    password: '',
    rememberMe: false,
  });
  const [showPassword, setShowPassword] = useState(false);
  const [validationErrors, setValidationErrors] = useState<{
    email?: string;
    password?: string;
  }>({});

  const handleInputChange = (field: keyof LoginCredentials, value: string | boolean) => {
    setCredentials(prev => ({ ...prev, [field]: value }));
    
    // Clear validation error when user starts typing
    if (validationErrors[field as keyof typeof validationErrors]) {
      setValidationErrors(prev => ({ ...prev, [field]: undefined }));
    }
  };

  const validateForm = (): boolean => {
    const errors: typeof validationErrors = {};

    if (!credentials.email.trim()) {
      errors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(credentials.email)) {
      errors.email = 'Please enter a valid email address';
    }

    if (!credentials.password.trim()) {
      errors.password = 'Password is required';
    } else if (credentials.password.length < 6) {
      errors.password = 'Password must be at least 6 characters';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    
    if (!validateForm()) return;

    try {
      await onLogin(credentials);
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  const demoCredentials = [
    { role: 'Admin', email: 'admin@oiltrading.com', password: 'admin123' },
    { role: 'Trader', email: 'trader@oiltrading.com', password: 'trader123' },
    { role: 'Manager', email: 'manager@oiltrading.com', password: 'manager123' },
  ];

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
      <Grid container maxWidth="lg" sx={{ height: '80vh' }}>
        {/* Left Side - System Info */}
        <Grid item xs={12} md={6} sx={{ display: { xs: 'none', md: 'flex' } }}>
          <Paper
            sx={{
              width: '100%',
              borderRadius: '16px 0 0 16px',
              background: 'linear-gradient(45deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%)',
              color: 'white',
              p: 4,
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center',
              position: 'relative',
              overflow: 'hidden',
            }}
          >
            {/* Background Pattern */}
            <Box
              sx={{
                position: 'absolute',
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                opacity: 0.1,
                backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.1'%3E%3Cpath d='M30 30c0-16.569 13.431-30 30-30s30 13.431 30 30-13.431 30-30 30-30-13.431-30-30z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
              }}
            />
            
            <Box sx={{ position: 'relative', zIndex: 1 }}>
              <Box display="flex" alignItems="center" mb={4}>
                <Avatar sx={{ bgcolor: 'primary.main', mr: 2, width: 56, height: 56 }}>
                  <TrendingUp sx={{ fontSize: 32 }} />
                </Avatar>
                <Box>
                  <Typography variant="h4" component="h1" fontWeight="bold">
                    Oil Trading
                  </Typography>
                  <Typography variant="h6" color="rgba(255,255,255,0.7)">
                    Enterprise Platform
                  </Typography>
                </Box>
              </Box>

              <Typography variant="h5" gutterBottom fontWeight="bold">
                Welcome to the Future of Oil Trading
              </Typography>
              
              <Typography variant="body1" paragraph sx={{ opacity: 0.9 }}>
                Streamline your oil trading operations with our comprehensive platform featuring 
                real-time pricing, advanced risk management, and intelligent workflow automation.
              </Typography>

              <Grid container spacing={3} sx={{ mt: 3 }}>
                <Grid item xs={12}>
                  <Box display="flex" alignItems="center" mb={2}>
                    <Assessment sx={{ mr: 2, color: 'primary.main' }} />
                    <Typography variant="body2">
                      Advanced Risk Analytics & VaR Calculations
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12}>
                  <Box display="flex" alignItems="center" mb={2}>
                    <BarChart sx={{ mr: 2, color: 'primary.main' }} />
                    <Typography variant="body2">
                      Real-time Market Data & Technical Indicators
                    </Typography>
                  </Box>
                </Grid>
                <Grid item xs={12}>
                  <Box display="flex" alignItems="center" mb={2}>
                    <Business sx={{ mr: 2, color: 'primary.main' }} />
                    <Typography variant="body2">
                      Comprehensive Contract & Inventory Management
                    </Typography>
                  </Box>
                </Grid>
              </Grid>
            </Box>
          </Paper>
        </Grid>

        {/* Right Side - Login Form */}
        <Grid item xs={12} md={6}>
          <Paper
            sx={{
              width: '100%',
              height: '100%',
              borderRadius: { xs: '16px', md: '0 16px 16px 0' },
              p: 4,
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center',
            }}
          >
            <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 400, mx: 'auto', width: '100%' }}>
              <Typography variant="h4" component="h1" gutterBottom textAlign="center" fontWeight="bold">
                Sign In
              </Typography>
              
              <Typography variant="body2" color="text.secondary" textAlign="center" mb={4}>
                Access your trading dashboard
              </Typography>

              {error && (
                <Alert severity="error" sx={{ mb: 3 }}>
                  {error}
                </Alert>
              )}

              <TextField
                fullWidth
                label="Email Address"
                type="email"
                value={credentials.email}
                onChange={(e) => handleInputChange('email', e.target.value)}
                error={!!validationErrors.email}
                helperText={validationErrors.email}
                disabled={loading}
                sx={{ mb: 2 }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <Person color="action" />
                    </InputAdornment>
                  ),
                }}
              />

              <TextField
                fullWidth
                label="Password"
                type={showPassword ? 'text' : 'password'}
                value={credentials.password}
                onChange={(e) => handleInputChange('password', e.target.value)}
                error={!!validationErrors.password}
                helperText={validationErrors.password}
                disabled={loading}
                sx={{ mb: 2 }}
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

              <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={credentials.rememberMe}
                      onChange={(e) => handleInputChange('rememberMe', e.target.checked)}
                      disabled={loading}
                    />
                  }
                  label="Remember me"
                />
                
                <Link
                  component="button"
                  type="button"
                  variant="body2"
                  onClick={onNavigateToForgotPassword}
                  sx={{ textDecoration: 'none' }}
                >
                  Forgot password?
                </Link>
              </Box>

              <Button
                type="submit"
                fullWidth
                variant="contained"
                size="large"
                disabled={loading}
                sx={{ mb: 2, py: 1.5 }}
              >
                {loading ? (
                  <>
                    <CircularProgress size={20} sx={{ mr: 1 }} />
                    Signing In...
                  </>
                ) : (
                  'Sign In'
                )}
              </Button>

              <Divider sx={{ my: 3 }}>
                <Typography variant="body2" color="text.secondary">
                  Demo Accounts
                </Typography>
              </Divider>

              {/* Demo Credentials */}
              <Grid container spacing={1}>
                {demoCredentials.map((demo, index) => (
                  <Grid item xs={12} key={index}>
                    <Button
                      fullWidth
                      variant="outlined"
                      size="small"
                      onClick={() => {
                        setCredentials({
                          email: demo.email,
                          password: demo.password,
                          rememberMe: false,
                        });
                      }}
                      disabled={loading}
                      sx={{ textTransform: 'none', justifyContent: 'flex-start' }}
                    >
                      <Security sx={{ mr: 1, fontSize: 16 }} />
                      {demo.role}: {demo.email}
                    </Button>
                  </Grid>
                ))}
              </Grid>

              <Box textAlign="center" mt={3}>
                <Typography variant="body2" color="text.secondary">
                  Don't have an account?{' '}
                  <Link
                    component="button"
                    type="button"
                    variant="body2"
                    onClick={onNavigateToRegister}
                    sx={{ textDecoration: 'none', fontWeight: 'bold' }}
                  >
                    Sign up here
                  </Link>
                </Typography>
              </Box>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};