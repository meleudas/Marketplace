"use client";

import { AxiosError } from "axios";
import { create } from "zustand";
import {
  buildGoogleAuthUrl,
  confirmEmail as confirmEmailRequest,
  exchangeGoogleCallback,
  forgotPassword,
  getCurrentUser,
  loginUser,
  logoutUser,
  refreshAuth,
  registerUser,
  resetPassword,
} from "@/features/auth/api/auth.api";
import { isTwoFactorRequiredError } from "@/features/auth/api/is-two-factor-required-error";
import { clearAccessToken, getAccessToken, setAccessToken } from "@/shared/lib/token.storage";
import type { ProblemDetails } from "@/shared/types/api.types";
import type {
  AuthStore,
  ConfirmEmailPayload,
  ForgotPasswordPayload,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
} from "@/features/auth/model/auth.types";

const getErrorMessage = (error: unknown): string => {
  const axiosError = error as AxiosError;
  const data = axiosError.response?.data as ProblemDetails | string | undefined;

  if (!axiosError.response) {
    return "Network error: backend is unavailable or blocked by CORS/proxy settings.";
  }

  if (typeof data === "string") {
    return data;
  }

  if (data && typeof data === "object") {
    if (typeof data.detail === "string") {
      return data.detail;
    }

    if (typeof data.title === "string") {
      return data.title;
    }
  }

  return axiosError.message || "Unknown error";
};

const isRegistrationConfirmationRequired = (error: unknown): boolean => {
  const axiosError = error as AxiosError;
  const message = getErrorMessage(error).toLowerCase();

  return axiosError.response?.status === 403 && message.includes("confirm your email");
};

export const useAuth = create<AuthStore>((set, get) => ({
  user: null,
  isAuthenticated: false,
  loading: false,
  initialized: false,

  register: async (payload: RegisterPayload) => {
    set({ loading: true });
    try {
      await registerUser(payload);
      clearAccessToken();
      set({ user: null, isAuthenticated: false });
      return {
        success: true,
        message: "Підтвердіть пошту, щоб завершити створення акаунта.",
      };
    } catch (error) {
      if (isRegistrationConfirmationRequired(error)) {
        clearAccessToken();
        set({ user: null, isAuthenticated: false });
        return {
          success: true,
          message: "Підтвердіть пошту, щоб завершити створення акаунта.",
        };
      }

      const message = getErrorMessage(error);
      clearAccessToken();
      set({ user: null, isAuthenticated: false });
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  confirmEmail: async (payload: ConfirmEmailPayload) => {
    set({ loading: true });
    try {
      await confirmEmailRequest(payload);
      return {
        success: true,
        message: "Email confirmed. Now you can login.",
      };
    } catch (error) {
      const message = getErrorMessage(error);
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  login: async (payload: LoginPayload) => {
    set({ loading: true });
    try {
      const loginResult = await loginUser(payload);
      setAccessToken(loginResult.accessToken);

      const user = await getCurrentUser();
      set({
        user,
        isAuthenticated: true,
      });

      return { success: true, message: "Login successful." };
    } catch (error) {
      const message = getErrorMessage(error);

      if (isTwoFactorRequiredError(error)) {
        return {
          success: false,
          message,
          requiresTwoFactor: true,
        };
      }

      clearAccessToken();
      set({ user: null, isAuthenticated: false });
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  startGoogleLogin: () => {
    if (typeof window === "undefined") {
      return;
    }

    const redirectUrl = buildGoogleAuthUrl("/auth/callback");
    window.location.assign(redirectUrl);
  },

  completeGoogleLogin: async (code: string) => {
    set({ loading: true });
    try {
      const result = await exchangeGoogleCallback({ code });
      setAccessToken(result.accessToken);

      const user = await getCurrentUser();
      set({
        user,
        isAuthenticated: true,
      });

      return { success: true, message: "Google login successful." };
    } catch (error) {
      const message = getErrorMessage(error);
      clearAccessToken();
      set({ user: null, isAuthenticated: false });
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  forgotPassword: async (payload: ForgotPasswordPayload) => {
    set({ loading: true });
    try {
      await forgotPassword(payload);
      return {
        success: true,
        message: "Password reset code sent. Check your email.",
      };
    } catch (error) {
      const message = getErrorMessage(error);
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  resetPassword: async (payload: ResetPasswordPayload) => {
    set({ loading: true });
    try {
      await resetPassword(payload);
      return {
        success: true,
        message: "Password has been reset. You can login now.",
      };
    } catch (error) {
      const message = getErrorMessage(error);
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  logout: async () => {
    set({ loading: true });
    try {
      await logoutUser();
    } catch {
      // Keep logout behavior unchanged even if backend request fails.
    } finally {
      clearAccessToken();
      set({
        user: null,
        isAuthenticated: false,
        loading: false,
      });
    }

    return { success: true, message: "Logged out." };
  },

  loadMe: async () => {
    if (get().initialized) {
      return;
    }

    set({ loading: true });

    try {
      const token = getAccessToken();
      if (!token) {
        set({ user: null, isAuthenticated: false, initialized: true });
        return;
      }

      const user = await getCurrentUser();
      set({
        user,
        isAuthenticated: true,
        initialized: true,
      });
    } catch (error) {
      const axiosError = error as AxiosError;

      if (axiosError.response?.status === 401 && !axiosError.config?.url?.includes("/auth/refresh")) {
        try {
          const refreshed = await refreshAuth();
          setAccessToken(refreshed.accessToken);

          const user = await getCurrentUser();
          set({
            user,
            isAuthenticated: true,
            initialized: true,
          });
          return;
        } catch {
          // Refresh failure is already logged by API client interceptor.
        }
      }

      clearAccessToken();
      set({ user: null, isAuthenticated: false, initialized: true });
    } finally {
      set({ loading: false });
    }
  },
}));



