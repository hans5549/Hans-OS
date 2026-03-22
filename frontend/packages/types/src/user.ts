import type { BasicUserInfo } from '@vben-core/typings';

/** 用户信息 */
interface UserInfo extends BasicUserInfo {
  /**
   * 用户描述
   */
  desc: string;
  /**
   * 電子信箱
   */
  email: string;
  /**
   * 首页地址
   */
  homePath: string;
  /**
   * 電話
   */
  phone: string;

  /**
   * accessToken
   */
  token: string;
}

export type { UserInfo };
