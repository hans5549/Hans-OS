import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export interface CreateAccountRequest {
  name: string;
  accountType: string;
  initialBalance?: number;
  icon?: string;
  sortOrder?: number;
}

export interface UpdateAccountRequest extends CreateAccountRequest {
  isArchived?: boolean;
}

export interface AccountResponse {
  id: string;
  name: string;
  accountType: string;
  initialBalance: number;
  icon: string | null;
  sortOrder: number;
  isArchived: boolean;
}

export interface AccountBalanceResponse {
  id: string;
  name: string;
  accountType: string;
  initialBalance: number;
  currentBalance: number;
  icon: string | null;
  isArchived: boolean;
}

// ── API ───────────────────────────────────────────

/** 取得所有帳戶 */
export const getAccountsApi = () =>
  requestClient.get<AccountResponse[]>('/finance/accounts');

/** 新增帳戶 */
export const createAccountApi = (data: CreateAccountRequest) =>
  requestClient.post<AccountResponse>('/finance/accounts', data);

/** 更新帳戶 */
export const updateAccountApi = (id: string, data: UpdateAccountRequest) =>
  requestClient.put<AccountResponse>(`/finance/accounts/${id}`, data);

/** 刪除帳戶 */
export const deleteAccountApi = (id: string) =>
  requestClient.delete(`/finance/accounts/${id}`);

/** 取得帳戶餘額 */
export const getAccountBalancesApi = () =>
  requestClient.get<AccountBalanceResponse[]>('/finance/accounts/balances');
