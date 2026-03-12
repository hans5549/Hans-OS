import type { Component } from 'vue';

interface VbenDropdownMenuItem {
  disabled?: boolean;
  /**
   * @zh_TW 点击事件处理
   * @param data
   */
  handler?: (data: any) => void;
  /**
   * @zh_TW 图标
   */
  icon?: Component;
  /**
   * @zh_TW 标题
   */
  label: string;
  /**
   * @zh_TW 是否是分割线
   */
  separator?: boolean;
  /**
   * @zh_TW 唯一識別
   */
  value: string;
}

interface DropdownMenuProps {
  menus: VbenDropdownMenuItem[];
}

export type { DropdownMenuProps, VbenDropdownMenuItem };
