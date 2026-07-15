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
import { addCartItem } from "@/features/cart/api/cart.api";
import { clearGuestCart, getGuestCart, setGuestCart } from "@/features/cart/lib/guest-cart.storage";
import { useCartStore } from "@/features/cart/model/cart.store";
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
    return "Помилка мережі: бекенд недоступний або заблокований через CORS/проксі.";
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

  return axiosError.message || "Невідома помилка";
};

const mergeGuestCartIntoBackend = async (): Promise<void> => {
  const guestItems = getGuestCart();
  if (guestItems.length === 0) {
    return;
  }

  const remaining = [];
  for (const item of guestItems) {
    try {
      await addCartItem({ productId: item.productId, quantity: item.quantity });
    } catch {
      remaining.push(item);
    }
  }

  if (remaining.length > 0) {
    setGuestCart(remaining);
  } else {
    clearGuestCart();
  }

  await useCartStore.getState().loadCart();
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
        message: "Підтвердьте пошту, щоб завершити створення акаунта.",
      };
    } catch (error) {
      if (isRegistrationConfirmationRequired(error)) {
        clearAccessToken();
        set({ user: null, isAuthenticated: false });
        return {
          success: true,
          message: "Підтвердьте пошту, щоб завершити створення акаунта.",
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
        message: "Пошту підтверджено. Тепер можна увійти.",
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
      await mergeGuestCartIntoBackend();

      return { success: true, message: "Вхід виконано успішно." };
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
      await mergeGuestCartIntoBackend();

      return { success: true, message: "Вхід через Google виконано успішно." };
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
        message: "Код для скидання пароля надіслано. Перевірте пошту.",
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
        message: "Пароль скинуто. Тепер можна увійти.",
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

    return { success: true, message: "Вихід виконано." };
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



