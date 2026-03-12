/**
 * API request client configuration for Hans-OS.
 *
 * - requestClient: auto-unwraps ApiEnvelope<T>.data, injects Bearer token
 * - baseRequestClient: raw responses, used for refresh/logout to avoid circular interceptors
 */
import type { RequestClientOptions } from '@vben/request';

import { useAppConfig } from '@vben/hooks';
import { preferences } from '@vben/preferences';
import {
  authenticateResponseInterceptor,
  defaultResponseInterceptor,
  errorMessageResponseInterceptor,
  RequestClient,
} from '@vben/request';
import { useAccessStore } from '@vben/stores';

import { message } from 'ant-design-vue';

import { useAuthStore } from '#/store';

import { refreshTokenApi } from './core';

const { apiURL } = useAppConfig(import.meta.env, import.meta.env.PROD);

function createRequestClient(baseURL: string, options?: RequestClientOptions) {
  const client = new RequestClient({
    ...options,
    baseURL,
  });

  /**
   * Re-authenticate: clear token and redirect to login.
   */
  async function doReAuthenticate() {
    console.warn('Access token or refresh token is invalid or expired.');
    const accessStore = useAccessStore();
    const authStore = useAuthStore();
    accessStore.setAccessToken(null);
    if (
      preferences.app.loginExpiredMode === 'modal' &&
      accessStore.isAccessChecked
    ) {
      accessStore.setLoginExpired(true);
    } else {
      await authStore.logout();
    }
  }

  /**
   * Refresh access token via HttpOnly cookie.
   * Uses baseRequestClient (raw AxiosResponse), so we manually unwrap the envelope.
   */
  async function doRefreshToken() {
    const accessStore = useAccessStore();
    // baseRequestClient has no response interceptors → returns raw AxiosResponse.
    // RequestClient.post<T>() declares Promise<T> but actually returns AxiosResponse,
    // so we need a type assertion to access .data (the HTTP body / ApiEnvelope).
    const resp = (await refreshTokenApi()) as any;
    const newToken = resp.data.data.accessToken as string;
    accessStore.setAccessToken(newToken);
    return newToken;
  }

  function formatToken(token: null | string) {
    return token ? `Bearer ${token}` : null;
  }

  // Request interceptor: inject Bearer token and locale header
  client.addRequestInterceptor({
    fulfilled: async (config) => {
      const accessStore = useAccessStore();

      config.headers.Authorization = formatToken(accessStore.accessToken);
      config.headers['Accept-Language'] = preferences.app.locale;
      return config;
    },
  });

  // Response interceptor: unwrap ApiEnvelope<T> (code/data/message)
  client.addResponseInterceptor(
    defaultResponseInterceptor({
      codeField: 'code',
      dataField: 'data',
      successCode: 0,
    }),
  );

  // 401 handling: refresh token or redirect to login
  client.addResponseInterceptor(
    authenticateResponseInterceptor({
      client,
      doReAuthenticate,
      doRefreshToken,
      enableRefreshToken: preferences.app.enableRefreshToken,
      formatToken,
    }),
  );

  // Generic error handler: display error message via Ant Design message
  client.addResponseInterceptor(
    errorMessageResponseInterceptor((msg: string, error) => {
      // Backend returns errors in ApiEnvelope format: { code: -1, message: "...", data: null }
      const responseData = error?.response?.data ?? {};
      const errorMessage = responseData?.message ?? responseData?.error ?? '';
      message.error(errorMessage || msg);
    }),
  );

  return client;
}

export const requestClient = createRequestClient(apiURL, {
  responseReturn: 'data',
});

export const baseRequestClient = new RequestClient({
  baseURL: apiURL,
  withCredentials: true,
});
