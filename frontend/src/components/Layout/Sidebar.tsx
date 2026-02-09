import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Collapse,
  Box,
  Typography,
  IconButton,
  Divider,
  Tooltip,
} from '@mui/material';
import {
  Dashboard as DashboardIcon,
  ShoppingCart as PurchaseIcon,
  Storefront as SalesIcon,
  CompareArrows as MatchingIcon,
  ViewList as BlotterIcon,
  LocalShipping as ShippingIcon,
  Receipt as SettlementIcon,
  Inventory as InventoryIcon,
  ShowChart as PositionsIcon,
  Security as RiskIcon,
  TrendingUp as MarketIcon,
  Category as ProductsIcon,
  People as PartnersIcon,
  GroupWork as TradeGroupsIcon,
  Label as TagsIcon,
  Person as UsersIcon,
  ExpandLess,
  ExpandMore,
  ChevronLeft as CollapseIcon,
  ChevronRight as ExpandIcon,
} from '@mui/icons-material';

const DRAWER_WIDTH = 240;
const DRAWER_COLLAPSED_WIDTH = 64;

interface NavGroup {
  label: string;
  items: NavItem[];
}

interface NavItem {
  path: string;
  label: string;
  icon: React.ReactNode;
}

const navGroups: NavGroup[] = [
  {
    label: 'Overview',
    items: [
      { path: '/', label: 'Dashboard', icon: <DashboardIcon /> },
    ],
  },
  {
    label: 'Trading',
    items: [
      { path: '/contracts', label: 'Purchase Contracts', icon: <PurchaseIcon /> },
      { path: '/sales-contracts', label: 'Sales Contracts', icon: <SalesIcon /> },
      { path: '/contract-matching', label: 'Contract Matching', icon: <MatchingIcon /> },
      { path: '/trade-blotter', label: 'Trade Blotter', icon: <BlotterIcon /> },
    ],
  },
  {
    label: 'Operations',
    items: [
      { path: '/shipping', label: 'Shipping', icon: <ShippingIcon /> },
      { path: '/settlements', label: 'Settlements', icon: <SettlementIcon /> },
      { path: '/inventory', label: 'Inventory', icon: <InventoryIcon /> },
    ],
  },
  {
    label: 'Analytics',
    items: [
      { path: '/positions', label: 'Positions', icon: <PositionsIcon /> },
      { path: '/risk', label: 'Risk Management', icon: <RiskIcon /> },
      { path: '/market-data', label: 'Market Data', icon: <MarketIcon /> },
    ],
  },
  {
    label: 'Reference Data',
    items: [
      { path: '/products', label: 'Products', icon: <ProductsIcon /> },
      { path: '/trading-partners', label: 'Trading Partners', icon: <PartnersIcon /> },
      { path: '/trade-groups', label: 'Trade Groups', icon: <TradeGroupsIcon /> },
      { path: '/tags', label: 'Tags', icon: <TagsIcon /> },
    ],
  },
  {
    label: 'Admin',
    items: [
      { path: '/users', label: 'Users', icon: <UsersIcon /> },
    ],
  },
];

