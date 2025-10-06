import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Avatar,
  Grid,
  Card,
  CardContent,
  CardHeader,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Chip,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Switch,
  FormControlLabel,
  Tab,
  Tabs,
  IconButton,
  Badge,
} from '@mui/material';
import {
  Person,
  Edit,
  Save,
  Cancel,
  Security,
  Notifications,
  Business,
  LocationOn,
  Phone,
  Email,
  Schedule,
  VpnKey,
  Settings,
  Verified,
  Warning,
  PhotoCamera,
  Delete,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { useAuth } from '../../contexts/AuthContext';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index, ...other }) => {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`profile-tabpanel-${index}`}
      aria-labelledby={`profile-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  );
};

interface UserProfileProps {
  onClose?: () => void;
}

export const UserProfile: React.FC<UserProfileProps> = () => {
  const { user, updateUser } = useAuth();
  const [activeTab, setActiveTab] = useState(0);
  const [isEditing, setIsEditing] = useState(false);
  const [loading, setLoading] = useState(false);
  const [changePasswordDialog, setChangePasswordDialog] = useState(false);
  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phone: '',
    companyName: user?.companyName || '',
    department: '',
    position: '',
    location: '',
    timezone: 'UTC',
    language: 'en',
    notifications: {
      email: true,
      push: true,
      sms: false,
      marketUpdates: true,
      contractAlerts: true,
      riskAlerts: true,
    },
  });

  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleInputChange = (field: string, value: any) => {
    if (field.includes('.')) {
      const [parent, child] = field.split('.');
      setFormData(prev => ({
        ...prev,
        [parent]: {
          ...prev[parent as keyof typeof prev] as any,
          [child]: value,
        },
      }));
    } else {
      setFormData(prev => ({ ...prev, [field]: value }));
    }
  };

  const handleSave = async () => {
    setLoading(true);
    try {
      await updateUser({
        firstName: formData.firstName,
        lastName: formData.lastName,
        companyName: formData.companyName,
      });
      setIsEditing(false);
    } catch (error) {
      console.error('Failed to update profile:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleChangePassword = async () => {
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      alert('New passwords do not match');
      return;
    }

    setLoading(true);
    try {
      // In a real app, this would make an API call
      console.log('Changing password...');
      setChangePasswordDialog(false);
      setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (error) {
      console.error('Failed to change password:', error);
    } finally {
      setLoading(false);
    }
  };

  const mockSessions = [
    {
      id: '1',
      device: 'Chrome on Windows',
      location: 'New York, US',
      ipAddress: '192.168.1.100',
      lastActive: new Date(Date.now() - 1000 * 60 * 30), // 30 minutes ago
      isCurrent: true,
    },
    {
      id: '2',
      device: 'Safari on iPhone',
      location: 'New York, US',
      ipAddress: '192.168.1.101',
      lastActive: new Date(Date.now() - 1000 * 60 * 60 * 24), // 1 day ago
      isCurrent: false,
    },
  ];

  const mockLoginHistory = [
    {
      id: '1',
      timestamp: new Date(Date.now() - 1000 * 60 * 30),
      device: 'Chrome on Windows',
      location: 'New York, US',
      ipAddress: '192.168.1.100',
      success: true,
    },
    {
      id: '2',
      timestamp: new Date(Date.now() - 1000 * 60 * 60 * 24),
      device: 'Safari on iPhone',
      location: 'New York, US',
      ipAddress: '192.168.1.101',
      success: true,
    },
    {
      id: '3',
      timestamp: new Date(Date.now() - 1000 * 60 * 60 * 48),
      device: 'Chrome on Unknown',
      location: 'Unknown Location',
      ipAddress: '10.0.0.1',
      success: false,
    },
  ];

  if (!user) return null;

  return (
    <Box sx={{ maxWidth: 1000, mx: 'auto', p: 3 }}>
      {/* Header */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box display="flex" alignItems="center" gap={3}>
            <Badge
              overlap="circular"
              anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
              badgeContent={
                <IconButton size="small" sx={{ bgcolor: 'primary.main', color: 'white' }}>
                  <PhotoCamera sx={{ fontSize: 16 }} />
                </IconButton>
              }
            >
              <Avatar
                sx={{ width: 80, height: 80, fontSize: 32 }}
                src={user.avatar}
              >
                {user.firstName[0]}{user.lastName[0]}
              </Avatar>
            </Badge>
            
            <Box>
              <Typography variant="h4" fontWeight="bold">
                {user.firstName} {user.lastName}
              </Typography>
              <Typography variant="h6" color="text.secondary">
                {user.role}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {user.companyName}
              </Typography>
              <Box display="flex" alignItems="center" gap={1} mt={1}>
                <Verified color="success" sx={{ fontSize: 16 }} />
                <Typography variant="caption" color="success.main">
                  Verified Account
                </Typography>
              </Box>
            </Box>
          </Box>
          
          <Box>
            <Button
              variant={isEditing ? "outlined" : "contained"}
              startIcon={isEditing ? <Cancel /> : <Edit />}
              onClick={() => setIsEditing(!isEditing)}
              sx={{ mr: 1 }}
            >
              {isEditing ? 'Cancel' : 'Edit Profile'}
            </Button>
            {isEditing && (
              <Button
                variant="contained"
                startIcon={<Save />}
                onClick={handleSave}
                disabled={loading}
              >
                Save Changes
              </Button>
            )}
          </Box>
        </Box>
      </Paper>

      {/* Navigation Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange} aria-label="profile tabs">
          <Tab label="Personal Info" icon={<Person />} />
          <Tab label="Security" icon={<Security />} />
          <Tab label="Notifications" icon={<Notifications />} />
          <Tab label="Sessions" icon={<Schedule />} />
        </Tabs>
      </Box>

      {/* Tab Panels */}
      <TabPanel value={activeTab} index={0}>
        {/* Personal Information */}
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardHeader title="Personal Information" />
              <CardContent>
                <Grid container spacing={2}>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="First Name"
                      value={formData.firstName}
                      onChange={(e) => handleInputChange('firstName', e.target.value)}
                      disabled={!isEditing}
                      InputProps={{
                        startAdornment: <Person sx={{ mr: 1, color: 'action.active' }} />,
                      }}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Last Name"
                      value={formData.lastName}
                      onChange={(e) => handleInputChange('lastName', e.target.value)}
                      disabled={!isEditing}
                      InputProps={{
                        startAdornment: <Person sx={{ mr: 1, color: 'action.active' }} />,
                      }}
                    />
                  </Grid>
                  <Grid item xs={12}>
                    <TextField
                      fullWidth
                      label="Email"
                      value={formData.email}
                      disabled // Email typically can't be changed
                      InputProps={{
                        startAdornment: <Email sx={{ mr: 1, color: 'action.active' }} />,
                      }}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Phone"
                      value={formData.phone}
                      onChange={(e) => handleInputChange('phone', e.target.value)}
                      disabled={!isEditing}
                      InputProps={{
                        startAdornment: <Phone sx={{ mr: 1, color: 'action.active' }} />,
                      }}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Location"
                      value={formData.location}
                      onChange={(e) => handleInputChange('location', e.target.value)}
                      disabled={!isEditing}
                      InputProps={{
                        startAdornment: <LocationOn sx={{ mr: 1, color: 'action.active' }} />,
                      }}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardHeader title="Company Information" />
              <CardContent>
                <Grid container spacing={2}>
                  <Grid item xs={12}>
                    <TextField
                      fullWidth
                      label="Company Name"
                      value={formData.companyName}
                      onChange={(e) => handleInputChange('companyName', e.target.value)}
                      disabled={!isEditing}
                      InputProps={{
                        startAdornment: <Business sx={{ mr: 1, color: 'action.active' }} />,
                      }}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Department"
                      value={formData.department}
                      onChange={(e) => handleInputChange('department', e.target.value)}
                      disabled={!isEditing}
                    />
                  </Grid>
                  <Grid item xs={12} sm={6}>
                    <TextField
                      fullWidth
                      label="Position"
                      value={formData.position}
                      onChange={(e) => handleInputChange('position', e.target.value)}
                      disabled={!isEditing}
                    />
                  </Grid>
                </Grid>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={4}>
            <Card>
              <CardHeader title="Account Status" />
              <CardContent>
                <List>
                  <ListItem>
                    <ListItemIcon>
                      <Verified color="success" />
                    </ListItemIcon>
                    <ListItemText
                      primary="Account Verified"
                      secondary="Your account is verified and active"
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Security color="primary" />
                    </ListItemIcon>
                    <ListItemText
                      primary="Security Score"
                      secondary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="body2">High</Typography>
                          <Chip label="85%" color="success" size="small" />
                        </Box>
                      }
                    />
                  </ListItem>
                  <ListItem>
                    <ListItemIcon>
                      <Schedule color="action" />
                    </ListItemIcon>
                    <ListItemText
                      primary="Last Login"
                      secondary={format(user.lastLoginAt, 'PPpp')}
                    />
                  </ListItem>
                </List>
              </CardContent>
            </Card>

            <Card sx={{ mt: 2 }}>
              <CardHeader title="Permissions" />
              <CardContent>
                <Box display="flex" flexWrap="wrap" gap={1}>
                  {user.permissions.includes('*') ? (
                    <Chip label="Full Access" color="primary" />
                  ) : (
                    user.permissions.map(permission => (
                      <Chip
                        key={permission}
                        label={permission}
                        size="small"
                        variant="outlined"
                      />
                    ))
                  )}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        {/* Security Settings */}
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardHeader title="Password Security" />
              <CardContent>
                <Alert severity="info" sx={{ mb: 2 }}>
                  For your security, we recommend changing your password regularly.
                </Alert>
                <Button
                  variant="contained"
                  startIcon={<VpnKey />}
                  onClick={() => setChangePasswordDialog(true)}
                >
                  Change Password
                </Button>
              </CardContent>
            </Card>

            <Card sx={{ mt: 3 }}>
              <CardHeader title="Two-Factor Authentication" />
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                  <Box>
                    <Typography variant="subtitle1">SMS Authentication</Typography>
                    <Typography variant="body2" color="text.secondary">
                      Receive codes via SMS
                    </Typography>
                  </Box>
                  <Switch defaultChecked />
                </Box>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Box>
                    <Typography variant="subtitle1">Authenticator App</Typography>
                    <Typography variant="body2" color="text.secondary">
                      Use Google Authenticator or similar
                    </Typography>
                  </Box>
                  <Switch />
                </Box>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} md={6}>
            <Card>
              <CardHeader title="Login History" />
              <CardContent>
                <List>
                  {mockLoginHistory.slice(0, 5).map((login) => (
                    <ListItem key={login.id} divider>
                      <ListItemIcon>
                        {login.success ? (
                          <Verified color="success" />
                        ) : (
                          <Warning color="error" />
                        )}
                      </ListItemIcon>
                      <ListItemText
                        primary={login.device}
                        secondary={
                          <Box>
                            <Typography variant="caption" display="block">
                              {format(login.timestamp, 'PPpp')}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {login.location} • {login.ipAddress}
                            </Typography>
                          </Box>
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        {/* Notification Settings */}
        <Card>
          <CardHeader title="Notification Preferences" />
          <CardContent>
            <Grid container spacing={3}>
              <Grid item xs={12} sm={6}>
                <Typography variant="h6" gutterBottom>
                  Delivery Methods
                </Typography>
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.notifications.email}
                      onChange={(e) => handleInputChange('notifications.email', e.target.checked)}
                    />
                  }
                  label="Email Notifications"
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.notifications.push}
                      onChange={(e) => handleInputChange('notifications.push', e.target.checked)}
                    />
                  }
                  label="Push Notifications"
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.notifications.sms}
                      onChange={(e) => handleInputChange('notifications.sms', e.target.checked)}
                    />
                  }
                  label="SMS Notifications"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <Typography variant="h6" gutterBottom>
                  Content Types
                </Typography>
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.notifications.marketUpdates}
                      onChange={(e) => handleInputChange('notifications.marketUpdates', e.target.checked)}
                    />
                  }
                  label="Market Updates"
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.notifications.contractAlerts}
                      onChange={(e) => handleInputChange('notifications.contractAlerts', e.target.checked)}
                    />
                  }
                  label="Contract Alerts"
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={formData.notifications.riskAlerts}
                      onChange={(e) => handleInputChange('notifications.riskAlerts', e.target.checked)}
                    />
                  }
                  label="Risk Alerts"
                />
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        {/* Active Sessions */}
        <Card>
          <CardHeader title="Active Sessions" />
          <CardContent>
            <List>
              {mockSessions.map((session) => (
                <ListItem
                  key={session.id}
                  divider
                  secondaryAction={
                    !session.isCurrent && (
                      <Button size="small" color="error" startIcon={<Delete />}>
                        Revoke
                      </Button>
                    )
                  }
                >
                  <ListItemIcon>
                    <Settings color={session.isCurrent ? 'primary' : 'action'} />
                  </ListItemIcon>
                  <ListItemText
                    primary={
                      <Box display="flex" alignItems="center" gap={1}>
                        {session.device}
                        {session.isCurrent && (
                          <Chip label="Current" color="primary" size="small" />
                        )}
                      </Box>
                    }
                    secondary={
                      <Box>
                        <Typography variant="caption" display="block">
                          {session.location} • {session.ipAddress}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Last active: {format(session.lastActive, 'PPpp')}
                        </Typography>
                      </Box>
                    }
                  />
                </ListItem>
              ))}
            </List>
          </CardContent>
        </Card>
      </TabPanel>

      {/* Change Password Dialog */}
      <Dialog
        open={changePasswordDialog}
        onClose={() => setChangePasswordDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Change Password</DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 1 }}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Current Password"
                type="password"
                value={passwordData.currentPassword}
                onChange={(e) => setPasswordData(prev => ({ ...prev, currentPassword: e.target.value }))}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="New Password"
                type="password"
                value={passwordData.newPassword}
                onChange={(e) => setPasswordData(prev => ({ ...prev, newPassword: e.target.value }))}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Confirm New Password"
                type="password"
                value={passwordData.confirmPassword}
                onChange={(e) => setPasswordData(prev => ({ ...prev, confirmPassword: e.target.value }))}
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setChangePasswordDialog(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleChangePassword}
            variant="contained"
            disabled={loading || !passwordData.currentPassword || !passwordData.newPassword || passwordData.newPassword !== passwordData.confirmPassword}
          >
            Change Password
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};