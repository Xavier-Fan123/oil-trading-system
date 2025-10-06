import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  Security,
  Lock,
  Warning,
} from '@mui/icons-material';
import { useAuth } from '../../contexts/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermissions?: string[];
  requiredRoles?: string[];
  fallbackComponent?: React.ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredPermissions = [],
  requiredRoles = [],
  fallbackComponent,
}) => {
  const { isAuthenticated, isLoading, user, hasPermission, isInRole } = useAuth();

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
        flexDirection="column"
        gap={2}
      >
        <CircularProgress size={60} />
        <Typography variant="h6" color="text.secondary">
          Authenticating...
        </Typography>
      </Box>
    );
  }

  // If not authenticated, show login prompt
  if (!isAuthenticated) {
    return fallbackComponent || (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
        sx={{
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          p: 2,
        }}
      >
        <Paper
          sx={{
            p: 4,
            maxWidth: 400,
            textAlign: 'center',
            borderRadius: 2,
          }}
        >
          <Security sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
          <Typography variant="h5" gutterBottom fontWeight="bold">
            Authentication Required
          </Typography>
          <Typography variant="body1" color="text.secondary" paragraph>
            You need to be logged in to access this page.
          </Typography>
          <Button
            variant="contained"
            size="large"
            onClick={() => window.location.href = '/login'}
            sx={{ mt: 2 }}
          >
            Go to Login
          </Button>
        </Paper>
      </Box>
    );
  }

  // Check role requirements
  if (requiredRoles.length > 0 && !isInRole(requiredRoles)) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="100vh"
        sx={{ p: 2 }}
      >
        <Paper
          sx={{
            p: 4,
            maxWidth: 500,
            textAlign: 'center',
            borderRadius: 2,
          }}
        >
          <Warning sx={{ fontSize: 64, color: 'warning.main', mb: 2 }} />
          <Typography variant="h5" gutterBottom fontWeight="bold">
            Insufficient Role Privileges
          </Typography>
          <Typography variant="body1" color="text.secondary" paragraph>
            Your role ({user?.role}) does not have access to this feature.
          </Typography>
          <Alert severity="info" sx={{ mt: 2, textAlign: 'left' }}>
            <Typography variant="subtitle2" gutterBottom>
              Required Roles:
            </Typography>
            <ul style={{ margin: 0, paddingLeft: 20 }}>
              {requiredRoles.map(role => (
                <li key={role}>{role}</li>
              ))}
            </ul>
          </Alert>
          <Button
            variant="outlined"
            onClick={() => window.history.back()}
            sx={{ mt: 2 }}
          >
            Go Back
          </Button>
        </Paper>
      </Box>
    );
  }

  // Check permission requirements
  if (requiredPermissions.length > 0) {
    const missingPermissions = requiredPermissions.filter(
      permission => !hasPermission(permission)
    );

    if (missingPermissions.length > 0) {
      return (
        <Box
          display="flex"
          justifyContent="center"
          alignItems="center"
          minHeight="100vh"
          sx={{ p: 2 }}
        >
          <Paper
            sx={{
              p: 4,
              maxWidth: 500,
              textAlign: 'center',
              borderRadius: 2,
            }}
          >
            <Lock sx={{ fontSize: 64, color: 'error.main', mb: 2 }} />
            <Typography variant="h5" gutterBottom fontWeight="bold">
              Insufficient Permissions
            </Typography>
            <Typography variant="body1" color="text.secondary" paragraph>
              You don't have the required permissions to access this feature.
            </Typography>
            <Alert severity="error" sx={{ mt: 2, textAlign: 'left' }}>
              <Typography variant="subtitle2" gutterBottom>
                Missing Permissions:
              </Typography>
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                {missingPermissions.map(permission => (
                  <li key={permission}>{permission}</li>
                ))}
              </ul>
            </Alert>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              Contact your administrator to request access.
            </Typography>
            <Button
              variant="outlined"
              onClick={() => window.history.back()}
              sx={{ mt: 2 }}
            >
              Go Back
            </Button>
          </Paper>
        </Box>
      );
    }
  }

  // User is authenticated and has required permissions/roles
  return <>{children}</>;
};

// Higher-order component for easy wrapping
export const withAuth = (
  WrappedComponent: React.ComponentType<any>,
  requiredPermissions?: string[],
  requiredRoles?: string[]
) => {
  return (props: any) => (
    <ProtectedRoute
      requiredPermissions={requiredPermissions}
      requiredRoles={requiredRoles}
    >
      <WrappedComponent {...props} />
    </ProtectedRoute>
  );
};

// Hook for permission-based rendering
export const usePermissionCheck = () => {
  const { hasPermission, isInRole } = useAuth();

  const canAccess = (permissions?: string[], roles?: string[]): boolean => {
    if (permissions && permissions.length > 0) {
      const hasRequiredPermissions = permissions.every(permission => 
        hasPermission(permission)
      );
      if (!hasRequiredPermissions) return false;
    }

    if (roles && roles.length > 0) {
      if (!isInRole(roles)) return false;
    }

    return true;
  };

  return { canAccess, hasPermission, isInRole };
};