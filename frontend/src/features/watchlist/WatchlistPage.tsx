import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { watchlistApi } from '../../services/api'
import { WatchListDto } from '../../types'
import { Link } from 'react-router-dom'
import { 
  Star, 
  Trash2, 
  TrendingUp, 
  ExternalLink, 
  Loader2, 
  AlertCircle,
  Clock,
  Briefcase,
  ShoppingCart
} from 'lucide-react'
import { toast } from 'react-hot-toast'
import TradeModal from '../transactions/components/TradeModal'

export default function WatchlistPage() {
  const queryClient = useQueryClient()
  const [selectedStock, setSelectedStock] = useState<{ symbol: string; price: number } | null>(null)
  const [isTradeModalOpen, setIsTradeModalOpen] = useState(false)

  const { data: watchlist, isLoading, isError, refetch } = useQuery<WatchListDto[]>({
    queryKey: ['watchlist'],
    queryFn: async () => {
      const response = await watchlistApi.get()
      return response.data
    },
  })

  const removeMutation = useMutation({
    mutationFn: (stockId: number) => watchlistApi.remove(stockId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watchlist'] })
      toast.success('Removed from watchlist')
    },
    onError: () => toast.error('Failed to remove item'),
  })

  if (isLoading) return <div className="flex justify-center py-20"><Loader2 className="w-10 h-10 animate-spin text-blue-500" /></div>
  
  if (isError) return (
    <div className="card border-red-500/20 bg-red-500/5 text-center py-12">
      <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
      <h3 className="text-xl font-bold text-red-400 mb-2">Error Loading Watchlist</h3>
      <button onClick={() => refetch()} className="btn btn-primary mt-4">Retry</button>
    </div>
  )

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold flex items-center gap-2">
            <Star className="w-6 h-6 text-amber-500 fill-amber-500" />
            My Watchlist
          </h2>
          <p className="text-slate-400 text-sm mt-1">
            Monitoring {watchlist?.length || 0} stocks for potential opportunities.
          </p>
        </div>
      </div>

      {!watchlist || watchlist.length === 0 ? (
        <div className="card text-center py-24 border-dashed bg-slate-900/30">
          <div className="w-16 h-16 bg-slate-800 rounded-full flex items-center justify-center mx-auto mb-6">
            <Star className="w-8 h-8 text-slate-600" />
          </div>
          <h3 className="text-xl font-semibold text-slate-300">Your watchlist is empty</h3>
          <p className="text-slate-500 mt-2 max-w-sm mx-auto">
            Add stocks from the marketplace to start tracking their performance and ratings.
          </p>
          <Link to="/stocks" className="btn btn-primary mt-8 inline-flex items-center gap-2">
            Explore Stocks
            <TrendingUp className="w-4 h-4" />
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
          {watchlist.map((item) => (
            <div key={item.stockId} className="card hover:border-slate-700 transition-all flex flex-col sm:flex-row gap-6 p-4">
               <div className="flex-1 space-y-4">
                  <div className="flex justify-between items-start">
                    <div>
                      <div className="flex items-center gap-2">
                        <Link to={`/stocks/${item.stockId}`} className="text-lg font-bold hover:text-blue-400 transition-colors">
                          {item.symbol}
                        </Link>
                        <span className="text-xs text-slate-500 font-medium px-2 py-0.5 bg-slate-800 rounded border border-slate-700">
                          {item.companyName}
                        </span>
                      </div>
                      <div className="flex items-center gap-4 mt-2 text-xs text-slate-400">
                        <span className="flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          Added {item.daysOnWatchList} days ago
                        </span>
                        <span className="flex items-center gap-1">
                          <Star className="w-3 h-3 text-amber-500" />
                          {item.averageRating.toFixed(1)} ({item.ratingCount})
                        </span>
                      </div>
                    </div>
                  </div>

                  {item.notes && (
                    <div className="p-3 bg-slate-950 rounded-lg border border-slate-800/50 text-sm text-slate-400 italic">
                      "{item.notes}"
                    </div>
                  )}

                  <div className="flex items-center gap-4 text-[10px] font-black uppercase tracking-widest text-slate-500">
                    <span>{item.industry}</span>
                    <span>•</span>
                    <span>MCAP: ${(item.marketCap / 1000000000).toFixed(2)}B</span>
                  </div>

                  <div className="flex items-center gap-4 text-xs font-semibold">
                    <div className="flex-1 grid grid-cols-2 gap-4">
                       <div className="p-2 rounded bg-slate-800/50 border border-slate-800 flex justify-between">
                          <span className="text-slate-500 uppercase tracking-tighter">Purchase Target</span>
                          <span className="text-blue-400">${item.purchase.toFixed(2)}</span>
                       </div>
                       <div className="p-2 rounded bg-slate-800/50 border border-slate-800 flex justify-between">
                          <span className="text-slate-500 uppercase tracking-tighter">Yield</span>
                          <span className="text-emerald-400">${item.lastDiv.toFixed(2)}</span>
                       </div>
                    </div>
                  </div>
               </div>

               <div className="flex sm:flex-col gap-2 justify-end sm:justify-start border-t sm:border-t-0 sm:border-l border-slate-800 pt-4 sm:pt-0 sm:pl-4 min-w-[120px]">
                  <button 
                    onClick={() => {
                      setSelectedStock({ symbol: item.symbol, price: item.purchase })
                      setIsTradeModalOpen(true)
                    }}
                    className="btn btn-primary flex-1 flex items-center justify-center gap-2 py-2 text-xs"
                  >
                    <ShoppingCart className="w-3 h-3" />
                    Buy
                  </button>
                  <Link 
                    to={`/stocks/${item.stockId}`}
                    className="btn btn-secondary flex-1 flex items-center justify-center gap-2 py-2 text-xs"
                  >
                    <ExternalLink className="w-3 h-3" />
                    Details
                  </Link>
                  <button 
                    onClick={() => removeMutation.mutate(item.stockId)}
                    disabled={removeMutation.isPending}
                    className="btn border border-red-500/20 text-red-500 hover:bg-red-500/10 flex-1 flex items-center justify-center gap-2 py-2 text-xs"
                  >
                    <Trash2 className="w-3 h-3" />
                    Remove
                  </button>
               </div>
            </div>
          ))}
        </div>
      )}

      <div className="p-6 bg-slate-900 rounded-2xl border border-slate-800 flex flex-col md:flex-row items-center gap-6 justify-between shadow-lg">
         <div className="flex items-center gap-4">
            <div className="w-12 h-12 bg-blue-500/10 rounded-full flex items-center justify-center">
               <Briefcase className="w-6 h-6 text-blue-500" />
            </div>
            <div>
               <h4 className="font-bold">Portfolio Integration</h4>
               <p className="text-xs text-slate-400">Ready to buy? Convert your watchlist picks into portfolio holdings.</p>
            </div>
         </div>
         <button 
           onClick={() => {
             setSelectedStock(null)
             setIsTradeModalOpen(true)
           }} 
           className="btn btn-primary whitespace-nowrap"
         >
            Buy Now
         </button>
      </div>

      <TradeModal 
        isOpen={isTradeModalOpen}
        onClose={() => setIsTradeModalOpen(false)}
        initialSymbol={selectedStock?.symbol}
        initialPricePerShare={selectedStock?.price}
      />
    </div>
  )
}
