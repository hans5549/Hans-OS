import { acceptHMRUpdate, defineStore } from 'pinia';

interface BasicUserInfo {
  [key: string]: any;
  /**
   * 头像
   */
  avatar: string;
  /**
   * 使用者昵称
   */
  realName: string;
  /**
   * 使用者角色
   */
  roles?: string[];
  /**
   * 使用者id
   */
  userId: string;
  /**
   * 使用者名
   */
  username: string;
}

interface AccessState {
  /**
   * 使用者信息
   */
  userInfo: BasicUserInfo | null;
  /**
   * 使用者角色
   */
  userRoles: string[];
}

/**
 * @zh_TW 使用者信息相关
 */
export const useUserStore = defineStore('core-user', {
  actions: {
    setUserInfo(userInfo: BasicUserInfo | null) {
      // 设置使用者信息
      this.userInfo = userInfo;
      // 设置角色信息
      const roles = userInfo?.roles ?? [];
      this.setUserRoles(roles);
    },
    setUserRoles(roles: string[]) {
      this.userRoles = roles;
    },
  },
  state: (): AccessState => ({
    userInfo: null,
    userRoles: [],
  }),
});

// 解决热更新問題
const hot = import.meta.hot;
if (hot) {
  hot.accept(acceptHMRUpdate(useUserStore, hot));
}
