import { ComponentType, Suspense, lazy } from 'react'
import { Route, Routes } from 'react-router-dom'
import PWAInstallPrompt from './components/PWA/PWAInstallPrompt'
import { AppLayout } from './components/Layout/AppLayout'

const lazyNamed = <TModule extends Record<string, ComponentType<any>>>(
  loader: () => Promise<TModule>,
  exportName: keyof TModule,
) =>
  lazy(async () => {
    const module = await loader()
    return { default: module[exportName] }
  })

const Dashboard = lazyNamed(() => import('./pages/Dashboard'), 'Dashboard')
const Contracts = lazyNamed(() => import('./pages/Contracts'), 'Contracts')
const SalesContracts = lazyNamed(() => import('./pages/SalesContracts'), 'SalesContracts')
const Positions = lazyNamed(() => import('./pages/Positions'), 'Positions')
const Risk = lazyNamed(() => import('./pages/Risk'), 'Risk')
const MarketData = lazyNamed(() => import('./pages/MarketData'), 'MarketData')
const Shipping = lazyNamed(() => import('./pages/Shipping'), 'Shipping')
const Inventory = lazyNamed(() => import('./pages/Inventory'), 'Inventory')
const Products = lazyNamed(() => import('./pages/Products'), 'Products')
const Tags = lazyNamed(() => import('./pages/Tags'), 'Tags')
const TradeGroups = lazyNamed(() => import('./pages/TradeGroups'), 'TradeGroups')
const ContractSettlement = lazyNamed(() => import('./pages/ContractSettlement'), 'ContractSettlement')
const TradeBlotter = lazyNamed(() => import('./pages/TradeBlotter'), 'TradeBlotter')
const Users = lazy(() => import('./pages/Users'))
const TradingPartners = lazy(() => import('./pages/TradingPartners'))
const ContractMatchingPage = lazy(() => import('./pages/ContractMatching'))

const routeFallback = (
  <div
    style={{
      display: 'grid',
      minHeight: '40vh',
      placeItems: 'center',
      fontSize: '0.95rem',
      color: '#4b5563',
    }}
  >
    Loading page...
  </div>
)

function App() {
  return (
    <AppLayout>
      <Suspense fallback={routeFallback}>
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
      </Suspense>
      <PWAInstallPrompt />
    </AppLayout>
  )
}

export default App
