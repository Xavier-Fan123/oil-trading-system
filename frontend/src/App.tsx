import { Routes, Route } from 'react-router-dom'
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
import ContractMatchingPage from './pages/ContractMatching'
import { TradeBlotter } from './pages/TradeBlotter'
import PWAInstallPrompt from './components/PWA/PWAInstallPrompt'
import { AppLayout } from './components/Layout/AppLayout'

function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/contracts" element={<Contracts />} />
        <Route path="/sales-contracts" element={<SalesContracts />} />
        <Route path="/contract-matching" element={<ContractMatchingPage />} />
        <Route path="/trade-blotter" element={<TradeBlotter />} />
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
    </AppLayout>
  )
}

export default App
