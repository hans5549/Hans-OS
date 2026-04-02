import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export interface BuyStockRequest {
  stockSymbol: string;
  stockName: string;
  shares: number;
  pricePerShare: number;
  commissionDiscount?: number;
  tradeDate: string;
  note?: string;
}

export interface SellStockRequest {
  stockSymbol: string;
  shares: number;
  pricePerShare: number;
  commissionDiscount?: number;
  isEtf?: boolean;
  tradeDate: string;
  note?: string;
}

export interface StockTransactionResponse {
  id: string;
  stockSymbol: string;
  stockName: string;
  tradeType: string;
  shares: number;
  pricePerShare: number;
  totalAmount: number;
  commission: number;
  tax: number;
  netAmount: number;
  realizedProfitLoss: number | null;
  tradeDate: string;
  note: string | null;
}

export interface StockHoldingResponse {
  stockSymbol: string;
  stockName: string;
  shares: number;
  averageCost: number;
  totalCost: number;
  totalRealizedPL: number;
}

export interface StockProfitSummaryResponse {
  totalRealizedPL: number;
  totalCommission: number;
  totalTax: number;
  transactionCount: number;
}

// ── API ───────────────────────────────────────────

/** 取得持股列表 */
export const getHoldingsApi = () =>
  requestClient.get<StockHoldingResponse[]>('/finance/stocks/holdings');

/** 取得股票交易紀錄 */
export const getStockTransactionsApi = (symbol?: string) =>
  requestClient.get<StockTransactionResponse[]>(
    '/finance/stocks/transactions',
    { params: { symbol } },
  );

/** 取得股票損益摘要 */
export const getStockProfitSummaryApi = (year?: number) =>
  requestClient.get<StockProfitSummaryResponse>('/finance/stocks/summary', {
    params: { year },
  });

/** 買入股票 */
export const buyStockApi = (data: BuyStockRequest) =>
  requestClient.post<StockTransactionResponse>('/finance/stocks/buy', data);

/** 賣出股票 */
export const sellStockApi = (data: SellStockRequest) =>
  requestClient.post<StockTransactionResponse>('/finance/stocks/sell', data);

/** 刪除股票交易紀錄 */
export const deleteStockTransactionApi = (id: string) =>
  requestClient.delete(`/finance/stocks/transactions/${id}`);
