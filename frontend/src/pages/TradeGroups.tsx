import React from 'react';
import { Box } from '@mui/material';
import { TradeGroupManagement } from '@/components/TradeGroups';

export const TradeGroups: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <TradeGroupManagement />
    </Box>
  );
};