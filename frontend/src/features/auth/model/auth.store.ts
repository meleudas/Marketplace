"use client";

import { AxiosError } from "axios";
import { create } from "zustand";
import {
  buildGoogleAuthUrl,
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
import type {
  AuthStore,
  ForgotPasswordPayload,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
} from "@/features/auth/model/auth.types";

const getErrorMessage = (error: unknown): string => {
  const axiosError = error as AxiosError;
  const data = axiosError.response?.data;

  if (!axiosError.response) {
    return "Network error: backend is unavailable or blocked by CORS/proxy settings.";
  }

  if (typeof data === "string") {
    return data;
  }

  if (data && typeof data === "object") {
    const problemDetail = (data as Record<string, unknown>).detail;
    if (typeof problemDetail === "string") {
      return problemDetail;
    }

    const legacyMessage = (data as Record<string, unknown>).message;
    if (typeof legacyMessage === "string") {
      return legacyMessage;
    }
  }

  return axiosError.message || "Unknown error";
};

export const useAuth = create<AuthStore>((set, get) => ({
  user: null,
  isAuthenticated: false,
  loading: false,
  initialized: false,

  register: async (payload: RegisterPayload) => {
    set({ loading: true });
    try {
      const registerResult = await registerUser(payload);
      setAccessToken(registerResult.accessToken);

      const user = await getCurrentUser();
      set({
        user,
        isAuthenticated: true,
      });

      return { success: true, message: "Registration successful." };
    } catch (error) {
      const message = getErrorMessage(error);
      clearAccessToken();
      set({ user: null, isAuthenticated: false });
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



