import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { getAccessToken } from "@/lib/storage/token";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

console.log("[API] Axios client initialized.", {
  baseURL: BASE_URL,
  withCredentials: true,
});

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
    } else {
      console.warn("[API][REQUEST] No access token available for request.", {
        method: config.method?.toUpperCase(),
        endpoint: formatUrl(config),
      });
    }

    console.log("[API][REQUEST] Request started.", {
      method: config.method?.toUpperCase(),
      endpoint: formatUrl(config),
      payload: config.data,
      withCredentials: config.withCredentials,
    });

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
  (response) => {
    console.log("[API][RESPONSE] Response received.", {
      method: response.config.method?.toUpperCase(),
      endpoint: formatUrl(response.config),
      status: response.status,
      data: response.data,
    });

    return response;
  },
  (error: AxiosError) => {
    const isNetworkError = !error.response;

    console.error("[API][RESPONSE] Request failed.", {
      method: error.config?.method?.toUpperCase(),
      endpoint: error.config ? formatUrl(error.config) : "unknown",
      status: error.response?.status,
      errorData: error.response?.data,
      code: error.code,
      isNetworkError,
      message: error.message,
    });

    if (isNetworkError) {
      console.error("[API][RESPONSE] Network/CORS level failure detected (no HTTP response).", {
        hint: "Check backend availability, NEXT_PUBLIC_API_URL, proxy rewrite, and browser CORS/preflight errors.",
      });
    }

    return Promise.reject(error);
  },
);

