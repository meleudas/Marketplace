import type { UserDto } from "@/shared/types/user.types";
import type {
  AuthTokensDto,
  ConfirmEmailRequest,
  ForgotPasswordRequest,
  GoogleCallbackExchangeRequest,
  GoogleCallbackExchangeResponse,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
} from "@/shared/types/api.types";

export type RegisterPayload = RegisterRequest;
export type LoginPayload = LoginRequest;
export type ConfirmEmailPayload = ConfirmEmailRequest;
export type ForgotPasswordPayload = ForgotPasswordRequest;
export type ResetPasswordPayload = ResetPasswordRequest;
export type AuthTokensResponse = AuthTokensDto;
export type GoogleCallbackExchangePayload = GoogleCallbackExchangeRequest;
export type GoogleCallbackResponse = GoogleCallbackExchangeResponse;

export interface AuthActionResult {
  success: boolean;
  message: string;
}

export interface LoginActionResult extends AuthActionResult {
  requiresTwoFactor?: boolean;
}

export interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  loading: boolean;
  initialized: boolean;
}

export interface AuthActions {
  register: (payload: RegisterPayload) => Promise<AuthActionResult>;
  confirmEmail: (payload: ConfirmEmailPayload) => Promise<AuthActionResult>;
  login: (payload: LoginPayload) => Promise<LoginActionResult>;
  startGoogleLogin: () => void;
  completeGoogleLogin: (code: string) => Promise<AuthActionResult>;
  forgotPassword: (payload: ForgotPasswordPayload) => Promise<AuthActionResult>;
  resetPassword: (payload: ResetPasswordPayload) => Promise<AuthActionResult>;
  logout: () => Promise<AuthActionResult>;
  loadMe: () => Promise<void>;
}

export type AuthStore = AuthState & AuthActions;



