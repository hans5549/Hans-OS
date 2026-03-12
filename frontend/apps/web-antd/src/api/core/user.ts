import type { UserInfo } from '@vben/types';

import { requestClient } from '#/api/request';

/**
 * 取得使用者信息
 */
export async function getUserInfoApi() {
  return requestClient.get<UserInfo>('/user/info');
}
