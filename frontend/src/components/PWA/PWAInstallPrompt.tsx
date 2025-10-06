import React from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  IconButton,
  Snackbar,
  Alert,
  Slide,
  Fade,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  Close as CloseIcon,
  GetApp as InstallIcon,
  CloudOff as OfflineIcon,
  CloudOff as CloudOffIcon,
  Notifications as NotificationsIcon,
  Speed as SpeedIcon,
  Security as SecurityIcon,
  PhoneIphone as PhoneIcon,
} from '@mui/icons-material';
import { usePWA, useServiceWorker } from '../../hooks/usePWA';

interface PWAInstallPromptProps {
  onInstall?: () => void;
  onDismiss?: () => void;
}

const PWAInstallPrompt: React.FC<PWAInstallPromptProps> = ({ onInstall, onDismiss }) => {
  const { isInstallable, isInstalled, isOnline, promptInstall } = usePWA();
  const { isUpdateAvailable, updateApp } = useServiceWorker();
  const [showPrompt, setShowPrompt] = React.useState(false);
  const [showFeatures, setShowFeatures] = React.useState(false);
  const [dismissed, setDismissed] = React.useState(false);

  React.useEffect(() => {
    if (isInstallable && !dismissed && !isInstalled) {
      const timer = setTimeout(() => setShowPrompt(true), 5000); // Show after 5 seconds
      return () => clearTimeout(timer);
    }
  }, [isInstallable, dismissed, isInstalled]);

  const handleInstall = async () => {
    try {
      await promptInstall();
      setShowPrompt(false);
      onInstall?.();
    } catch (error) {
      console.error('Installation failed:', error);
    }
  };

  const handleDismiss = () => {
    setShowPrompt(false);
    setDismissed(true);
    onDismiss?.();
  };

  const handleShowFeatures = () => {
    setShowFeatures(true);
  };

  const features = [
    {
      icon: <OfflineIcon color="primary" />,
      title: 'Work Offline',
      description: 'Access your trading data even without internet connection',
    },
    {
      icon: <SpeedIcon color="primary" />,
      title: 'Faster Loading',
      description: 'Native-like performance with instant startup',
    },
    {
      icon: <NotificationsIcon color="primary" />,
      title: 'Push Notifications',
      description: 'Get real-time alerts for price changes and trade updates',
    },
    {
      icon: <SecurityIcon color="primary" />,
      title: 'Secure & Reliable',
      description: 'Enhanced security with data encryption and secure storage',
    },
    {
      icon: <PhoneIcon color="primary" />,
      title: 'Mobile Optimized',
      description: 'Responsive design that works perfectly on all devices',
    },
  ];

  if (isInstalled) {
    return null;
  }

  return (
    <>
      {/* Install Prompt Snackbar */}
      <Snackbar
        open={showPrompt && isInstallable}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        TransitionComponent={Slide}
        TransitionProps={{}}
      >
        <Card sx={{ minWidth: 300, maxWidth: 400 }}>
          <CardContent sx={{ pb: 1 }}>
            <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={1}>
              <Box display="flex" alignItems="center">
                <InstallIcon color="primary" sx={{ mr: 1 }} />
                <Typography variant="subtitle1" fontWeight="bold">
                  Install Oil Trading App
                </Typography>
              </Box>
              <IconButton size="small" onClick={handleDismiss} aria-label="Dismiss install prompt">
                <CloseIcon fontSize="small" />
              </IconButton>
            </Box>
            
            <Typography variant="body2" color="textSecondary" mb={2}>
              Get the full app experience with offline access and push notifications.
            </Typography>
            
            <Box display="flex" gap={1} justifyContent="flex-end">
              <Button size="small" onClick={handleShowFeatures}>
                Learn More
              </Button>
              <Button
                variant="contained"
                size="small"
                startIcon={<InstallIcon />}
                onClick={handleInstall}
              >
                Install
              </Button>
            </Box>
          </CardContent>
        </Card>
      </Snackbar>

      {/* App Update Available */}
      <Snackbar
        open={isUpdateAvailable}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert
          severity="info"
          action={
            <Button color="inherit" size="small" onClick={updateApp}>
              Update
            </Button>
          }
        >
          A new version of the app is available!
        </Alert>
      </Snackbar>

      {/* Offline Status */}
      <Snackbar
        open={!isOnline}
        anchorOrigin={{ vertical: 'top', horizontal: 'left' }}
        TransitionComponent={Fade}
      >
        <Alert
          severity="warning"
          icon={<CloudOffIcon />}
        >
          You're offline. Some features may be limited.
        </Alert>
      </Snackbar>

      {/* Features Dialog */}
      <Dialog
        open={showFeatures}
        onClose={() => setShowFeatures(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          <Box display="flex" alignItems="center">
            <InstallIcon color="primary" sx={{ mr: 1 }} />
            Why Install Oil Trading App?
          </Box>
        </DialogTitle>
        
        <DialogContent>
          <Typography variant="body2" color="textSecondary" mb={3}>
            Installing the app gives you a native-like experience with enhanced features
            and better performance.
          </Typography>
          
          <List>
            {features.map((feature, index) => (
              <ListItem key={index} sx={{ pl: 0 }}>
                <ListItemIcon>{feature.icon}</ListItemIcon>
                <ListItemText
                  primary={feature.title}
                  secondary={feature.description}
                />
              </ListItem>
            ))}
          </List>
          
          <Box mt={3} p={2} bgcolor="grey.50" borderRadius={1}>
            <Typography variant="body2" fontWeight="bold" mb={1}>
              Installation Requirements:
            </Typography>
            <Typography variant="body2" color="textSecondary" component="div">
              • Chrome, Edge, or Safari browser<br />
              • HTTPS connection<br />
              • ~2MB storage space
            </Typography>
          </Box>
        </DialogContent>
        
        <DialogActions>
          <Button onClick={() => setShowFeatures(false)}>
            Maybe Later
          </Button>
          <Button
            variant="contained"
            startIcon={<InstallIcon />}
            onClick={() => {
              setShowFeatures(false);
              handleInstall();
            }}
          >
            Install Now
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default PWAInstallPrompt;