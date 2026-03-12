interface FallbackProps {
  /**
   * 描述
   */
  description?: string;
  /**
   *  @zh_TW 首頁路由地址
   *  @default /
   */
  homePath?: string;
  /**
   * @zh_TW 默认顯示的图片
   * @default pageNotFoundSvg
   */
  image?: string;
  /**
   *  @zh_TW 内置类型
   */
  status?: '403' | '404' | '500' | 'coming-soon' | 'offline';
  /**
   *  @zh_TW 頁面提示语
   */
  title?: string;
}
export type { FallbackProps };
