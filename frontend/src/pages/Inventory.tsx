import React from 'react';
import { Box } from '@mui/material';
import InventoryDashboard from '@/components/Inventory/InventoryDashboard';

export const Inventory: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <InventoryDashboard />
    </Box>
  );
};