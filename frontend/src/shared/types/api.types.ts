export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

export interface AuthTokensDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  userName: string;
  phoneNumber: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
  twoFactorCode?: string | null;
}

export interface RefreshRequest {
  refreshToken?: string | null;
}

export interface ConfirmEmailRequest {
  email: string;
  token: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface EnableEmailTwoFactorRequest {
  code: string;
}

export interface GoogleCallbackExchangeRequest {
  code: string;
}

export interface GoogleCallbackExchangeResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}
