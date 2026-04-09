import { requestClient } from '#/api/request';

// ── 型別定義 ──────────────────────────────────────

export type TransactionType = 0 | 1; // 0=Income, 1=Expense

export interface BankTransactionResponse {
  id: string;
  bankName: string;
  transactionType: TransactionType;
  transactionDate: string;
  description: string;
  departmentId: string | null;
  departmentName: string | null;
  amount: number;
  fee: number;
  receiptCollected: boolean;
  receiptMailed: boolean;
  runningBalance: number;
  activityId: string | null;
  activityName: string | null;
  pendingRemittanceId: string | null;
}

export interface BankTransactionSummaryResponse {
  openingBalance: number;
  totalIncome: number;
  totalExpense: number;
  closingBalance: number;
}

export interface CreateBankTransactionRequest {
  transactionType: TransactionType;
  transactionDate: string;
  description: string;
  departmentId?: string;
  amount: number;
  fee?: number;
  receiptCollected?: boolean;
  receiptMailed?: boolean;
}

export interface UpdateBankTransactionRequest {
  transactionType: TransactionType;
  transactionDate: string;
  description: string;
  departmentId?: string;
  amount: number;
  fee?: number;
  receiptCollected?: boolean;
  receiptMailed?: boolean;
  activityId?: string;
}

export interface PatchReceiptStatusRequest {
  receiptCollected?: boolean;
  receiptMailed?: boolean;
}

export interface BatchUpdateDepartmentRequest {
  ids: string[];
  departmentId?: null | string;
}

function buildPeriodParams(year: number, month?: number) {
  const params = new URLSearchParams({ year: String(year) });
  if (month !== undefined) {
    params.set('month', String(month));
  }
  return params;
}

function buildPeriodUrl(path: string, year: number, month?: number) {
  return `${path}?${buildPeriodParams(year, month)}`;
}

// ── API 函式 ──────────────────────────────────────

/** 取得銀行交易清單（含逐筆餘額） */
export async function getBankTransactionsApi(
  bankName: string,
  year: number,
  month?: number,
) {
  return requestClient.get<BankTransactionResponse[]>(
    buildPeriodUrl(`/bank-transactions/${encodeURIComponent(bankName)}`, year, month),
  );
}

/** 取得期間摘要（期初/期末餘額、收支合計） */
export async function getBankTransactionSummaryApi(
  bankName: string,
  year: number,
  month?: number,
) {
  return requestClient.get<BankTransactionSummaryResponse>(
    buildPeriodUrl(
      `/bank-transactions/${encodeURIComponent(bankName)}/summary`,
      year,
      month,
    ),
  );
}

/** 新增交易 */
export async function createBankTransactionApi(
  bankName: string,
  data: CreateBankTransactionRequest,
) {
  return requestClient.post<BankTransactionResponse>(
    `/bank-transactions/${encodeURIComponent(bankName)}`,
    data,
  );
}

/** 更新交易 */
export async function updateBankTransactionApi(
  id: string,
  data: UpdateBankTransactionRequest,
) {
  return requestClient.put(`/bank-transactions/${id}`, data);
}

/** 刪除交易 */
export async function deleteBankTransactionApi(id: string) {
  return requestClient.delete(`/bank-transactions/${id}`);
}

/** 批次更新歸屬部門 */
export async function batchUpdateDepartmentApi(
  data: BatchUpdateDepartmentRequest,
) {
  return requestClient.post('/bank-transactions/batch-department', data);
}

/** 匯出 Excel（觸發瀏覽器下載） */
export async function exportBankTransactionsApi(
  bankName: string,
  year: number,
  month?: number,
): Promise<void> {
  const url = buildPeriodUrl(
    `/bank-transactions/${encodeURIComponent(bankName)}/export`,
    year,
    month,
  );
  const blob = await requestClient.download<Blob>(url);
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  const periodLabel = month ? `${year}年${month}月` : `${year}年度`;
  link.href = downloadUrl;
  link.download = `${bankName}收支表_${periodLabel}.xlsx`;
  link.click();
  window.URL.revokeObjectURL(downloadUrl);
}

// ── 匯入相關 ──────────────────────────────────────

export interface BankImportDetail {
  bankName: string;
  transactionCount: number;
  initialBalance: number;
}

export interface ImportResultResponse {
  totalTransactions: number;
  banks: BankImportDetail[];
}

/** 匯入歷史收支資料（全量替換） */
export async function importBankTransactionsApi() {
  return requestClient.post<ImportResultResponse>(
    '/bank-transactions/import',
  );
}

// ── 收據追蹤 ──────────────────────────────────────

export interface ReceiptTrackingResponse {
  id: string;
  bankName: string;
  transactionDate: string;
  description: string;
  departmentId: string | null;
  departmentName: string | null;
  amount: number;
  receiptCollected: boolean;
  receiptMailed: boolean;
}

export interface ReceiptTrackingSummaryResponse {
  totalCount: number;
  notCollectedCount: number;
  notMailedCount: number;
  items: ReceiptTrackingResponse[];
}

/** 更新收據狀態（inline 勾選） */
export async function patchReceiptStatusApi(
  id: string,
  data: PatchReceiptStatusRequest,
) {
  return requestClient.request(`/bank-transactions/${id}/receipt-status`, {
    data,
    method: 'PATCH',
  });
}

/** 取得需關注的收據清單（跨銀行） */
export async function getReceiptTrackingApi(
  year: number,
  month?: number,
) {
  return requestClient.get<ReceiptTrackingSummaryResponse>(
    buildPeriodUrl('/bank-transactions/receipt-tracking', year, month),
  );
}
