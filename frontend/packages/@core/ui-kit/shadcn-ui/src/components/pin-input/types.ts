interface PinInputProps {
  class?: any;
  /**
   * 驗證碼长度
   */
  codeLength?: number;
  /**
   * 发送驗證碼按钮文本
   */
  createText?: (countdown: number) => string;
  /**
   * 是否禁用
   */
  disabled?: boolean;
  /**
   * 自定义驗證碼发送逻辑
   * @returns
   */
  handleSendCode?: () => Promise<void>;
  /**
   * 发送驗證碼按钮loading
   */
  loading?: boolean;
  /**
   * 最大重试时间
   */
  maxTime?: number;
}

export type { PinInputProps };
