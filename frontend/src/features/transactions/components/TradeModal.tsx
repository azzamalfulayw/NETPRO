import { useState, useEffect } from 'react'
import { TransactionType } from '../../../types'
import { 
  X, 
  ArrowUpRight, 
  ArrowDownRight, 
  PlusCircle, 
  Loader2 
} from 'lucide-react'
import { useTradeMutation } from '../hooks/useTradeMutation'

interface TradeModalProps {
  isOpen: boolean
  onClose: () => void
  initialSymbol?: string
  initialType?: TransactionType
  initialQuantity?: number
  initialPricePerShare?: number
}

export default function TradeModal({ 
  isOpen, 
  onClose, 
  initialSymbol = '', 
  initialType = TransactionType.Buy,
  initialQuantity = 0,
  initialPricePerShare = 0
}: TradeModalProps) {
  const [formData, setFormData] = useState({
    symbol: initialSymbol,
    type: initialType,
    quantity: initialQuantity,
    pricePerShare: initialPricePerShare,
    notes: '',
    category: 0 // Default: MarketOrder
  })

  // Sync state when props change (if the modal stays mounted)
  useEffect(() => {
    if (isOpen) {
      setFormData({
        symbol: initialSymbol,
        type: initialType,
        quantity: initialQuantity,
        pricePerShare: initialPricePerShare,
        notes: '',
        category: 0
      })
    }
  }, [isOpen, initialSymbol, initialType, initialQuantity, initialPricePerShare])

  const tradeMutation = useTradeMutation(() => {
    onClose()
  })

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm">
       <div className="card w-full max-w-lg shadow-2xl relative bg-slate-900 border-slate-800">
          <button 
            onClick={onClose}
            className="absolute top-4 right-4 p-2 hover:bg-slate-800 rounded-lg text-slate-500 transition-colors"
            aria-label="Close modal"
          >
            <X className="w-5 h-5" />
          </button>

          <h2 className="text-xl font-bold italic tracking-tighter mb-8 flex items-center gap-2">
             <PlusCircle className="w-5 h-5 text-blue-500" />
             EXECUTE TRADE
          </h2>

          <div className="grid grid-cols-2 gap-4 mb-6">
             <button 
                onClick={() => setFormData({...formData, type: TransactionType.Buy})}
                className={`btn py-4 flex flex-col items-center gap-2 transition-all ${formData.type === TransactionType.Buy ? 'bg-blue-600 border-blue-500 text-white shadow-lg shadow-blue-900/40' : 'bg-slate-800 border-slate-700 opacity-50 hover:opacity-80'}`}
             >
                <ArrowDownRight className="w-6 h-6 border rounded border-white/20 p-1" />
                <span className="text-[10px] font-black uppercase">BUY - Accumulate</span>
             </button>
             <button 
                onClick={() => setFormData({...formData, type: TransactionType.Sell})}
                className={`btn py-4 flex flex-col items-center gap-2 transition-all ${formData.type === TransactionType.Sell ? 'bg-purple-600 border-purple-500 text-white shadow-lg shadow-purple-900/40' : 'bg-slate-800 border-slate-700 opacity-50 hover:opacity-80'}`}
             >
                <ArrowUpRight className="w-6 h-6 border rounded border-white/20 p-1" />
                <span className="text-[10px] font-black uppercase">SELL - Liquidate</span>
             </button>
          </div>

          <div className="space-y-4">
             <div className="grid grid-cols-2 gap-4">
                <div>
                   <label className="text-[10px] font-black text-slate-500 uppercase tracking-widest pl-1">Symbol</label>
                   <input 
                     type="text" 
                     className="input uppercase bg-slate-950 border-slate-800 focus:border-blue-500" 
                     placeholder="e.g. AAPL"
                     value={formData.symbol}
                     onChange={(e) => setFormData({...formData, symbol: e.target.value})}
                   />
                </div>
                <div>
                   <label className="text-[10px] font-black text-slate-500 uppercase tracking-widest pl-1">Category</label>
                   <select 
                     className="input bg-slate-950 border-slate-800 focus:border-blue-500"
                     value={formData.category}
                     onChange={(e) => setFormData({...formData, category: parseInt(e.target.value)})}
                   >
                      <option value={0}>Market Order</option>
                      <option value={1}>Limit Order</option>
                      <option value={2}>Stop Order</option>
                   </select>
                </div>
             </div>

             <div className="grid grid-cols-2 gap-4">
                <div>
                   <label className="text-[10px] font-black text-slate-500 uppercase tracking-widest pl-1">Quantity</label>
                   <input 
                     type="number" 
                     className="input bg-slate-950 border-slate-800 focus:border-blue-500" 
                     placeholder="0"
                     value={formData.quantity || ''}
                     onChange={(e) => setFormData({...formData, quantity: parseInt(e.target.value) || 0})}
                   />
                </div>
                <div>
                   <label className="text-[10px] font-black text-slate-500 uppercase tracking-widest pl-1">Price per Share</label>
                   <input 
                     type="number" 
                     className="input bg-slate-950 border-slate-800 focus:border-blue-500" 
                     placeholder="0.00"
                     step="0.01"
                     value={formData.pricePerShare || ''}
                     onChange={(e) => setFormData({...formData, pricePerShare: parseFloat(e.target.value) || 0})}
                   />
                </div>
             </div>
             <div>
                <label className="text-[10px] font-black text-slate-500 uppercase tracking-widest pl-1">Reason / Notes</label>
                <input 
                  type="text" 
                  className="input bg-slate-950 border-slate-800 focus:border-blue-500" 
                  placeholder="e.g. Portfolio rebalancing"
                  value={formData.notes}
                  onChange={(e) => setFormData({...formData, notes: e.target.value})}
                />
             </div>
          </div>

          <div className="mt-8 flex gap-3">
             <button 
               onClick={onClose}
               className="btn btn-secondary flex-1 py-3 text-xs font-black uppercase border border-slate-800 hover:bg-slate-800"
             >
                Cancel
             </button>
             <button 
               onClick={() => tradeMutation.mutate(formData)}
               disabled={tradeMutation.isPending || !formData.symbol || !formData.quantity}
               className="btn btn-primary flex-1 py-3 text-xs font-black uppercase shadow-blue-500/20 active:translate-y-0.5 transition-transform"
             >
                {tradeMutation.isPending ? <Loader2 className="w-5 h-5 animate-spin mx-auto" /> : 'Confirm Order'}
             </button>
          </div>
       </div>
    </div>
  )
}
