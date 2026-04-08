import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { stocksApi, aiRecommendationApi } from '../../services/api'
import { Search, Loader2, Bot, Activity, AlertCircle, RefreshCw } from 'lucide-react'
import { clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

function cn(...inputs: (string | undefined | null | false)[]) {
  return twMerge(clsx(inputs))
}

interface AiRecommendationResponse {
  symbol: string;
  companyName: string;
  rating: string;
  summary: string;
  modelUsed: string;
  generatedAt: string;
}

export default function LivePricesPage() {
  const [livePriceSymbol, setLivePriceSymbol] = useState('')
  const [activeLivePriceSymbol, setActiveLivePriceSymbol] = useState('')
  
  const [aiSymbol, setAiSymbol] = useState('')

  // Query for Live Price
  const { data: livePriceData, isLoading: isLivePriceLoading, isError: isLivePriceError, error: livePriceError, refetch: refetchLivePrice, isFetching: isLivePriceFetching } = useQuery({
    queryKey: ['livePrice', activeLivePriceSymbol],
    queryFn: async () => {
      const response = await stocksApi.getLivePrice(activeLivePriceSymbol)
      return response.data
    },
    enabled: !!activeLivePriceSymbol,
    retry: false
  })

  // Mutation for AI Recommendation
  const { mutate: getRecommendation, data: recommendationData, isPending: isAiLoading, isError: isAiError, error: aiError } = useMutation({
    mutationFn: async (sym: string) => {
      const response = await aiRecommendationApi.getRecommendation(sym)
      return response.data as AiRecommendationResponse
    }
  })

  const handleSearchLivePrice = (e: React.FormEvent) => {
    e.preventDefault()
    if (livePriceSymbol.trim()) {
      setActiveLivePriceSymbol(livePriceSymbol.trim().toUpperCase())
    }
  }

  const handleGetRecommendation = (e: React.FormEvent) => {
    e.preventDefault()
    if (aiSymbol.trim()) {
      getRecommendation(aiSymbol.trim().toUpperCase())
    }
  }

  const renderError = (error: any) => {
    const msg = error?.response?.data?.error || error?.response?.data || error?.message || 'An unexpected error occurred.'
    return (
      <div className="flex items-center gap-3 text-red-400 bg-red-500/10 p-4 rounded-xl border border-red-500/20 mt-4">
        <AlertCircle className="w-5 h-5 flex-shrink-0" />
        <p className="text-sm font-medium">{typeof msg === 'string' ? msg : JSON.stringify(msg)}</p>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center gap-3 mb-6">
        <div className="bg-blue-600/20 p-3 rounded-2xl border border-blue-500/30">
          <Activity className="w-8 h-8 text-blue-400" />
        </div>
        <div>
          <h1 className="text-3xl font-bold text-slate-100 tracking-tight">Market Tools</h1>
          <p className="text-slate-400">Live prices and AI-powered recommendations</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Live Price Section */}
        <div className="card border-slate-800 bg-slate-900/50 flex flex-col">
          <div className="card-header border-b border-slate-800 pb-4 mb-6">
            <h2 className="text-xl font-bold flex items-center gap-2">
              <Activity className="w-5 h-5 text-green-400" />
              Live Stock Price
            </h2>
            <p className="text-sm text-slate-400 mt-1">Check real-time stock quotes instantly.</p>
          </div>
          
          <form onSubmit={handleSearchLivePrice} className="relative w-full group mb-6">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-500 group-focus-within:text-green-400 transition-colors" />
            <input 
              type="text" 
              placeholder="Enter ticker (e.g. AAPL)..."
              className="w-full bg-slate-950 border border-slate-800 rounded-xl py-3 pl-10 pr-24 text-slate-100 placeholder-slate-500 focus:outline-none focus:border-green-500/50 focus:ring-1 focus:ring-green-500/50 transition-all font-mono"
              value={livePriceSymbol}
              onChange={(e) => setLivePriceSymbol(e.target.value)}
              disabled={isLivePriceLoading || isLivePriceFetching}
            />
            <button 
              type="submit" 
              className="absolute right-2 top-1/2 -translate-y-1/2 bg-green-600 hover:bg-green-500 text-white px-4 py-1.5 rounded-lg text-sm font-medium transition-colors disabled:opacity-50"
              disabled={!livePriceSymbol.trim() || isLivePriceLoading || isLivePriceFetching}
            >
              Check
            </button>
          </form>

          <div className="flex-1 flex flex-col justify-center">
            {isLivePriceLoading || isLivePriceFetching ? (
              <div className="flex flex-col items-center justify-center py-10 gap-4">
                <Loader2 className="w-8 h-8 text-green-500 animate-spin" />
                <p className="text-slate-500 text-sm animate-pulse">Fetching live quote...</p>
              </div>
            ) : isLivePriceError ? (
              renderError(livePriceError)
            ) : livePriceData ? (
              <div className="text-center p-8 bg-slate-950 rounded-2xl border border-slate-800 relative overflow-hidden group">
                <div className="absolute top-0 left-0 w-full h-1 bg-green-500/50 transform origin-left transition-transform duration-500"></div>
                <div className="text-sm font-black text-slate-400 tracking-widest mb-2">{activeLivePriceSymbol}</div>
                <div className="text-5xl font-mono font-bold text-white tracking-tighter shadow-sm flex items-center justify-center gap-2">
                  <span className="text-green-400">$</span>
                  {typeof livePriceData === 'number' 
                    ? livePriceData.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                    : (livePriceData as any).currentPrice?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) || "N/A"}
                </div>
                {!(typeof livePriceData === 'number') && (livePriceData as any).changePercent !== undefined && (
                  <div className={cn(
                    "mt-3 flex items-center justify-center gap-2 text-sm font-bold", 
                    (livePriceData as any).changeAmount >= 0 ? "text-green-400" : "text-red-400"
                  )}>
                    <span>{(livePriceData as any).changeAmount > 0 ? '+' : ''}{(livePriceData as any).changeAmount?.toFixed(2)}</span>
                    <span>({(livePriceData as any).changePercent > 0 ? '+' : ''}{(livePriceData as any).changePercent?.toFixed(2)}%)</span>
                  </div>
                )}
                <p className="text-xs font-semibold text-slate-500 uppercase tracking-widest mt-6 flex items-center justify-center gap-1">
                  <Activity className="w-3 h-3" /> Live Market Data
                </p>
                <button onClick={() => refetchLivePrice()} className="absolute top-4 right-4 text-slate-500 hover:text-green-400 transition-colors">
                  <RefreshCw className="w-4 h-4" />
                </button>
              </div>
            ) : (
              <div className="text-center py-10 opacity-30 flex flex-col items-center">
                <Activity className="w-12 h-12 mb-3" />
                <p>Enter a symbol to view its live price</p>
              </div>
            )}
          </div>
        </div>

        {/* AI Recommendation Section */}
        <div className="card border-slate-800 bg-slate-900/50 flex flex-col">
          <div className="card-header border-b border-slate-800 pb-4 mb-6">
            <h2 className="text-xl font-bold flex items-center gap-2">
              <Bot className="w-5 h-5 text-indigo-400" />
              AI Analyst Recommendation
            </h2>
            <p className="text-sm text-slate-400 mt-1">On-demand stock analysis. Uses generation tokens.</p>
          </div>

          <form onSubmit={handleGetRecommendation} className="relative w-full group mb-6">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-500 group-focus-within:text-indigo-400 transition-colors" />
            <input 
              type="text" 
              placeholder="Enter ticker (e.g. MSFT)..."
              className="w-full bg-slate-950 border border-slate-800 rounded-xl py-3 pl-10 pr-32 text-slate-100 placeholder-slate-500 focus:outline-none focus:border-indigo-500/50 focus:ring-1 focus:ring-indigo-500/50 transition-all font-mono"
              value={aiSymbol}
              onChange={(e) => setAiSymbol(e.target.value)}
              disabled={isAiLoading}
            />
            <button 
              type="submit" 
              className="absolute right-2 top-1/2 -translate-y-1/2 bg-indigo-600 hover:bg-indigo-500 text-white px-4 py-1.5 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 flex items-center gap-1"
              disabled={!aiSymbol.trim() || isAiLoading}
            >
              <Bot className="w-4 h-4" /> Analyze
            </button>
          </form>

          <div className="flex-1 flex flex-col">
            {isAiLoading ? (
              <div className="flex flex-col items-center justify-center py-14 gap-4 bg-slate-950 rounded-2xl border border-slate-800/50">
                <div className="relative">
                  <div className="absolute inset-0 bg-indigo-500 rounded-full blur-xl opacity-20 animate-pulse"></div>
                  <Bot className="w-10 h-10 text-indigo-400 animate-bounce relative z-10" />
                </div>
                <div className="text-center">
                  <p className="text-slate-300 font-medium">AI is thinking...</p>
                  <p className="text-xs text-slate-500 mt-1">Analyzing market sentiment and technicals</p>
                </div>
              </div>
            ) : isAiError ? (
              renderError(aiError)
            ) : recommendationData ? (
              <div className="bg-gradient-to-br from-slate-950 to-indigo-950/20 rounded-2xl border border-indigo-500/20 p-6 overflow-hidden relative">
                <div className="absolute top-0 right-0 p-4 opacity-10">
                  <Bot className="w-32 h-32" />
                </div>
                
                <div className="relative z-10">
                  <div className="flex justify-between items-start mb-6">
                    <div>
                      <h3 className="text-2xl font-black text-white tracking-tight flex items-center gap-2">
                        {recommendationData.symbol}
                      </h3>
                      <p className="text-sm text-indigo-200 mt-1">{recommendationData.companyName}</p>
                    </div>
                    <div className={cn(
                      "px-3 py-1 rounded-full text-xs font-bold uppercase tracking-widest border",
                      recommendationData.rating.includes('Buy') ? "bg-green-500/10 text-green-400 border-green-500/20" :
                      recommendationData.rating.includes('Sell') ? "bg-red-500/10 text-red-400 border-red-500/20" :
                      "bg-yellow-500/10 text-yellow-400 border-yellow-500/20"
                    )}>
                      {recommendationData.rating}
                    </div>
                  </div>

                  <div className="bg-slate-900/80 rounded-xl p-4 border border-slate-800 mb-4 backdrop-blur-sm">
                    <p className="text-slate-300 text-sm leading-relaxed whitespace-pre-wrap">
                      {recommendationData.summary}
                    </p>
                  </div>

                  <div className="flex items-center justify-between mt-6 text-xs font-medium text-slate-500">
                    <div className="flex items-center gap-1">
                      <span className="w-2 h-2 rounded-full bg-indigo-500 animate-pulse"></span>
                      Powered by {recommendationData.modelUsed || 'AI'}
                    </div>
                    {recommendationData.generatedAt && (
                      <div className="text-slate-600">
                        {new Date(recommendationData.generatedAt).toLocaleString()}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-12 opacity-30 flex flex-col items-center justify-center bg-slate-950 rounded-2xl border border-slate-800 border-dashed h-full">
                <Bot className="w-12 h-12 mb-3" />
                <p>Demand-based analyst recommendations</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
