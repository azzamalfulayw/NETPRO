import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { stocksApi, commentsApi, ratingsApi, watchlistApi, analyticsApi } from '../../services/api'
import { StockDto, CommentDto, RatingDto, TransactionType } from '../../types'
import { 
  ArrowLeft, 
  TrendingUp, 
  TrendingDown, 
  Star, 
  MessageSquare, 
  Calendar, 
  User, 
  Send,
  Plus,
  Bookmark,
  BookmarkCheck,
  Loader2,
  LineChart as ChartIcon,
  ShoppingCart,
  ArrowUpRight
} from 'lucide-react'
import { toast } from 'react-hot-toast'
import TradeModal from '../transactions/components/TradeModal'

export default function StockDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const [commentTitle, setCommentTitle] = useState('')
  const [commentContent, setCommentContent] = useState('')
  const [ratingScore, setRatingScore] = useState(5)
  
  // Trade Modal State
  const [isTradeModalOpen, setIsTradeModalOpen] = useState(false)
  const [tradeType, setTradeType] = useState<TransactionType>(TransactionType.Buy)

  const stockId = parseInt(id || '0')

  const { data: stock, isLoading, isError } = useQuery<StockDto>({
    queryKey: ['stock', stockId],
    queryFn: async () => {
      const response = await stocksApi.getById(stockId)
      return response.data
    },
    enabled: !!stockId,
  })

  const { data: watchlist } = useQuery({
    queryKey: ['watchlist'],
    queryFn: async () => {
      const response = await watchlistApi.get()
      return response.data
    },
  })

  const isInWatchlist = watchlist?.some((item: any) => item.stockId === stockId)

  // Mutations
  const addCommentMutation = useMutation({
    mutationFn: (data: any) => commentsApi.create(stockId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock', stockId] })
      setCommentTitle('')
      setCommentContent('')
      toast.success('Comment added successfully')
    },
    onError: () => toast.error('Failed to add comment'),
  })

  const addRatingMutation = useMutation({
    mutationFn: (score: number) => ratingsApi.create(stockId, { score }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock', stockId] })
      toast.success('Rating submitted')
    },
  })

  const toggleWatchlistMutation = useMutation({
    mutationFn: () => isInWatchlist ? watchlistApi.remove(stockId) : watchlistApi.add(stockId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['watchlist'] })
      toast.success(isInWatchlist ? 'Removed from watchlist' : 'Added to watchlist')
    },
  })

  const { data: performance, isLoading: perfLoading } = useQuery({
    queryKey: ['stock_performance', stockId],
    queryFn: async () => {
      const response = await analyticsApi.getStockPerformance(stockId)
      return response.data
    },
    enabled: !!stockId,
  })

  if (isLoading) return <div className="flex justify-center py-20"><Loader2 className="w-10 h-10 animate-spin text-blue-500" /></div>
  if (isError || !stock) return <div className="text-center py-20 text-red-500">Stock not found</div>

  const isPositive = stock.priceChangePercent >= 0

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Breadcrumbs */}
      <Link to="/stocks" className="inline-flex items-center gap-2 text-slate-400 hover:text-white transition-colors">
        <ArrowLeft className="w-4 h-4" />
        Back to Marketplace
      </Link>

      {/* Hero Section */}
      <div className="flex flex-col lg:flex-row gap-8 items-start">
        <div className="flex-1 w-full space-y-6">
          <div className="flex justify-between items-start">
            <div>
              <div className="flex items-center gap-3 mb-2">
                <h1 className="text-4xl font-extrabold tracking-tight">{stock.symbol}</h1>
                <span className="bg-slate-800 px-3 py-1 rounded-full text-xs font-semibold text-slate-400 uppercase tracking-widest border border-slate-700">
                  {stock.industry}
                </span>
              </div>
              <p className="text-xl text-slate-400 font-medium">{stock.companyName}</p>
            </div>
            
            <div className="flex gap-3">
              <button 
                onClick={() => {
                  setTradeType(TransactionType.Buy)
                  setIsTradeModalOpen(true)
                }}
                className="btn btn-primary flex items-center gap-2 px-6"
              >
                <ShoppingCart className="w-5 h-5" />
                Buy
              </button>
              <button 
                onClick={() => {
                  setTradeType(TransactionType.Sell)
                  setIsTradeModalOpen(true)
                }}
                className="btn border border-purple-500/30 bg-purple-500/10 text-purple-400 hover:bg-purple-500/20 flex items-center gap-2 px-6"
              >
                <ArrowUpRight className="w-5 h-5" />
                Sell
              </button>
              <button 
                onClick={() => toggleWatchlistMutation.mutate()}
                disabled={toggleWatchlistMutation.isPending}
                className={`btn ${isInWatchlist ? 'btn-secondary text-amber-500' : 'btn-primary'} flex items-center gap-2`}
              >
                {isInWatchlist ? <BookmarkCheck className="w-5 h-5" /> : <Bookmark className="w-5 h-5" />}
                {isInWatchlist ? 'Watchlisted' : 'Add to Watchlist'}
              </button>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="card glass relative overflow-hidden">
              <div className="absolute top-0 right-0 p-4 opacity-10">
                <TrendingUp className="w-12 h-12" />
              </div>
              <p className="text-sm text-slate-400 mb-1">Current Price</p>
              <div className="flex items-baseline gap-3">
                <span className="text-3xl font-bold">${stock.currentPrice.toFixed(2)}</span>
                <span className={`text-sm font-semibold flex items-center gap-1 ${isPositive ? 'text-green-400' : 'text-red-400'}`}>
                  {isPositive ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                  {isPositive ? '+' : ''}{stock.priceChangePercent.toFixed(2)}%
                </span>
              </div>
            </div>

            <div className="card glass">
               <p className="text-sm text-slate-400 mb-1">Market Cap</p>
               <span className="text-xl font-bold">${(stock.marketCap / 1000000000).toFixed(2)}B</span>
            </div>

            <div className="card glass">
               <p className="text-sm text-slate-400 mb-1">Last Dividend</p>
               <span className="text-xl font-bold">${stock.lastDiv.toFixed(2)}</span>
            </div>
          </div>
        </div>

        <div className="w-full lg:w-80 space-y-6">
          <div className="card bg-slate-900 border-none shadow-2xl">
            <h3 className="font-bold mb-4 flex items-center gap-2">
              <Star className="w-4 h-4 text-amber-500" />
              Stock Rating
            </h3>
            <div className="flex items-center gap-4 mb-6">
              <span className="text-5xl font-extrabold">{stock.averageRating.toFixed(1)}</span>
              <div>
                <div className="flex gap-0.5 mb-1">
                  {[1,2,3,4,5].map(s => (
                    <Star key={s} className={`w-4 h-4 ${s <= Math.round(stock.averageRating) ? 'text-amber-500 fill-amber-500' : 'text-slate-700'}`} />
                  ))}
                </div>
                <p className="text-xs text-slate-500">{stock.ratingCount} user ratings</p>
              </div>
            </div>

            <div className="space-y-4">
              <p className="text-xs text-slate-400 font-medium">SUBMIT YOUR RATING</p>
              <div className="flex justify-between items-center bg-slate-950 p-3 rounded-lg border border-slate-800">
                <div className="flex gap-2">
                  {[1,2,3,4,5].map(s => (
                    <button 
                      key={s} 
                      onClick={() => setRatingScore(s)}
                      className={`w-8 h-8 rounded flex items-center justify-center transition-all ${ratingScore === s ? 'bg-blue-600 text-white' : 'hover:bg-slate-800 text-slate-500'}`}
                    >
                      {s}
                    </button>
                  ))}
                </div>
                <button 
                  onClick={() => addRatingMutation.mutate(ratingScore)}
                  disabled={addRatingMutation.isPending}
                  className="p-2 bg-blue-600 rounded-lg hover:bg-blue-500"
                >
                  <Plus className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Tabs / Content Sections */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-8">
          {/* Comments Section */}
          <div className="card">
            <h2 className="text-xl font-bold mb-6 flex items-center gap-3">
              <MessageSquare className="w-5 h-5 text-blue-500" />
              Community Discussions
            </h2>

            <div className="space-y-6 mb-8">
              {stock.comments.length > 0 ? stock.comments.map((comment) => (
                <div key={comment.id} className="p-5 bg-slate-950/50 rounded-2xl border border-slate-800/50 space-y-3">
                  <div className="flex justify-between items-start">
                    <h4 className="font-bold text-slate-200">{comment.title}</h4>
                    <span className="text-[10px] text-slate-500 font-medium flex items-center gap-1 uppercase tracking-wider">
                      <Calendar className="w-3 h-3" />
                      {new Date(comment.createdOn).toLocaleDateString()}
                    </span>
                  </div>
                  <p className="text-slate-400 text-sm leading-relaxed">{comment.content}</p>
                  <div className="flex items-center gap-2 pt-2 border-t border-slate-800/30">
                    <div className="w-5 h-5 rounded-full bg-blue-600 flex items-center justify-center text-[10px] font-bold">
                      {comment.createdBy[0].toUpperCase()}
                    </div>
                    <span className="text-xs text-slate-500 font-medium">{comment.createdBy}</span>
                  </div>
                </div>
              )) : (
                <p className="text-center py-8 text-slate-500 italic">No comments yet. Be the first to start the conversation!</p>
              )}
            </div>

            {/* Add Comment Form */}
            <div className="border-t border-slate-800 pt-8 space-y-4">
              <h3 className="font-bold text-sm text-slate-400">POST A COMMENT</h3>
              <div className="space-y-3">
                <input 
                  type="text" 
                  placeholder="Subject"
                  className="input"
                  value={commentTitle}
                  onChange={(e) => setCommentTitle(e.target.value)}
                />
                <textarea 
                  placeholder="Share your thoughts on this stock..."
                  className="input h-32 resize-none"
                  value={commentContent}
                  onChange={(e) => setCommentContent(e.target.value)}
                />
                <button 
                  onClick={() => addCommentMutation.mutate({ title: commentTitle, content: commentContent })}
                  disabled={!commentTitle || !commentContent || addCommentMutation.isPending}
                  className="btn btn-primary w-full flex items-center justify-center gap-2 py-3"
                >
                  {addCommentMutation.isPending ? <Loader2 className="w-5 h-5 animate-spin" /> : <Send className="w-5 h-5" />}
                  Post Comment
                </button>
              </div>
            </div>
          </div>
        </div>

        <div className="space-y-8">
           {/* Analytics Card */}
           <div className="card glass border-blue-500/10 h-full">
              <h3 className="font-bold mb-4 flex items-center gap-3">
                <ChartIcon className="w-5 h-5 text-blue-500" />
                Performance Insights
              </h3>
              
              {perfLoading ? (
                <div className="flex items-center justify-center py-10">
                   <Loader2 className="w-6 h-6 animate-spin text-slate-500" />
                </div>
              ) : performance ? (
                <div className="space-y-4 text-sm">
                  <div className="flex justify-between py-2 border-b border-slate-800">
                     <span className="text-slate-400">Total Transactions</span>
                     <span className="font-bold">{performance.totalTransactions}</span>
                  </div>
                  <div className="flex justify-between py-2 border-b border-slate-800">
                     <span className="text-slate-400">Total Buy Volume</span>
                     <span className="font-bold text-blue-400">
                        {performance.totalBuyQuantity.toLocaleString()} shares
                     </span>
                  </div>
                  <div className="flex justify-between py-2 border-b border-slate-800">
                     <span className="text-slate-400">Total Sell Volume</span>
                     <span className="font-bold text-purple-400">
                        {performance.totalSellQuantity.toLocaleString()} shares
                     </span>
                  </div>
                  <p className="text-xs text-slate-500 leading-relaxed pt-2">
                    Insights are derived from your lifetime transaction activity for <strong>{stock.symbol}</strong>.
                  </p>
                </div>
              ) : (
                <div className="text-center py-10">
                   <p className="text-xs text-slate-600 uppercase font-black tracking-widest">No holding data</p>
                </div>
              )}
           </div>
        </div>
      </div>

      {/* Trade Modal */}
      <TradeModal 
        isOpen={isTradeModalOpen}
        onClose={() => setIsTradeModalOpen(false)}
        initialSymbol={stock.symbol}
        initialType={tradeType}
        initialPricePerShare={stock.currentPrice}
      />
    </div>
  )
}
