import { baseRequestClient, requestClient } from '#/api/request';

export namespace AuthApi {
  export interface ApiEnvelope<T> {
    code: number;
    data: T;
    error: null | string;
    message: string;
  }

  export interface RawResponse<T> {
    data: T;
  }

  /** 登录接口参数 */
  export interface LoginParams {
    password?: string;
    username?: string;
  }

  /** 登录接口返回值 */
  export interface LoginResult {
    accessToken: string;
    expiresIn: number;
  }

  export interface RefreshTokenResult {
    accessToken: string;
    expiresIn: number;
  }
}

/**
 * 登录
 */
export async function loginApi(data: AuthApi.LoginParams) {
  return requestClient.post<AuthApi.LoginResult>('/auth/login', data, {
    withCredentials: true,
  });
}

/**
 * 刷新accessToken
 */
export async function refreshTokenApi() {
  return baseRequestClient.post<
    AuthApi.RawResponse<AuthApi.ApiEnvelope<AuthApi.RefreshTokenResult>>
  >('/auth/refresh', undefined, {
    withCredentials: true,
  });
}

/**
 * 退出登录
 */
export async function logoutApi() {
  return baseRequestClient.post('/auth/logout', undefined, {
    withCredentials: true,
  });
}

/**
 * 获取用户权限码
 */
export async function getAccessCodesApi() {
  return requestClient.get<string[]>('/auth/codes');
}
