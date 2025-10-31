import { Routes, Route } from 'react-router-dom'
import { Box, AppBar, Toolbar, Typography, Button } from '@mui/material'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import { Dashboard } from './pages/Dashboard'
import { Contracts } from './pages/Contracts'
import { SalesContracts } from './pages/SalesContracts'
import { Positions } from './pages/Positions'
import { Risk } from './pages/Risk'
import { MarketData } from './pages/MarketData'
import { Shipping } from './pages/Shipping'
import { Inventory } from './pages/Inventory'
import { Products } from './pages/Products'
import Users from './pages/Users'
import TradingPartners from './pages/TradingPartners'
import { Tags } from './pages/Tags'
import { TradeGroups } from './pages/TradeGroups'
import { ContractSettlement } from './pages/ContractSettlement'
// GraphQL Demo removed for production stability
import PWAInstallPrompt from './components/PWA/PWAInstallPrompt'

function App() {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard' },
    { path: '/contracts', label: 'Purchase' },
    { path: '/sales-contracts', label: 'Sales' },
    { path: '/settlements', label: 'Settlements' },
    { path: '/shipping', label: 'Shipping' },
    { path: '/inventory', label: 'Inventory' },
    { path: '/products', label: 'Products' },
    { path: '/trading-partners', label: 'Partners' },
    { path: '/tags', label: 'Tags' },
    { path: '/trade-groups', label: 'Trade Groups' },
    { path: '/positions', label: 'Positions' },
    { path: '/market-data', label: 'Market Data' },
    { path: '/risk', label: 'Risk Management' },
    { path: '/users', label: 'Users' },
  ];

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: 'background.default' }}>
      <AppBar position="static">
        <Toolbar>
          <Box sx={{ flexGrow: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="h5" component="div" sx={{ fontWeight: 'bold', fontSize: '2rem', letterSpacing: '0.1em' }}>
              X
            </Typography>
            <Typography variant="caption" sx={{ fontSize: '0.75rem', opacity: 0.8, fontStyle: 'italic' }}>
              made by unispark
            </Typography>
          </Box>
          {navItems.map((item) => (
            <Button
              key={item.path}
              color="inherit"
              component={RouterLink}
              to={item.path}
              sx={{
                backgroundColor: location.pathname === item.path ? 'rgba(255,255,255,0.1)' : 'transparent'
              }}
            >
              {item.label}
            </Button>
          ))}
        </Toolbar>
      </AppBar>

      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/contracts" element={<Contracts />} />
        <Route path="/sales-contracts" element={<SalesContracts />} />
        <Route path="/shipping" element={<Shipping />} />
        <Route path="/inventory" element={<Inventory />} />
        <Route path="/products" element={<Products />} />
        <Route path="/trading-partners" element={<TradingPartners />} />
        <Route path="/tags" element={<Tags />} />
        <Route path="/trade-groups" element={<TradeGroups />} />
        <Route path="/settlements" element={<ContractSettlement />} />
        <Route path="/positions" element={<Positions />} />
        <Route path="/market-data" element={<MarketData />} />
        <Route path="/risk" element={<Risk />} />
        <Route path="/users" element={<Users />} />
      </Routes>

      <PWAInstallPrompt />
    </Box>
  )
}

export default App