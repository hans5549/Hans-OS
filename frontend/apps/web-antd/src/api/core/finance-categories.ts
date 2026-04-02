import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export interface CreateCategoryRequest {
  name: string;
  categoryType: string;
  parentId?: string;
  icon?: string;
  sortOrder?: number;
}

export interface UpdateCategoryRequest {
  name: string;
  icon?: string;
  sortOrder?: number;
}

export interface CategoryResponse {
  id: string;
  name: string;
  categoryType: string;
  icon: string | null;
  sortOrder: number;
  isSystem: boolean;
  children: CategoryResponse[] | null;
}

// ── API ───────────────────────────────────────────

/** 取得分類列表 */
export const getCategoriesApi = (type?: string) =>
  requestClient.get<CategoryResponse[]>('/finance/categories', {
    params: { type },
  });

/** 新增分類 */
export const createCategoryApi = (data: CreateCategoryRequest) =>
  requestClient.post<CategoryResponse>('/finance/categories', data);

/** 更新分類 */
export const updateCategoryApi = (id: string, data: UpdateCategoryRequest) =>
  requestClient.put<CategoryResponse>(`/finance/categories/${id}`, data);

/** 刪除分類 */
export const deleteCategoryApi = (id: string) =>
  requestClient.delete(`/finance/categories/${id}`);