interface SidebarProps {
  collapsed: boolean;
  onToggleCollapse: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ collapsed, onToggleCollapse }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>(() => {
    // Default: expand the group containing the current path
    const initial: Record<string, boolean> = {};
    navGroups.forEach((group) => {
      const hasActive = group.items.some(
        (item) => item.path === location.pathname || (item.path !== '/' && location.pathname.startsWith(item.path))
      );
      initial[group.label] = hasActive || group.label === 'Overview';
    });
    return initial;
  });

  const toggleGroup = (label: string) => {
    if (collapsed) return;
    setExpandedGroups((prev) => ({ ...prev, [label]: !prev[label] }));
  };

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/';
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  const drawerWidth = collapsed ? DRAWER_COLLAPSED_WIDTH : DRAWER_WIDTH;

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        transition: 'width 0.2s ease',
        '& .MuiDrawer-paper': {
          width: drawerWidth,
          boxSizing: 'border-box',
          backgroundColor: '#111422',
          borderRight: '1px solid #2a2d3a',
          transition: 'width 0.2s ease',
          overflowX: 'hidden',
        },
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: collapsed ? 'center' : 'space-between',
          px: collapsed ? 0 : 2,
          py: 1.5,
          minHeight: 56,
        }}
      >
        {!collapsed && (
          <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 0.5 }}>
            <Typography
              variant="h5"
              sx={{ fontWeight: 'bold', fontSize: '1.5rem', letterSpacing: '0.1em', color: '#fff' }}
            >
              X
            </Typography>
            <Typography variant="caption" sx={{ fontSize: '0.6rem', opacity: 0.5, fontStyle: 'italic' }}>
              unispark
            </Typography>
          </Box>
        )}
        {collapsed && (
          <Typography
            variant="h5"
            sx={{ fontWeight: 'bold', fontSize: '1.5rem', letterSpacing: '0.1em', color: '#fff' }}
          >
            X
          </Typography>
        )}
        <IconButton onClick={onToggleCollapse} size="small" sx={{ color: '#b0b0b0' }}>
          {collapsed ? <ExpandIcon /> : <CollapseIcon />}
        </IconButton>
      </Box>

      <Divider sx={{ borderColor: '#2a2d3a' }} />

      {/* Navigation Groups */}
      <List component="nav" sx={{ px: 0.5, py: 1 }}>
        {navGroups.map((group) => (
          <React.Fragment key={group.label}>
            {/* Group Header */}
            {!collapsed && (
              <ListItemButton
                onClick={() => toggleGroup(group.label)}
                sx={{
                  borderRadius: 1,
                  mb: 0.25,
                  py: 0.5,
                  minHeight: 32,
                  '&:hover': { backgroundColor: 'rgba(255,255,255,0.04)' },
                }}
              >
                <ListItemText
                  primary={group.label.toUpperCase()}
                  primaryTypographyProps={{
                    fontSize: '0.65rem',
                    fontWeight: 600,
                    letterSpacing: '0.08em',
                    color: '#6b7280',
                  }}
                />
                {expandedGroups[group.label] ? (
                  <ExpandLess sx={{ fontSize: 16, color: '#6b7280' }} />
                ) : (
                  <ExpandMore sx={{ fontSize: 16, color: '#6b7280' }} />
                )}
              </ListItemButton>
            )}

            {/* Group Items */}
            <Collapse in={collapsed || expandedGroups[group.label]} timeout="auto">
              {group.items.map((item) => {
                const active = isActive(item.path);
                const button = (
                  <ListItemButton
                    key={item.path}
                    onClick={() => navigate(item.path)}
                    sx={{
                      borderRadius: 1,
                      mb: 0.25,
                      py: 0.75,
                      pl: collapsed ? 'auto' : 2.5,
                      justifyContent: collapsed ? 'center' : 'flex-start',
                      backgroundColor: active ? 'rgba(25, 118, 210, 0.15)' : 'transparent',
                      borderLeft: active ? '3px solid #1976d2' : '3px solid transparent',
                      '&:hover': {
                        backgroundColor: active ? 'rgba(25, 118, 210, 0.2)' : 'rgba(255,255,255,0.06)',
                      },
                    }}
                  >
                    <ListItemIcon
                      sx={{
                        minWidth: collapsed ? 0 : 36,
                        color: active ? '#42a5f5' : '#9ca3af',
                        justifyContent: 'center',
                      }}
                    >
                      {item.icon}
                    </ListItemIcon>
                    {!collapsed && (
                      <ListItemText
                        primary={item.label}
                        primaryTypographyProps={{
                          fontSize: '0.8rem',
                          fontWeight: active ? 600 : 400,
                          color: active ? '#fff' : '#d1d5db',
                        }}
                      />
                    )}
                  </ListItemButton>
                );

                if (collapsed) {
                  return (
                    <Tooltip key={item.path} title={item.label} placement="right" arrow>
                      {button}
                    </Tooltip>
                  );
                }
                return button;
              })}
            </Collapse>

            {/* Divider between groups (only when expanded) */}
            {!collapsed && <Divider sx={{ borderColor: '#1e2130', my: 0.5 }} />}
          </React.Fragment>
        ))}
      </List>
    </Drawer>
  );
};
