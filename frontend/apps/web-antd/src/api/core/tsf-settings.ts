import { requestClient } from '#/api/request';

// ── 體育部門 ────────────────────────────────────

export interface DepartmentResponse {
  id: string;
  name: string;
  note: string | null;
}

export interface CreateDepartmentRequest {
  name: string;
  note?: string;
}

export interface UpdateDepartmentRequest {
  name: string;
  note?: string;
}

/** 取得所有體育部門 */
export async function getDepartmentsApi() {
  return requestClient.get<DepartmentResponse[]>('/tsf-settings/departments');
}

/** 新增體育部門 */
export async function createDepartmentApi(data: CreateDepartmentRequest) {
  return requestClient.post<DepartmentResponse>(
    '/tsf-settings/departments',
    data,
  );
}

/** 更新體育部門 */
export async function updateDepartmentApi(
  id: string,
  data: UpdateDepartmentRequest,
) {
  return requestClient.put(`/tsf-settings/departments/${id}`, data);
}

/** 刪除體育部門 */
export async function deleteDepartmentApi(id: string) {
  return requestClient.delete(`/tsf-settings/departments/${id}`);
}

// ── 收支表起始資料 ──────────────────────────────

export interface BankInitialBalanceResponse {
  id: string;
  bankName: string;
  initialAmount: number;
}

export interface UpdateBankInitialBalanceRequest {
  initialAmount: number;
}

/** 取得所有銀行起始資料 */
export async function getBankBalancesApi() {
  return requestClient.get<BankInitialBalanceResponse[]>(
    '/tsf-settings/bank-balances',
  );
}

/** 更新銀行起始金額 */
export async function updateBankBalanceApi(
  id: string,
  data: UpdateBankInitialBalanceRequest,
) {
  return requestClient.put(`/tsf-settings/bank-balances/${id}`, data);
}
