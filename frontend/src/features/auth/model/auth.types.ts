import type { CurrentUser } from "@/shared/types/user.types";

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


export interface ForgotPasswordPayload {
  email: string;
}

export interface ResetPasswordPayload {
  email: string;
  token: string;
  newPassword: string;
}

export interface GoogleCallbackExchangePayload {
  code: string;
}

export interface GoogleCallbackResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

export interface AuthActionResult {
  success: boolean;
  message: string;
}

export interface LoginActionResult extends AuthActionResult {
  requiresTwoFactor?: boolean;
}

export interface AuthState {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  loading: boolean;
  initialized: boolean;
}

export interface AuthActions {
  register: (payload: RegisterPayload) => Promise<AuthActionResult>;
  login: (payload: LoginPayload) => Promise<LoginActionResult>;
  startGoogleLogin: () => void;
  completeGoogleLogin: (code: string) => Promise<AuthActionResult>;
  forgotPassword: (payload: ForgotPasswordPayload) => Promise<AuthActionResult>;
  resetPassword: (payload: ResetPasswordPayload) => Promise<AuthActionResult>;
  logout: () => Promise<AuthActionResult>;
  loadMe: () => Promise<void>;
}

export type AuthStore = AuthState & AuthActions;



