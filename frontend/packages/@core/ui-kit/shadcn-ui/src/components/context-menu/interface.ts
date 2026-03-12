import type { Component } from 'vue';

interface IContextMenuItem {
  /**
   * @zh_TW 是否禁用
   */
  disabled?: boolean;
  /**
   * @zh_TW 点击事件处理
   * @param data
   */
  handler?: (data: any) => void;
  /**
   * @zh_TW 是否隐藏
   */
  hidden?: boolean;
  /**
   * @zh_TW 图标
   */
  icon?: Component;
  /**
   * @zh_TW 是否顯示图标
   */
  inset?: boolean;
  /**
   * @zh_TW 唯一識別
   */
  key: string;
  /**
   * @zh_TW 是否是分割线
   */
  separator?: boolean;
  /**
   * @zh_TW 快捷键
   */
  shortcut?: string;
  /**
   * @zh_TW 标题
   */
  text: string;
}
export type { IContextMenuItem };
