import React from 'react';
import { Box, Container, Typography } from '@mui/material';
import { UsersList } from '../components/Users/UsersList';

const Users: React.FC = () => {
  return (
    <Container maxWidth="lg">
      <Box py={3}>
        <Typography variant="h4" component="h1" gutterBottom>
          User Management
        </Typography>
        <Typography variant="body1" color="text.secondary" paragraph>
          Manage system users, roles, and permissions. Create, edit, and manage user accounts for the oil trading platform.
        </Typography>
        
        <UsersList />
      </Box>
    </Container>
  );
};

export default Users;