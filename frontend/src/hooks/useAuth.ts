"use client";

import { AxiosError } from "axios";
import { create } from "zustand";
import {
  getCurrentUser,
  loginUser,
  logoutUser,
  refreshAuth,
  registerUser,
} from "@/lib/api/auth";
import { clearAccessToken, getAccessToken, setAccessToken } from "@/lib/storage/token";
import type { AuthStore, LoginPayload, RegisterPayload } from "@/types/auth";

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
    const maybeMessage = (data as Record<string, unknown>).message;
    if (typeof maybeMessage === "string") {
      return maybeMessage;
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
    console.log("[AUTH] register() action started.", { payload });

    set({ loading: true });
    try {
      await registerUser(payload);
      console.log("[AUTH] register() action completed.");
      return { success: true, message: "Registration successful. You can now login." };
    } catch (error) {
      const message = getErrorMessage(error);
      console.error("[AUTH] register() action failed.", { message, error });
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  login: async (payload: LoginPayload) => {
    console.log("[AUTH] login() action started.", { payload });

    set({ loading: true });
    try {
      const loginResult = await loginUser(payload);
      if (loginResult.token) {
        setAccessToken(loginResult.token);
      } else {
        console.warn("[AUTH] login() completed without access token. Trying refresh endpoint.");
        const refreshResult = await refreshAuth({ refreshToken: null });
        if (refreshResult.token) {
          setAccessToken(refreshResult.token);
        } else {
          console.warn("[AUTH] refresh did not return access token. Proceeding with /users/me only.");
        }
      }

      const user = await getCurrentUser();
      set({
        user,
        isAuthenticated: Boolean(user),
      });

      console.log("[AUTH] login() action completed.", {
        isAuthenticated: Boolean(user),
        user,
      });

      if (!user) {
        return {
          success: false,
          message: "Login succeeded but user mapping failed. Check mapCurrentUser().",
        };
      }

      return { success: true, message: "Login successful." };
    } catch (error) {
      const message = getErrorMessage(error);
      console.error("[AUTH] login() action failed.", { message, error });
      clearAccessToken();
      set({ user: null, isAuthenticated: false });
      return { success: false, message };
    } finally {
      set({ loading: false });
    }
  },

  logout: async () => {
    console.log("[AUTH] logout() action started.");

    set({ loading: true });
    try {
      await logoutUser();
      console.log("[AUTH] logout() backend call completed.");
    } catch (error) {
      const message = getErrorMessage(error);
      console.warn("[AUTH] logout() backend call failed, still clearing local auth state.", {
        message,
        error,
      });
    } finally {
      clearAccessToken();
      set({
        user: null,
        isAuthenticated: false,
        loading: false,
      });
      console.log("[AUTH] logout() cleared localStorage and auth state.");
    }

    return { success: true, message: "Logged out." };
  },

  loadMe: async () => {
    if (get().initialized) {
      console.log("[AUTH] loadMe() skipped because auth is already initialized.");
      return;
    }

    console.log("[AUTH] loadMe() started on app bootstrap.");
    set({ loading: true });

    try {
      const token = getAccessToken();
      if (!token) {
        console.warn("[AUTH] loadMe() found no token. User is not authenticated.");
        set({ user: null, isAuthenticated: false, initialized: true });
        return;
      }

      const user = await getCurrentUser();
      set({
        user,
        isAuthenticated: Boolean(user),
        initialized: true,
      });

      console.log("[AUTH] loadMe() finished.", {
        isAuthenticated: Boolean(user),
        user,
      });
    } catch (error) {
      const message = getErrorMessage(error);
      console.error("[AUTH] loadMe() failed.", { message, error });
      clearAccessToken();
      set({ user: null, isAuthenticated: false, initialized: true });
    } finally {
      set({ loading: false });
    }
  },
}));

