import { requestClient } from '#/api/request';

export type ArticleBookmarkSourceType = 'ExternalUrl' | 'InternalArticle';

export interface ArticleBookmarkQueryRequest {
  keyword?: string;
  groupId?: string;
  sourceType?: ArticleBookmarkSourceType;
  isPinned?: boolean;
  isRead?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateArticleBookmarkRequest {
  sourceType: ArticleBookmarkSourceType;
  sourceId?: string;
  url?: string;
  title: string;
  customTitle?: string;
  excerptSnapshot?: string;
  coverImageUrl?: string;
  note?: string;
  groupId?: string;
  tags?: string[];
  isPinned?: boolean;
  isRead?: boolean;
}

export interface UpdateArticleBookmarkRequest
  extends CreateArticleBookmarkRequest {}

export interface PatchArticleBookmarkStateRequest {
  isPinned?: boolean;
  isRead?: boolean;
}

export interface ArticleBookmarkResponse {
  id: string;
  sourceType: ArticleBookmarkSourceType;
  sourceId: string | null;
  url: string | null;
  title: string;
  customTitle: string | null;
  excerptSnapshot: string | null;
  coverImageUrl: string | null;
  domain: string | null;
  note: string | null;
  groupId: string | null;
  groupName: string | null;
  tags: string[];
  isPinned: boolean;
  isRead: boolean;
  createdAt: string;
  updatedAt: string;
  lastOpenedAt: string | null;
}

export interface ArticleBookmarkListResponse {
  items: ArticleBookmarkResponse[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateArticleBookmarkGroupRequest {
  name: string;
  sortOrder?: number;
}

export interface UpdateArticleBookmarkGroupRequest
  extends CreateArticleBookmarkGroupRequest {}

export interface ArticleBookmarkGroupResponse {
  id: string;
  name: string;
  sortOrder: number;
  bookmarkCount: number;
}

export const getArticleBookmarksApi = (params: ArticleBookmarkQueryRequest = {}) =>
  requestClient.get<ArticleBookmarkListResponse>('/article-collection', {
    params,
  });

export const createArticleBookmarkApi = (data: CreateArticleBookmarkRequest) =>
  requestClient.post<ArticleBookmarkResponse>('/article-collection', data);

export const updateArticleBookmarkApi = (
  id: string,
  data: UpdateArticleBookmarkRequest,
) => requestClient.put<ArticleBookmarkResponse>(`/article-collection/${id}`, data);

export const patchArticleBookmarkStateApi = (
  id: string,
  data: PatchArticleBookmarkStateRequest,
) =>
  requestClient.request<ArticleBookmarkResponse>(
    `/article-collection/${id}/state`,
    { data, method: 'PATCH' },
  );

export const deleteArticleBookmarkApi = (id: string) =>
  requestClient.delete(`/article-collection/${id}`);

export const getArticleBookmarkGroupsApi = () =>
  requestClient.get<ArticleBookmarkGroupResponse[]>('/article-collection/groups');

export const createArticleBookmarkGroupApi = (
  data: CreateArticleBookmarkGroupRequest,
) => requestClient.post<ArticleBookmarkGroupResponse>('/article-collection/groups', data);

export const updateArticleBookmarkGroupApi = (
  id: string,
  data: UpdateArticleBookmarkGroupRequest,
) =>
  requestClient.put<ArticleBookmarkGroupResponse>(
    `/article-collection/groups/${id}`,
    data,
  );

export const deleteArticleBookmarkGroupApi = (id: string) =>
  requestClient.delete(`/article-collection/groups/${id}`);
