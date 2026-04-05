import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { transactionsApi, stocksApi } from '../../services/api'
import { TransactionDto, TransactionType, StockDto } from '../../types'
import { 
  History, 
  Search, 
  Filter, 
  Download, 
  Plus, 
  TrendingUp,
  ArrowUpRight, 
  ArrowDownRight, 
  Loader2, 
  AlertCircle,
  FileSpreadsheet,
  PlusCircle,
  X
} from 'lucide-react'
import { toast } from 'react-hot-toast'
import TradeModal from './components/TradeModal'

export default function TransactionsPage() {
  const queryClient = useQueryClient()
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [filterType, setFilterType] = useState<string>('')
  
  const { data: transactions, isLoading, isError } = useQuery<TransactionDto[]>({
    queryKey: ['transactions', search, filterType],
    queryFn: async () => {
      const response = await transactionsApi.getAll({ 
        stockSymbol: search,
        type: filterType ? parseInt(filterType) : undefined
      })
      return response.data
    },
  })

  const { data: realizedGains, refetch: refetchGains } = useQuery({
    queryKey: ['realized_gains'],
    queryFn: async () => {
      const response = await transactionsApi.getRealizedGains()
      return response.data
    },
  })

  const handleExport = async () => {
     try {
       const response = await transactionsApi.exportCsv()
       const url = window.URL.createObjectURL(new Blob([response.data]))
       const link = document.createElement('a')
       link.href = url
       link.setAttribute('download', `transactions_export_${new Date().toISOString()}.csv`)
       document.body.appendChild(link)
       link.click()
       link.remove()
       toast.success('Export started')
     } catch (error) {
       toast.error('Export failed')
     }
  }

  if (isLoading) return <div className="flex justify-center py-20"><Loader2 className="w-10 h-10 animate-spin text-blue-500" /></div>
  
  return (
    <div className="space-y-8 pb-20">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
           <h2 className="text-2xl font-black italic tracking-tighter flex items-center gap-3">
              <History className="w-6 h-6 text-blue-500" />
              TRANSACTIONS LEDGER
           </h2>
           <p className="text-xs text-slate-500 font-medium uppercase tracking-widest mt-1">
              Historical record of your equity executions.
           </p>
        </div>
        <div className="flex gap-3">
           <button 
             onClick={handleExport}
             className="btn btn-secondary px-4 py-2 text-xs flex items-center gap-2 border border-slate-800"
           >
              <FileSpreadsheet className="w-4 h-4" />
              Export CSV
           </button>
           <button 
             onClick={() => setIsFormOpen(true)}
             className="btn btn-primary px-4 py-2 text-xs flex items-center gap-2"
           >
              <PlusCircle className="w-4 h-4" />
              Log Execution
           </button>
        </div>
      </div>

      {/* Gains Summary Panel */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
         <div className="card bg-slate-900 shadow-2xl relative group overflow-hidden">
            <div className="absolute top-0 right-0 p-4 opacity-5 group-hover:scale-110 transition-transform">
               <TrendingUp className="w-12 h-12" />
            </div>
            <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mb-1">Realized Gains</p>
            <div className="flex items-baseline gap-2">
               <p className="text-2xl font-black text-green-400">
                  ${realizedGains?.toLocaleString() || '0.00'}
               </p>
               <span className="text-xs text-green-600 font-bold">Total Profit</span>
            </div>
         </div>
      </div>

      {/* Filters Bar */}
      <div className="flex flex-col md:flex-row gap-4 items-center bg-slate-900/50 p-3 rounded-2xl border border-slate-800">
         <div className="relative flex-1 group">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
            <input 
              type="text" 
              placeholder="Filter by Symbol..."
              className="input pl-10 h-10 text-sm"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
         </div>
         <select 
           className="input h-10 text-sm w-full md:w-40"
           value={filterType}
           onChange={(e) => setFilterType(e.target.value)}
         >
            <option value="">All Types</option>
            <option value="0">Buy Orders</option>
            <option value="1">Sell Orders</option>
         </select>
         <button className="btn btn-secondary h-10 px-4 text-sm flex items-center gap-2">
            <Filter className="w-4 h-4" />
            More Filters
         </button>
      </div>

      {/* Ledger Table */}
      <div className="card p-0 border-slate-800 overflow-hidden">
         <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
               <thead>
                  <tr className="bg-slate-950/50 text-[10px] uppercase font-black text-slate-500 tracking-widest border-b border-slate-800">
                     <th className="px-6 py-4">SYMBOL</th>
                     <th className="px-6 py-4">TYPE</th>
                     <th className="px-6 py-4 text-right">QUANTITY</th>
                     <th className="px-6 py-4 text-right">PRICE / SHARE</th>
                     <th className="px-6 py-4 text-right">TOTAL AMOUNT</th>
                     <th className="px-6 py-4">CATEGORY</th>
                     <th className="px-6 py-4">DATE</th>
                  </tr>
               </thead>
               <tbody className="divide-y divide-slate-800">
                  {isError ? (
                     <tr>
                        <td colSpan={7} className="text-center py-20">
                           <div className="flex flex-col items-center gap-2">
                              <AlertCircle className="w-8 h-8 text-red-500" />
                              <p className="text-sm text-red-400 font-bold tracking-tighter">FAILED TO LOAD LEDGER DATA</p>
                              <button 
                                onClick={() => queryClient.invalidateQueries({ queryKey: ['transactions'] })}
                                className="text-[10px] uppercase font-black text-blue-500 hover:underline"
                              >
                                Try Refresh
                              </button>
                           </div>
                        </td>
                     </tr>
                  ) : transactions && transactions.length > 0 ? transactions.map((tx: any) => (
                     <tr key={tx.id} className="hover:bg-slate-800/30 transition-all group">
                        <td className="px-6 py-4">
                           <div className="flex items-center gap-3">
                              <div className={tx.type === TransactionType.Buy ? "bg-blue-500/10 p-1.5 rounded-lg" : "bg-purple-500/10 p-1.5 rounded-lg"}>
                                 <Plus className={`w-3.5 h-3.5 ${tx.type === TransactionType.Buy ? "text-blue-400" : "text-purple-400 rotate-45"}`} />
                              </div>
                              <div>
                                 <p className="font-bold text-slate-200">{tx.symbol}</p>
                                 <p className="text-[10px] text-slate-600">{tx.companyName}</p>
                              </div>
                           </div>
                        </td>
                        <td className="px-6 py-4">
                           <span className={`text-[10px] uppercase font-black px-2 py-1 rounded inline-block ${tx.type === TransactionType.Buy ? 'bg-blue-500/10 text-blue-500 border border-blue-500/20' : 'bg-purple-500/10 text-purple-500 border border-purple-500/20'}`}>
                              {tx.type === TransactionType.Buy ? 'BUY' : 'SELL'}
                           </span>
                        </td>
                        <td className="px-6 py-4 text-right font-medium text-slate-400">{tx.quantity.toLocaleString()}</td>
                        <td className="px-6 py-4 text-right font-medium text-slate-400">${tx.pricePerShare.toFixed(2)}</td>
                        <td className="px-6 py-4 text-right font-black text-slate-100">${tx.totalAmount.toLocaleString()}</td>
                        <td className="px-6 py-4">
                           <span className="text-[10px] text-slate-500 border border-slate-800 px-2 py-0.5 rounded">
                              {tx.category || 'MarketOrder'}
                           </span>
                        </td>
                        <td className="px-6 py-4">
                           <div className="flex flex-col">
                              <span className="text-xs text-slate-400">{new Date(tx.transactionDate).toLocaleDateString()}</span>
                              <span className="text-[10px] text-slate-600">{new Date(tx.transactionDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                           </div>
                        </td>
                     </tr>
                  )) : (
                     <tr>
                        <td colSpan={7} className="text-center py-20 text-slate-600 italic">No historical data available.</td>
                     </tr>
                  )}
               </tbody>
            </table>
         </div>
      </div>

      {/* Transaction Modal */}
      <TradeModal 
        isOpen={isFormOpen} 
        onClose={() => setIsFormOpen(false)} 
      />
    </div>
  )
}
