import type { CurrentUser } from "@/types/user";

export interface RegisterPayload {
  email: string;
  password: string;
  userName: string;
  phoneNumber: string | null;
}

export interface AuthTokensResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface LoginPayload {
  email: string;
  password: string;
  rememberMe?: boolean;
  twoFactorCode?: string | null;
}

export interface RefreshPayload {
  refreshToken: string | null;
}

export interface ForgotPasswordPayload {
  email: string;
}

export interface ResetPasswordPayload {
  email: string;
  token: string;
  newPassword: string;
}

export interface AuthState {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  loading: boolean;
  initialized: boolean;
}

export interface AuthActions {
  register: (payload: RegisterPayload) => Promise<{ success: boolean; message: string }>;
  login: (payload: LoginPayload) => Promise<{ success: boolean; message: string }>;
  forgotPassword: (payload: ForgotPasswordPayload) => Promise<{ success: boolean; message: string }>;
  resetPassword: (payload: ResetPasswordPayload) => Promise<{ success: boolean; message: string }>;
  logout: () => Promise<{ success: boolean; message: string }>;
  loadMe: () => Promise<void>;
}

export type AuthStore = AuthState & AuthActions;

