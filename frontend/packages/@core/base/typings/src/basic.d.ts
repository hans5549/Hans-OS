interface BasicOption {
  label: string;
  value: string;
}

type SelectOption = BasicOption;

type TabOption = BasicOption;

interface BasicUserInfo {
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

type ClassType = Array<object | string> | object | string;

export type { BasicOption, BasicUserInfo, ClassType, SelectOption, TabOption };
