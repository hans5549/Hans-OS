import type { IContextMenuItem } from '@vben-core/shadcn-ui';
import type { TabDefinition, TabsStyleType } from '@vben-core/typings';

export type TabsEmits = {
  close: [string];
  sortTabs: [number, number];
  unpin: [TabDefinition];
};

export interface TabsProps {
  active?: string;
  /**
   * @zh_TW content class
   * @default tabs-chrome
   */
  contentClass?: string;
  /**
   * @zh_TW 右键菜单
   */
  contextMenus?: (data: any) => IContextMenuItem[];
  /**
   * @zh_TW 是否可以拖拽
   */
  draggable?: boolean;
  /**
   * @zh_TW 间隙
   * @default 7
   * 仅限 tabs-chrome
   */
  gap?: number;
  /**
   * @zh_TW tab 最大宽度
   * 仅限 tabs-chrome
   */
  maxWidth?: number;
  /**
   * @zh_TW 点击中键时关闭Tab
   */
  middleClickToClose?: boolean;

  /**
   * @zh_TW tab最小宽度
   * 仅限 tabs-chrome
   */
  minWidth?: number;

  /**
   * @zh_TW 是否顯示图标
   */
  showIcon?: boolean;
  /**
   * @zh_TW 标签页风格
   */
  styleType?: TabsStyleType;

  /**
   * @zh_TW 選項卡数据
   */
  tabs?: TabDefinition[];

  /**
   * @zh_TW 是否响应滚轮事件
   */
  wheelable?: boolean;
}

export interface TabConfig extends TabDefinition {
  affixTab: boolean;
  closable: boolean;
  icon: string;
  key: string;
  title: string;
}
