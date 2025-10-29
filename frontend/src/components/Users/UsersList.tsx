import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Button,
  TextField,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Chip,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  CircularProgress,
  Pagination,
  TableSortLabel,
  InputAdornment
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Key as KeyIcon
} from '@mui/icons-material';
import { userService } from '../../services/userService';
import { 
  UserSummary, 
  UserRole, 
  UserRoleLabels, 
  GetUsersParams,
  PagedResult 
} from '../../types/user';
import { UserForm } from './UserForm';
import { ChangePasswordDialog } from './ChangePasswordDialog';

export const UsersList: React.FC = () => {
  const [users, setUsers] = useState<PagedResult<UserSummary>>({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedUser, setSelectedUser] = useState<UserSummary | null>(null);
  const [showUserForm, setShowUserForm] = useState(false);
  const [showPasswordDialog, setShowPasswordDialog] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [userToDelete, setUserToDelete] = useState<UserSummary | null>(null);

  // Filters and search
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('');
  const [activeFilter, setActiveFilter] = useState<boolean | ''>('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [sortBy, setSortBy] = useState('lastName');
  const [sortDesc, setSortDesc] = useState(false);

  const loadUsers = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const params: GetUsersParams = {
        page,
        pageSize,
        searchTerm: searchTerm || undefined,
        role: roleFilter || undefined,
        isActive: activeFilter === '' ? undefined : activeFilter,
        sortBy,
        sortDescending: sortDesc
      };

      const result = await userService.getUsers(params);
      setUsers(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load users');
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, [page, pageSize, searchTerm, roleFilter, activeFilter, sortBy, sortDesc]);

  const handleSearch = () => {
    setPage(1);
    loadUsers();
  };

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortDesc(!sortDesc);
    } else {
      setSortBy(field);
      setSortDesc(false);
    }
  };

  const handleDeleteUser = async () => {
    if (!userToDelete) return;

    try {
      await userService.deleteUser(userToDelete.id);
      setDeleteDialogOpen(false);
      setUserToDelete(null);
      loadUsers();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete user');
    }
  };

  const getRoleColor = (role: UserRole): 'primary' | 'secondary' | 'error' | 'warning' => {
    switch (role) {
      case UserRole.Administrator: return 'error';
      case UserRole.RiskManager: return 'warning';
      case UserRole.Trader: return 'primary';
      case UserRole.Viewer: return 'secondary';
      default: return 'secondary';
    }
  };

  const getStatusColor = (isActive: boolean): 'success' | 'default' => {
    return isActive ? 'success' : 'default';
  };

  if (loading && users.items.length === 0) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h5" component="h2">
            User Management
          </Typography>
          <Box display="flex" gap={1}>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={loadUsers}
              disabled={loading}
            >
              Refresh
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => {
                setSelectedUser(null);
                setShowUserForm(true);
              }}
            >
              Add User
            </Button>
          </Box>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* Filters */}
        <Box display="flex" gap={2} mb={3} flexWrap="wrap">
          <TextField
            size="small"
            placeholder="Search users..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            }}
            sx={{ minWidth: 200 }}
          />
          
          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Role</InputLabel>
            <Select
              value={roleFilter}
              label="Role"
              onChange={(e) => setRoleFilter(e.target.value as UserRole | '')}
            >
              <MenuItem value="">All Roles</MenuItem>
              {Object.entries(UserRoleLabels).map(([value, label]) => (
                <MenuItem key={value} value={parseInt(value)}>
                  {label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 120 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={activeFilter}
              label="Status"
              onChange={(e) => {
                const value = e.target.value;
                setActiveFilter(value === '' ? '' : value === 'true');
              }}
            >
              <MenuItem value="">All Users</MenuItem>
              <MenuItem value="true">Active</MenuItem>
              <MenuItem value="false">Inactive</MenuItem>
            </Select>
          </FormControl>
        </Box>

        {/* Users Table */}
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>
                  <TableSortLabel
                    active={sortBy === 'lastName'}
                    direction={sortBy === 'lastName' ? (sortDesc ? 'desc' : 'asc') : 'asc'}
                    onClick={() => handleSort('lastName')}
                  >
                    Name
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortBy === 'email'}
                    direction={sortBy === 'email' ? (sortDesc ? 'desc' : 'asc') : 'asc'}
                    onClick={() => handleSort('email')}
                  >
                    Email
                  </TableSortLabel>
                </TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortBy === 'role'}
                    direction={sortBy === 'role' ? (sortDesc ? 'desc' : 'asc') : 'asc'}
                    onClick={() => handleSort('role')}
                  >
                    Role
                  </TableSortLabel>
                </TableCell>
                <TableCell>Status</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortBy === 'lastLoginAt'}
                    direction={sortBy === 'lastLoginAt' ? (sortDesc ? 'desc' : 'asc') : 'asc'}
                    onClick={() => handleSort('lastLoginAt')}
                  >
                    Last Login
                  </TableSortLabel>
                </TableCell>
                <TableCell align="center">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users.items?.map((user) => (
                <TableRow key={user.id} hover>
                  <TableCell>
                    <Typography variant="body2" fontWeight="medium">
                      {user.fullName}
                    </Typography>
                  </TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>
                    <Chip
                      label={user.roleName}
                      color={getRoleColor(user.role)}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={user.isActive ? 'Active' : 'Inactive'}
                      color={getStatusColor(user.isActive)}
                      size="small"
                      variant={user.isActive ? 'filled' : 'outlined'}
                    />
                  </TableCell>
                  <TableCell>
                    {user.lastLoginAt ? 
                      new Date(user.lastLoginAt).toLocaleDateString() : 
                      'Never'
                    }
                  </TableCell>
                  <TableCell align="center">
                    <Box display="flex" justifyContent="center" gap={1}>
                      <Tooltip title="Edit User">
                        <IconButton
                          size="small"
                          onClick={() => {
                            setSelectedUser(user);
                            setShowUserForm(true);
                          }}
                        >
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Change Password">
                        <IconButton
                          size="small"
                          onClick={() => {
                            setSelectedUser(user);
                            setShowPasswordDialog(true);
                          }}
                        >
                          <KeyIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete User">
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => {
                            setUserToDelete(user);
                            setDeleteDialogOpen(true);
                          }}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Tooltip>
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>

        {/* Pagination */}
        <Box display="flex" justifyContent="center" mt={3}>
          <Pagination
            count={users.totalPages}
            page={page}
            onChange={(_, value) => setPage(value)}
            color="primary"
            showFirstButton
            showLastButton
          />
        </Box>

        {/* User Form Dialog */}
        <UserForm
          open={showUserForm}
          onClose={() => {
            setShowUserForm(false);
            setSelectedUser(null);
          }}
          user={selectedUser}
          onSuccess={() => {
            setShowUserForm(false);
            setSelectedUser(null);
            loadUsers();
          }}
        />

        {/* Change Password Dialog */}
        <ChangePasswordDialog
          open={showPasswordDialog}
          onClose={() => {
            setShowPasswordDialog(false);
            setSelectedUser(null);
          }}
          user={selectedUser}
          onSuccess={() => {
            setShowPasswordDialog(false);
            setSelectedUser(null);
          }}
        />

        {/* Delete Confirmation Dialog */}
        <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
          <DialogTitle>Delete User</DialogTitle>
          <DialogContent>
            <Typography>
              Are you sure you want to delete user "{userToDelete?.fullName}"? 
              This action cannot be undone.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
            <Button onClick={handleDeleteUser} color="error" variant="contained">
              Delete
            </Button>
          </DialogActions>
        </Dialog>
      </CardContent>
    </Card>
  );
};