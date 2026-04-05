import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { stocksApi } from '../../services/api'
import { StockDto } from '../../types'
import StockCard from './StockCard'
import { Search, Loader2, RefreshCw, AlertCircle } from 'lucide-react'
import TradeModal from '../transactions/components/TradeModal'

export default function StocksPage() {
  const [search, setSearch] = useState('')
  const [sortBy, setSortBy] = useState('Symbol')
  const [isDescending, setIsDescending] = useState(false)
  
  // Trade Modal State
  const [selectedStock, setSelectedStock] = useState<StockDto | null>(null)
  const [isTradeModalOpen, setIsTradeModalOpen] = useState(false)

  const { data: stocks, isLoading, isError, refetch, isRefetching } = useQuery<StockDto[]>({
    queryKey: ['stocks', search, sortBy, isDescending],
    queryFn: async () => {
      const response = await stocksApi.getAll({
        Symbol: search,
        SortBy: sortBy,
        IsDecsending: isDescending,
      })
      return response.data
    },
  })

  const handleBuy = (stock: StockDto) => {
    setSelectedStock(stock)
    setIsTradeModalOpen(true)
  }

  return (
    <div className="space-y-6">
      {/* Header & Filters */}
      <div className="flex flex-col md:flex-row gap-4 justify-between items-start md:items-center">
        <div className="relative w-full md:w-96 group">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500 group-focus-within:text-blue-500 transition-colors" />
          <input 
            type="text" 
            placeholder="Search by symbol or company..."
            className="input pl-10"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="flex items-center gap-3 w-full md:w-auto">
          <select 
            className="input py-2 text-sm w-full md:w-40"
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
          >
            <option value="Symbol">Sort by Symbol</option>
            <option value="CompanyName">Sort by Name</option>
            <option value="Purchase">Sort by Price</option>
            <option value="Industry">Sort by Industry</option>
          </select>
          
          <button 
            onClick={() => setIsDescending(!isDescending)}
            className="btn btn-secondary px-3"
            title="Toggle Sort Order"
          >
            <RefreshCw className={`w-4 h-4 transition-transform ${isDescending ? 'rotate-180' : ''}`} />
          </button>

          <button 
            onClick={() => refetch()}
            disabled={isLoading || isRefetching}
            className="btn btn-primary px-3 disabled:opacity-50"
          >
            {isRefetching ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
          </button>
        </div>
      </div>

      {/* Grid Content */}
      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <Loader2 className="w-10 h-10 text-blue-500 animate-spin" />
          <p className="text-slate-500 font-medium animate-pulse">Fetching latest market data...</p>
        </div>
      ) : isError ? (
        <div className="card border-red-500/20 bg-red-500/5 text-center py-12">
          <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-xl font-bold text-red-400 mb-2">Connection Error</h3>
          <p className="text-slate-400 max-w-md mx-auto">
            Unable to connect to the backend. Please check your API configuration in Settings.
          </p>
          <button onClick={() => refetch()} className="btn btn-primary mt-6">Try Again</button>
        </div>
      ) : stocks && stocks.length > 0 ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 gap-6">
          {stocks.map((stock) => (
            <StockCard key={stock.id} stock={stock} onBuy={handleBuy} />
          ))}
        </div>
      ) : (
        <div className="card text-center py-20 text-slate-500 border-dashed">
          <Search className="w-12 h-12 mx-auto mb-4 opacity-20" />
          <p className="text-lg">No stocks found matching your search.</p>
        </div>
      )}

      {/* Trade Modal */}
      <TradeModal 
        isOpen={isTradeModalOpen}
        onClose={() => setIsTradeModalOpen(false)}
        initialSymbol={selectedStock?.symbol}
        initialPricePerShare={selectedStock?.currentPrice}
      />
    </div>
  )
}
