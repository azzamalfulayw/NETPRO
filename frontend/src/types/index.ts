export enum TransactionType {
    Buy = 'Buy',
    Sell = 'Sell'
}

export interface StockDto {
    id: number;
    symbol: string;
    companyName: string;
    purchase: number;
    lastDiv: number;
    industry: string;
    marketCap: number;
    comments: CommentDto[];
    averageRating: number;
    ratingCount: number;
    currentPrice: number;
    priceChangePercent: number;
    lastPriceUpdate: string;
}

export interface CommentDto {
    id: number;
    title: string;
    content: string;
    createdOn: string;
    createdBy: string;
    stockId: number;
}

export interface RatingDto {
    id: number;
    score: number;
    createdOn: string;
    createdBy: string;
    stockId: number;
}

export interface WatchListDto {
    stockId: number;
    symbol: string;
    companyName: string;
    purchase: number;
    lastDiv: number;
    industry: string;
    marketCap: number;
    addedOn: string;
    notes?: string;
    daysOnWatchList: number;
    averageRating: number;
    ratingCount: number;
}

export interface TransactionDto {
    id: number;
    stockId: number;
    symbol: string;
    companyName: string;
    type: TransactionType;
    quantity: number;
    pricePerShare: number;
    totalAmount: number;
    transactionDate: string;
    notes?: string;
    category?: string;
}

export interface PortfolioHolding {
    symbol: string;
    companyName: string;
    quantity: number;
    averageCostBasis: number;
    currentPrice: number;
    currentValue: number;
    totalInvested: number;
    gainLoss: number;
    gainLossPercent: number;
}

export interface PortfolioPerformance {
    totalValue: number;
    totalInvested: number;
    totalGainLoss: number;
    totalGainLossPercent: number;
    dayChange: number;
    dayChangePercent: number;
    holdings: PortfolioHolding[];
}

export interface PortfolioHistoryItem {
    date: string;
    value: number;
}

export interface IndustryDiversification {
    industry: string;
    value: number;
    percentage: number;
}

export interface DiversificationData {
    industries: IndustryDiversification[];
}

export interface StockPerformanceMetrics {
    stockId: number;
    symbol: string;
    companyName: string;
    currentPrice: number;
    priceChangePercent: number;
    lastPriceUpdate: string;
    averageRating: number;
    ratingCount: number;
    totalTransactions: number;
    totalBuyQuantity: number;
    totalSellQuantity: number;
}
