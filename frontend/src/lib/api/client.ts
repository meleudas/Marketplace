import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { getAccessToken } from "@/lib/storage/token";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

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
  (error: AxiosError) => {
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

