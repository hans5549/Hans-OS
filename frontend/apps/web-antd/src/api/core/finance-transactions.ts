import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export interface CreateTransactionRequest {
  transactionType: string;
  amount: number;
  transactionDate: string;
  categoryId?: string;
  accountId: string;
  toAccountId?: string;
  note?: string;
}

export interface UpdateTransactionRequest extends CreateTransactionRequest {}

export interface TransactionResponse {
  id: string;
  transactionType: string;
  amount: number;
  transactionDate: string;
  note: string | null;
  categoryId: string | null;
  categoryName: string | null;
  categoryIcon: string | null;
  accountId: string;
  accountName: string;
  toAccountId: string | null;
  toAccountName: string | null;
}

export interface DailyTransactionGroup {
  date: string;
  dayIncome: number;
  dayExpense: number;
  transactions: TransactionResponse[];
}

export interface MonthlySummaryResponse {
  year: number;
  month: number;
  totalIncome: number;
  totalExpense: number;
  balance: number;
}

// ── API ───────────────────────────────────────────

/** 取得月份交易記錄 */
export const getTransactionsApi = (year: number, month: number) =>
  requestClient.get<DailyTransactionGroup[]>('/finance/transactions', {
    params: { year, month },
  });

/** 取得月份收支摘要 */
export const getMonthlySummaryApi = (year: number, month: number) =>
  requestClient.get<MonthlySummaryResponse>('/finance/transactions/summary', {
    params: { year, month },
  });

/** 新增交易 */
export const createTransactionApi = (data: CreateTransactionRequest) =>
  requestClient.post<TransactionResponse>('/finance/transactions', data);

/** 更新交易 */
export const updateTransactionApi = (
  id: string,
  data: UpdateTransactionRequest,
) =>
  requestClient.put<TransactionResponse>(`/finance/transactions/${id}`, data);

/** 刪除交易 */
export const deleteTransactionApi = (id: string) =>
  requestClient.delete(`/finance/transactions/${id}`);
