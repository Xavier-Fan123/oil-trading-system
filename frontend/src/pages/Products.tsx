import React from 'react';
import { Box } from '@mui/material';
import ProductsManagement from '@/components/Products/ProductsManagement';

export const Products: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <ProductsManagement />
    </Box>
  );
};