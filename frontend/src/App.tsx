import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import SettingsPage from './pages/SettingsPage'
import StocksPage from './features/stocks/StocksPage'
import StockDetailsPage from './features/stocks/StockDetailsPage'
import WatchlistPage from './features/watchlist/WatchlistPage'
import PortfolioPage from './features/portfolio/PortfolioPage'
import TransactionsPage from './features/transactions/TransactionsPage'
import DashboardPage from './features/analytics/DashboardPage'
import LoginPage from './features/auth/LoginPage'
import { ProtectedRoute } from './components/ProtectedRoute'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        
        <Route path="/" element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="stocks" element={<StocksPage />} />
          <Route path="stocks/:id" element={<StockDetailsPage />} />
          <Route path="watchlist" element={<WatchlistPage />} />
          <Route path="portfolio" element={<PortfolioPage />} />
          <Route path="transactions" element={<TransactionsPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
