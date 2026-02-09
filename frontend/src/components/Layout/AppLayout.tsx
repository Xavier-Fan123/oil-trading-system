import React, { useState } from 'react';
import { Box } from '@mui/material';
import { Sidebar } from './Sidebar';

interface AppLayoutProps {
  children: React.ReactNode;
}

const DRAWER_WIDTH = 240;
const DRAWER_COLLAPSED_WIDTH = 64;

export const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const [collapsed, setCollapsed] = useState(false);

  const sidebarWidth = collapsed ? DRAWER_COLLAPSED_WIDTH : DRAWER_WIDTH;

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', backgroundColor: 'background.default' }}>
      <Sidebar collapsed={collapsed} onToggleCollapse={() => setCollapsed(!collapsed)} />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: `calc(100% - ${sidebarWidth}px)`,
          transition: 'width 0.2s ease',
          overflow: 'auto',
        }}
      >
        {children}
      </Box>
    </Box>
  );
};
