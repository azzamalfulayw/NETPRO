import { StockDto } from '../../types'
import { Link } from 'react-router-dom'
import { TrendingUp, TrendingDown, Star, MessageSquare, ShoppingCart } from 'lucide-react'

interface StockCardProps {
  stock: StockDto
  onBuy: (stock: StockDto) => void
}

export default function StockCard({ stock, onBuy }: StockCardProps) {
  const isPositive = stock.priceChangePercent >= 0

  return (
    <div className="group relative">
      <div className="card hover:border-blue-500/50 hover:shadow-blue-900/10 transition-all lg:p-5 h-full flex flex-col justify-between">
        <div>
          <div className="flex justify-between items-start mb-4">
            <div>
              <h3 className="font-bold text-lg group-hover:text-blue-400 transition-colors uppercase tracking-tight">{stock.symbol}</h3>
              <p className="text-sm text-slate-500 truncate max-w-[150px] font-medium">{stock.companyName}</p>
            </div>
            <div className={isPositive ? "text-green-400" : "text-red-400"}>
              {isPositive ? <TrendingUp className="w-5 h-5" /> : <TrendingDown className="w-5 h-5" />}
            </div>
          </div>

          <div className="space-y-3">
            <div className="flex justify-between items-end">
              <span className="text-2xl font-black text-slate-100">${stock.currentPrice.toFixed(2)}</span>
              <span className={`text-sm font-bold ${isPositive ? "text-green-400" : "text-red-400"}`}>
                {isPositive ? '+' : ''}{stock.priceChangePercent.toFixed(2)}%
              </span>
            </div>

            <div className="flex items-center gap-4 pt-2 border-t border-slate-800/50 text-[10px] text-slate-500 font-bold uppercase tracking-widest">
              <div className="flex items-center gap-1">
                <Star className="w-3 h-3 text-amber-500 fill-amber-500" />
                <span>{stock.averageRating.toFixed(1)}</span>
              </div>
              <div className="flex items-center gap-1">
                <MessageSquare className="w-3 h-3" />
                <span>{stock.ratingCount}</span>
              </div>
              <div className="ml-auto bg-slate-800 px-2 py-0.5 rounded text-[9px]">
                {stock.industry}
              </div>
            </div>
          </div>
        </div>

        <div className="mt-6 pt-4 border-t border-slate-800/30 flex justify-between items-center relative z-20">
           <button 
             onClick={(e) => {
               e.preventDefault()
               e.stopPropagation()
               onBuy(stock)
             }}
             className="btn btn-primary w-full py-2.5 text-[10px] font-black uppercase tracking-widest flex items-center justify-center gap-2 shadow-lg shadow-blue-900/20 active:translate-y-0.5 transition-all"
           >
             <ShoppingCart className="w-3.5 h-3.5" />
             Buy {stock.symbol}
           </button>
        </div>
      </div>

      {/* Invisible link for card navigation */}
      <Link 
        to={`/stocks/${stock.id}`}
        className="absolute inset-0 z-10"
        aria-label={`View ${stock.symbol} details`}
      />
    </div>
  )
}
