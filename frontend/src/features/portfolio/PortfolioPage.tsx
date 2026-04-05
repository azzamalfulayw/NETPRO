import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { portfolioApi, analyticsApi } from '../../services/api'
import { PortfolioPerformance, PortfolioHolding, TransactionType } from '../../types'
import { 
  Briefcase, 
  TrendingUp, 
  TrendingDown, 
  Plus, 
  Trash2, 
  Loader2, 
  AlertCircle,
  PieChart as PieIcon,
  CircleDollarSign,
  ArrowUpRight
} from 'lucide-react'
import { toast } from 'react-hot-toast'
import TradeModal from '../transactions/components/TradeModal'

export default function PortfolioPage() {
  const queryClient = useQueryClient()
  const [symbol, setSymbol] = useState('')
  const [selectedHolding, setSelectedHolding] = useState<PortfolioHolding | null>(null)
  const [isTradeModalOpen, setIsTradeModalOpen] = useState(false)

  const { data: performance, isLoading, isError, refetch } = useQuery<PortfolioPerformance>({
    queryKey: ['portfolio_performance'],
    queryFn: async () => {
      const response = await analyticsApi.getPortfolioPerformance()
      return response.data
    },
  })

  const addHoldingMutation = useMutation({
    mutationFn: (s: string) => portfolioApi.add(s),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['portfolio_performance'] })
      setSymbol('')
      toast.success('Holding added to portfolio')
    },
    onError: () => toast.error('Symbol not found or already in portfolio'),
  })

  const removeHoldingMutation = useMutation({
    mutationFn: (s: string) => portfolioApi.remove(s),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['portfolio_performance'] })
      toast.success('Holding removed')
    },
  })

  if (isLoading) return <div className="flex justify-center py-20"><Loader2 className="w-10 h-10 animate-spin text-blue-500" /></div>
  
  if (isError) return (
    <div className="card border-red-500/20 bg-red-500/5 text-center py-12">
      <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
      <h3 className="text-xl font-bold text-red-400 mb-2">Error Loading Portfolio</h3>
      <button onClick={() => refetch()} className="btn btn-primary mt-4">Retry</button>
    </div>
  )

  const holdings = performance?.holdings || []

  return (
    <div className="space-y-8">
      {/* Portfolio Header / Performance Summary */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="card glass">
          <p className="text-xs text-slate-500 font-bold uppercase tracking-widest mb-1">Total Value</p>
          <p className="text-2xl font-black">${performance?.totalValue.toLocaleString()}</p>
        </div>
        <div className="card glass">
          <p className="text-xs text-slate-500 font-bold uppercase tracking-widest mb-1">Total Invested</p>
          <p className="text-2xl font-black">${performance?.totalInvested.toLocaleString()}</p>
        </div>
        <div className="card glass">
          <p className="text-xs text-slate-500 font-bold uppercase tracking-widest mb-1">Total Gain/Loss</p>
          <div className="flex items-center gap-2">
            <p className={`text-2xl font-black ${performance?.totalGainLoss! >= 0 ? 'text-green-400' : 'text-red-400'}`}>
              ${performance?.totalGainLoss.toLocaleString()}
            </p>
            <span className={`text-sm font-bold ${performance?.totalGainLoss! >= 0 ? 'text-green-500' : 'text-red-500'}`}>
              {performance?.totalGainLossPercent.toFixed(2)}%
            </span>
          </div>
        </div>
        <div className="card glass border-blue-500/20">
          <div className="flex flex-col h-full justify-between gap-4">
             <div className="flex gap-2">
                <input 
                  type="text" 
                  value={symbol}
                  onChange={(e) => setSymbol(e.target.value.toUpperCase())}
                  placeholder="ADD SYMBOL"
                  className="input py-1.5 text-sm uppercase"
                />
                <button 
                  onClick={() => addHoldingMutation.mutate(symbol)}
                  disabled={!symbol || addHoldingMutation.isPending}
                  className="btn btn-primary p-2 flex items-center justify-center disabled:opacity-50"
                >
                  <Plus className="w-5 h-5" />
                </button>
             </div>
             <p className="text-[10px] text-slate-500 text-center uppercase tracking-wider font-bold">
                Quick Track Stock
             </p>
          </div>
        </div>
      </div>

      {/* Holdings Table */}
      <div className="card p-0 overflow-hidden">
        <div className="p-6 border-b border-slate-800 flex justify-between items-center">
            <h3 className="text-xl font-bold flex items-center gap-2">
              <Briefcase className="w-6 h-6 text-blue-500" />
              Active Holdings
            </h3>
            <div className="flex gap-2">
               <div className="bg-slate-800/50 px-3 py-1 rounded inline-flex items-center gap-2 text-xs font-medium text-slate-400">
                  <PieIcon className="w-3 h-3" />
                  {holdings.length} Positions
               </div>
            </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-slate-950/50 text-[10px] uppercase font-black text-slate-500 tracking-widest border-b border-slate-800">
                <th className="px-6 py-4">SYMBOL / COMPANY</th>
                <th className="px-6 py-4 text-right">QUANTITY</th>
                <th className="px-6 py-4 text-right">AVG COST</th>
                <th className="px-6 py-4 text-right">CURRENT PRICE</th>
                <th className="px-6 py-4 text-right">TOTAL VALUE</th>
                <th className="px-6 py-4 text-right">RETURN</th>
                <th className="px-6 py-4 text-center">ACTION</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {holdings.length > 0 ? holdings.map((holding) => (
                <tr key={holding.symbol} className="group hover:bg-slate-800/30 transition-all">
                  <td className="px-6 py-5">
                    <div>
                      <p className="font-bold text-slate-200">{holding.symbol}</p>
                      <p className="text-[10px] text-slate-500">{holding.companyName}</p>
                    </div>
                  </td>
                  <td className="px-6 py-5 text-right font-medium text-slate-400">{holding.quantity.toLocaleString()}</td>
                  <td className="px-6 py-5 text-right font-medium text-slate-400">${holding.averageCostBasis.toFixed(2)}</td>
                  <td className="px-6 py-5 text-right font-bold text-white">${holding.currentPrice.toFixed(2)}</td>
                  <td className="px-6 py-5 text-right font-black text-slate-100">${holding.currentValue.toLocaleString()}</td>
                  <td className="px-6 py-5 text-right font-bold">
                    <div className="flex flex-col items-end">
                       <span className={holding.gainLoss >= 0 ? 'text-green-400' : 'text-red-400'}>
                         {holding.gainLoss >= 0 ? '+' : ''}{holding.gainLoss.toLocaleString()}
                       </span>
                       <span className={`text-[10px] ${holding.gainLoss >= 0 ? 'text-green-500' : 'text-red-500'}`}>
                         {holding.gainLossPercent.toFixed(2)}%
                       </span>
                    </div>
                  </td>
                  <td className="px-6 py-5 text-center flex items-center justify-center gap-2">
                    <button 
                      onClick={() => {
                        setSelectedHolding(holding)
                        setIsTradeModalOpen(true)
                      }}
                      className="p-2 text-purple-500 hover:bg-purple-500/10 rounded-lg transition-colors"
                      title="Sell Position"
                    >
                      <ArrowUpRight className="w-4 h-4" />
                    </button>
                    <button 
                      onClick={() => removeHoldingMutation.mutate(holding.symbol)}
                      className="p-2 text-slate-600 hover:text-red-500 transition-colors"
                      title="Untrack Holding"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              )) : (
                <tr>
                   <td colSpan={7} className="text-center py-20 text-slate-600 italic">
                      No holdings found. Submit a transaction or add a symbol to get started.
                   </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

       <div className="p-8 glass rounded-3xl border border-blue-500/10 flex items-center justify-between shadow-2xl relative overflow-hidden">
          {/* Decorative element */}
          <div className="absolute top-0 right-0 p-8 opacity-5">
             <CircleDollarSign className="w-32 h-32 text-blue-500" />
          </div>
          
          <div className="space-y-2 relative z-10">
             <h3 className="text-2xl font-black italic tracking-tighter">FINALIZE YOUR POSITION?</h3>
             <p className="text-sm text-slate-400 max-w-sm">
                Ready to sell or average down? Log your real trade executions in the Transactions Ledger to keep your history accurate.
             </p>
          </div>
          <button 
            onClick={() => {
              setSelectedHolding(null)
              setIsTradeModalOpen(true)
            }}
            className="btn btn-primary px-8 py-4 text-xs font-black uppercase tracking-widest relative z-10 shadow-blue-500/20"
          >
             Log Execution
             <TrendingUp className="w-4 h-4 ml-2" />
          </button>
       </div>

       <TradeModal 
         isOpen={isTradeModalOpen}
         onClose={() => setIsTradeModalOpen(false)}
         initialSymbol={selectedHolding?.symbol}
         initialType={selectedHolding ? TransactionType.Sell : TransactionType.Buy}
         initialQuantity={selectedHolding?.quantity}
         initialPricePerShare={selectedHolding?.currentPrice}
       />
    </div>
  )
}
