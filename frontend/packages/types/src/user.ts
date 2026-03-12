import type { BasicUserInfo } from '@vben-core/typings';

/** 使用者信息 */
interface UserInfo extends BasicUserInfo {
  /**
   * 使用者描述
   */
  desc: string;
  /**
   * 首頁地址
   */
  homePath: string;

  /**
   * accessToken
   */
  token: string;
}

export type { UserInfo };
