import { AxiosError } from "axios";
import { apiClient } from "@/lib/api/client";
import type { CurrentUser } from "@/types/user";
import type {
  AuthTokensResponse,
  ForgotPasswordPayload,
  GoogleCallbackExchangePayload,
  GoogleCallbackResponse,
  LoginPayload,
  RefreshPayload,
  RegisterPayload,
  ResetPasswordPayload,
} from "@/types/auth";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export const registerUser = async (payload: RegisterPayload): Promise<AuthTokensResponse> => {
  try {
    const response = await apiClient.post<AuthTokensResponse>("/auth/register", payload);
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Register failed.", {
      endpoint: "/auth/register",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const loginUser = async (payload: LoginPayload): Promise<AuthTokensResponse> => {
  try {
    const response = await apiClient.post<AuthTokensResponse>("/auth/login", payload);
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Login failed.", {
      endpoint: "/auth/login",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const refreshAuth = async (payload: RefreshPayload): Promise<AuthTokensResponse> => {
  try {
    const response = await apiClient.post<AuthTokensResponse>("/auth/refresh", payload);
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Refresh failed.", {
      endpoint: "/auth/refresh",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const logoutUser = async (): Promise<void> => {
  try {
    await apiClient.post("/auth/logout");
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Logout failed.", {
      endpoint: "/auth/logout",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const getCurrentUser = async (): Promise<CurrentUser> => {
  try {
    const response = await apiClient.get<CurrentUser>("/users/me");
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[PROFILE] Load current user failed.", {
      endpoint: "/users/me",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const forgotPassword = async (payload: ForgotPasswordPayload): Promise<void> => {
  try {
    await apiClient.post("/account/forgot-password", payload);
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Forgot password failed.", {
      endpoint: "/account/forgot-password",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const resetPassword = async (payload: ResetPasswordPayload): Promise<void> => {
  try {
    await apiClient.post("/account/reset-password", payload);
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Reset password failed.", {
      endpoint: "/account/reset-password",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

export const buildGoogleAuthUrl = (returnPath = "/auth/callback"): string => {
  const query = new URLSearchParams({ returnPath });
  return `${API_BASE_URL}/auth/google?${query.toString()}`;
};

export const exchangeGoogleCallback = async (
  payload: GoogleCallbackExchangePayload,
): Promise<GoogleCallbackResponse> => {
  try {
    const response = await apiClient.post<GoogleCallbackResponse>("/auth/google/callback", payload);
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Google callback exchange failed.", {
      endpoint: "/auth/google/callback",
      status: axiosError.response?.status,
      message: axiosError.message,
    });
    throw error;
  }
};

