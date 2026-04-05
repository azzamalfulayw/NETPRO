import { useMutation, useQueryClient } from '@tanstack/react-query'
import { transactionsApi, stocksApi } from '../../../services/api'
import { TransactionType } from '../../../types'
import { toast } from 'react-hot-toast'

interface TradeMutationData {
  symbol: string
  type: TransactionType
  quantity: number
  pricePerShare: number
  notes: string
  category: number
}

export function useTradeMutation(onSuccess?: () => void) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: TradeMutationData) => {
      // 1. Resolve Symbol to StockId
      const stocksRes = await stocksApi.getAll({ Symbol: data.symbol });
      const stock = stocksRes.data.find((s: any) => s.symbol.toUpperCase() === data.symbol.toUpperCase());
      
      if (!stock) {
        throw new Error(`Stock symbol "${data.symbol}" not found in marketplace.`);
      }

      // 2. Map to CreateTransactionDto
      const payload = {
        stockId: stock.id,
        type: data.type === TransactionType.Buy ? 0 : 1,
        quantity: data.quantity,
        pricePerShare: data.pricePerShare,
        category: data.category,
        notes: data.notes,
        transactionDate: new Date().toISOString()
      };

      return transactionsApi.create(payload);
    },
    onSuccess: () => {
      // Invalidate all relevant queries
      queryClient.invalidateQueries({ queryKey: ['transactions'] })
      queryClient.invalidateQueries({ queryKey: ['portfolio_performance'] })
      queryClient.invalidateQueries({ queryKey: ['realized_gains'] })
      queryClient.invalidateQueries({ queryKey: ['portfolio_history'] })
      
      // Also invalidate specific stock performance if we can find it
      // Since we don't have the stockId here easily without extra work, 
      // we can invalidate all stock performance keys or let the user handle it
      queryClient.invalidateQueries({ queryKey: ['stock_performance'] })
      
      toast.success('Transaction executed successfully')
      if (onSuccess) onSuccess()
    },
    onError: (error: any) => {
      const msg = error.response?.data || error.message || 'Failed to log transaction';
      toast.error(msg);
    },
  })
}
