interface AuthenticationProps {
  /**
   * @zh_TW 驗證碼登入路徑
   */
  codeLoginPath?: string;
  /**
   * @zh_TW 忘记密码路徑
   */
  forgetPasswordPath?: string;

  /**
   * @zh_TW 是否处于載入处理状态
   */
  loading?: boolean;

  /**
   * @zh_TW 二維碼登入路徑
   */
  qrCodeLoginPath?: string;

  /**
   * @zh_TW 註冊路徑
   */
  registerPath?: string;

  /**
   * @zh_TW 是否顯示驗證碼登入
   */
  showCodeLogin?: boolean;
  /**
   * @zh_TW 是否顯示忘记密码
   */
  showForgetPassword?: boolean;

  /**
   * @zh_TW 是否顯示二維碼登入
   */
  showQrcodeLogin?: boolean;

  /**
   * @zh_TW 是否顯示註冊按钮
   */
  showRegister?: boolean;

  /**
   * @zh_TW 是否顯示记住账号
   */
  showRememberMe?: boolean;

  /**
   * @zh_TW 是否顯示第三方登入
   */
  showThirdPartyLogin?: boolean;

  /**
   * @zh_TW 登入框子标题
   */
  subTitle?: string;

  /**
   * @zh_TW 登入框标题
   */
  title?: string;
  /**
   * @zh_TW 提交按钮文本
   */
  submitButtonText?: string;
}

export type { AuthenticationProps };
