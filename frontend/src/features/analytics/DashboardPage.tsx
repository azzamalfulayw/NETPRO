import { useQuery } from '@tanstack/react-query'
import { analyticsApi } from '../../services/api'
import { PortfolioPerformance, PortfolioHistoryItem, DiversificationData } from '../../types'
import { 
  LineChart, 
  Line, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  AreaChart,
  Area
} from 'recharts'
import { 
  Wallet, 
  TrendingUp, 
  TrendingDown, 
  PieChart as PieIcon, 
  Activity,
  ArrowUpRight,
  ArrowDownRight,
  Loader2,
  AlertCircle
} from 'lucide-react'

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4'];

export default function DashboardPage() {
  const { data: performance, isLoading: perfLoading, isError: perfError } = useQuery<PortfolioPerformance>({
    queryKey: ['portfolio_performance'],
    queryFn: async () => {
      const response = await analyticsApi.getPortfolioPerformance()
      return response.data
    },
  })

  const { data: history, isLoading: historyLoading } = useQuery<PortfolioHistoryItem[]>({
    queryKey: ['portfolio_history'],
    queryFn: async () => {
      const response = await analyticsApi.getPortfolioHistory(30)
      return response.data
    },
  })

  const { data: diversification, isLoading: divLoading } = useQuery<DiversificationData>({
    queryKey: ['portfolio_diversification'],
    queryFn: async () => {
      const response = await analyticsApi.getDiversification()
      return response.data
    },
  })

  if (perfLoading || historyLoading || divLoading) {
    return <div className="flex flex-col items-center justify-center py-40 gap-4">
      <Loader2 className="w-12 h-12 text-blue-500 animate-spin" />
      <p className="text-slate-500 font-bold tracking-widest text-xs uppercase animate-pulse">Computing Portfolio Metrics...</p>
    </div>
  }

  if (perfError) return (
    <div className="card border-red-500/20 bg-red-500/5 text-center py-12">
      <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
      <h3 className="text-xl font-bold text-red-400 mb-2">Service Unavailable</h3>
      <p className="text-slate-400">Failed to aggregate portfolio analytics.</p>
    </div>
  )

  const isPositive = performance?.totalGainLoss! >= 0

  return (
    <div className="space-y-8 pb-20">
      {/* 4 Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
        <div className="card relative group hover:border-blue-500/50 transition-all cursor-default">
           <div className="flex justify-between items-start mb-4">
              <div className="p-2 bg-blue-500/10 rounded-lg text-blue-500">
                 <Wallet className="w-5 h-5" />
              </div>
              <span className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Total Value</span>
           </div>
           <h3 className="text-3xl font-black italic tracking-tighter">${performance?.totalValue.toLocaleString()}</h3>
           <div className="flex items-center gap-2 mt-2 text-xs font-semibold">
              <span className={isPositive ? 'text-green-500' : 'text-red-500'}>
                 {isPositive ? '+' : ''}{performance?.totalGainLossPercent.toFixed(2)}%
              </span>
              <span className="text-slate-500">all time</span>
           </div>
        </div>

        <div className="card relative transition-all cursor-default">
           <div className="flex justify-between items-start mb-4">
              <div className="p-2 bg-emerald-500/10 rounded-lg text-emerald-500">
                 <TrendingUp className="w-5 h-5" />
              </div>
              <span className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Day Change</span>
           </div>
           <h3 className={`text-3xl font-black italic tracking-tighter ${performance?.dayChange! >= 0 ? 'text-green-400' : 'text-red-400'}`}>
              ${performance?.dayChange.toLocaleString()}
           </h3>
           <div className="flex items-center gap-2 mt-2 text-xs font-semibold">
              <span className="text-slate-400">{performance?.dayChangePercent.toFixed(2)}%</span>
              <span className="text-slate-500 uppercase text-[9px] tracking-widest font-black">Today</span>
           </div>
        </div>

        <div className="card relative transition-all cursor-default">
           <div className="flex justify-between items-start mb-4">
              <div className="p-2 bg-amber-500/10 rounded-lg text-amber-500">
                 <Activity className="w-5 h-5" />
              </div>
              <span className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Total Invested</span>
           </div>
           <h3 className="text-3xl font-black italic tracking-tighter">${performance?.totalInvested.toLocaleString()}</h3>
           <p className="text-[10px] text-slate-500 mt-2 uppercase font-black">Net Cash Inflow</p>
        </div>

        <div className="card relative transition-all cursor-default">
           <div className="flex justify-between items-start mb-4">
              <div className="p-2 bg-purple-500/10 rounded-lg text-purple-500">
                 <PieIcon className="w-5 h-5" />
              </div>
              <span className="text-[10px] font-black text-slate-500 uppercase tracking-widest">Holdings Count</span>
           </div>
           <h3 className="text-3xl font-black italic tracking-tighter">{performance?.holdings.length}</h3>
           <p className="text-[10px] text-slate-500 mt-2 uppercase font-black">Active Positions</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Main Chart */}
        <div className="lg:col-span-2 card p-6 flex flex-col min-h-[400px]">
           <div className="flex justify-between items-center mb-8">
              <div>
                 <h3 className="text-lg font-black italic tracking-tighter uppercase">Equity Growth History</h3>
                 <p className="text-xs text-slate-500">Aggregated performance over the last 30 intervals.</p>
              </div>
              <div className="flex gap-2">
                 <div className="px-2 py-1 bg-slate-950 rounded border border-slate-800 text-[10px] font-black text-slate-400">30D</div>
              </div>
           </div>
           <div className="flex-1 w-full -ml-4">
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={history}>
                <defs>
                  <linearGradient id="colorValue" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3}/>
                    <stop offset="95%" stopColor="#3b82f6" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#1e293b" />
                <XAxis 
                  dataKey="date" 
                  hide={true}
                />
                <YAxis 
                   domain={['auto', 'auto']}
                   axisLine={false}
                   tickLine={false}
                   tick={{fill: '#475569', fontSize: 10, fontWeight: 700}}
                   tickFormatter={(val) => `$${(val / 1000).toFixed(0)}k`}
                />
                <Tooltip 
                   contentStyle={{ backgroundColor: '#0f172a', borderColor: '#1e293b', borderRadius: '12px', fontSize: '12px', fontWeight: 'bold' }}
                   itemStyle={{ color: '#3b82f6' }}
                   labelStyle={{ display: 'none' }}
                   formatter={(val: number) => [`$${val.toLocaleString()}`, 'Portfolio Value']}
                />
                <Area 
                  type="monotone" 
                  dataKey="value" 
                  stroke="#3b82f6" 
                  strokeWidth={4} 
                  fillOpacity={1} 
                  fill="url(#colorValue)" 
                />
              </AreaChart>
            </ResponsiveContainer>
           </div>
        </div>

        {/* Diversification Chart */}
        <div className="card p-6 flex flex-col min-h-[400px]">
           <h3 className="text-lg font-black italic tracking-tighter uppercase mb-2">Market Exposure</h3>
           <p className="text-xs text-slate-500 mb-8 font-medium">Industry distribution and leverage.</p>
           
           <div className="flex-1 flex flex-col justify-center gap-8">
              <div className="h-48 w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={diversification?.industries}
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={80}
                      paddingAngle={5}
                      dataKey="value"
                      nameKey="industry"
                    >
                      {diversification?.industries.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip 
                       contentStyle={{ backgroundColor: '#0f172a', borderColor: '#1e293b', borderRadius: '12px', fontSize: '10px' }}
                    />
                  </PieChart>
                </ResponsiveContainer>
              </div>

              <div className="space-y-3">
                 {diversification?.industries.map((item, index) => (
                    <div key={item.industry} className="flex items-center justify-between group">
                       <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full" style={{ backgroundColor: COLORS[index % COLORS.length] }}></div>
                          <span className="text-[10px] font-black uppercase text-slate-400 group-hover:text-slate-200 transition-colors uppercase truncate max-w-[120px]">
                             {item.industry}
                          </span>
                       </div>
                       <span className="text-[10px] font-black text-slate-100">{item.percentage.toFixed(1)}%</span>
                    </div>
                 ))}
              </div>
           </div>
        </div>
      </div>

      {/* Recent Activity Mini-Ledger */}
      <div className="card p-6">
         <div className="flex justify-between items-center mb-6">
             <h3 className="text-lg font-black italic tracking-tighter uppercase">High Performance Assets</h3>
             <span className="text-[10px] font-black text-blue-500 uppercase bg-blue-500/10 px-2 py-0.5 rounded tracking-widest border border-blue-500/20">LIVE REPORT</span>
         </div>
         
         <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
            {performance?.holdings.slice(0, 3).map((holding) => (
               <div key={holding.symbol} className="flex items-center gap-4 p-4 bg-slate-950/50 rounded-2xl border border-slate-800">
                  <div className={`p-3 rounded-xl ${holding.gainLoss >= 0 ? "bg-green-500/10 text-green-500" : "bg-red-500/10 text-red-500"}`}>
                     {holding.gainLoss >= 0 ? <ArrowUpRight className="w-6 h-6" /> : <ArrowDownRight className="w-6 h-6" />}
                  </div>
                  <div className="flex-1">
                     <p className="font-bold text-slate-200">{holding.symbol}</p>
                     <p className="text-[10px] text-slate-500 uppercase font-black">{holding.companyName}</p>
                  </div>
                  <div className="text-right">
                     <p className={`font-black italic ${holding.gainLoss >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                        {holding.gainLoss >= 0 ? '+' : ''}{holding.gainLossPercent.toFixed(2)}%
                     </p>
                     <p className="text-[10px] text-slate-500 font-bold">${holding.currentValue.toLocaleString()}</p>
                  </div>
               </div>
            ))}
         </div>
      </div>
    </div>
  )
}
