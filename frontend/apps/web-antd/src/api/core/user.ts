import type { UserInfo } from '@vben/types';

import { requestClient } from '#/api/request';

/**
 * 取得使用者資訊
 */
export async function getUserInfoApi() {
  return requestClient.get<UserInfo>('/user/info');
}

/**
 * 更新個人資料
 */
export async function updateProfileApi(data: { realName: string }) {
  return requestClient.put('/user/profile', data);
}

/**
 * 修改密碼
 */
export async function changePasswordApi(data: {
  oldPassword: string;
  newPassword: string;
}) {
  return requestClient.post('/user/change-password', data);
}
