import { baseRequestClient, requestClient } from '#/api/request';

export namespace AuthApi {
  export interface LoginParams {
    password: string;
    username: string;
  }

  /** Login response — refreshToken is set via HttpOnly cookie */
  export interface LoginResult {
    accessToken: string;
  }

  /** Refresh response envelope (raw, not unwrapped) */
  export interface RefreshTokenResult {
    code: number;
    data: { accessToken: string };
    message: string;
  }
}

/**
 * Login — POST /auth/login
 * Backend sets HttpOnly refresh token cookie automatically.
 */
export async function loginApi(data: AuthApi.LoginParams) {
  return requestClient.post<AuthApi.LoginResult>('/auth/login', data, {
    withCredentials: true,
  });
}

/**
 * Refresh access token — POST /auth/refresh
 * Uses baseRequestClient (no auth interceptors) to avoid circular refresh.
 * HttpOnly cookie is sent automatically by the browser.
 */
export async function refreshTokenApi() {
  return baseRequestClient.post<AuthApi.RefreshTokenResult>(
    '/auth/refresh',
    undefined,
    { withCredentials: true },
  );
}

/**
 * Logout — POST /auth/logout
 * Backend revokes refresh token and clears the HttpOnly cookie.
 */
export async function logoutApi() {
  return baseRequestClient.post(
    '/auth/logout',
    undefined,
    { withCredentials: true },
  );
}

/**
 * Get permission codes for current user — GET /auth/codes
 */
export async function getAccessCodesApi() {
  return requestClient.get<string[]>('/auth/codes');
}
