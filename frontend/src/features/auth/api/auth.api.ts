import { apiClient } from "@/shared/api/http.client";
import type { CurrentUser } from "@/shared/types/user.types";
import type {
  AuthTokensResponse,
  ForgotPasswordPayload,
  GoogleCallbackExchangePayload,
  GoogleCallbackResponse,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
} from "@/features/auth/model/auth.types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export const registerUser = async (payload: RegisterPayload): Promise<AuthTokensResponse> => {
  const response = await apiClient.post<AuthTokensResponse>("/auth/register", payload);
  return response.data;
};

export const loginUser = async (payload: LoginPayload): Promise<AuthTokensResponse> => {
  const response = await apiClient.post<AuthTokensResponse>("/auth/login", payload);
  return response.data;
};

export const refreshAuth = async (): Promise<AuthTokensResponse> => {
  const response = await apiClient.post<AuthTokensResponse>("/auth/refresh", null);
  return response.data;
};

export const logoutUser = async (): Promise<void> => {
  await apiClient.post("/auth/logout");
};

export const getCurrentUser = async (): Promise<CurrentUser> => {
  const response = await apiClient.get<CurrentUser>("/users/me");
  return response.data;
};

export const forgotPassword = async (payload: ForgotPasswordPayload): Promise<void> => {
  await apiClient.post("/account/forgot-password", payload);
};

export const resetPassword = async (payload: ResetPasswordPayload): Promise<void> => {
  await apiClient.post("/account/reset-password", payload);
};

export const buildGoogleAuthUrl = (returnPath = "/auth/callback"): string => {
  const query = new URLSearchParams({ returnPath });
  return `${API_BASE_URL}/auth/google?${query.toString()}`;
};

export const exchangeGoogleCallback = async (
  payload: GoogleCallbackExchangePayload,
): Promise<GoogleCallbackResponse> => {
  const response = await apiClient.post<GoogleCallbackResponse>("/auth/google/callback", payload);
  return response.data;
};


