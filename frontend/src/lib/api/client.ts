import axios, { AxiosError, AxiosHeaders, InternalAxiosRequestConfig } from "axios";
import { clearAccessToken, getAccessToken, setAccessToken } from "@/lib/storage/token";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

interface RefreshTokensResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

let refreshPromise: Promise<string> | null = null;

const shouldSkipAutoRefresh = (config: InternalAxiosRequestConfig): boolean => {
  const url = config.url ?? "";

  return ["/auth/login", "/auth/register", "/auth/refresh", "/auth/logout"].some((path) =>
    url.includes(path),
  );
};

const refreshAccessTokenWithCookie = async (): Promise<string> => {
  const response = await axios.post<RefreshTokensResponse>(`${BASE_URL}/auth/refresh`, null, {
    withCredentials: true,
    timeout: 15000,
    headers: {
      "Content-Type": "application/json",
    },
  });

  setAccessToken(response.data.accessToken);
  return response.data.accessToken;
};

const formatUrl = (config: InternalAxiosRequestConfig): string => {
  const baseURL = config.baseURL ?? "";
  const url = config.url ?? "";

  if (url.startsWith("http://") || url.startsWith("https://")) {
    return url;
  }

  return `${baseURL}${url}`;
};

export const apiClient = axios.create({
  baseURL: BASE_URL,
  withCredentials: true,
  timeout: 15000,
  headers: {
    "Content-Type": "application/json",
  },
});

apiClient.interceptors.request.use(
  (config) => {
    const token = getAccessToken();

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error: AxiosError) => {
    console.error("[API][REQUEST] Request interceptor error.", {
      message: error.message,
      response: error.response?.data,
    });
    return Promise.reject(error);
  },
);

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !shouldSkipAutoRefresh(originalRequest)
    ) {
      originalRequest._retry = true;

      try {
        if (!refreshPromise) {
          refreshPromise = refreshAccessTokenWithCookie().finally(() => {
            refreshPromise = null;
          });
        }

        const newAccessToken = await refreshPromise;

        const headers = AxiosHeaders.from(originalRequest.headers);
        headers.set("Authorization", `Bearer ${newAccessToken}`);
        originalRequest.headers = headers;

        return apiClient(originalRequest);
      } catch (refreshError) {
        clearAccessToken();
        return Promise.reject(refreshError);
      }
    }

    console.error("[API][RESPONSE] Request failed.", {
      method: error.config?.method?.toUpperCase(),
      endpoint: error.config ? formatUrl(error.config) : "unknown",
      status: error.response?.status,
      code: error.code,
      message: error.message,
    });


    return Promise.reject(error);
  },
);

