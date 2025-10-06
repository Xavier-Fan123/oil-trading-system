import React from 'react';
import { Box } from '@mui/material';
import { RiskDashboard } from '@/components/Risk/RiskDashboard';

export const Risk: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <RiskDashboard />
    </Box>
  );
};