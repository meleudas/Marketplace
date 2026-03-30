import { AxiosError } from "axios";
import { apiClient } from "@/lib/api/client";
import type { CurrentUser } from "@/types/user";
import type { AuthTokensResponse, LoginPayload, RefreshPayload, RegisterPayload } from "@/types/auth";

export const registerUser = async (payload: RegisterPayload): Promise<AuthTokensResponse> => {
  console.log("[AUTH] Register started.", {
    endpoint: "/auth/register",
    payload,
  });

  try {
    const response = await apiClient.post<AuthTokensResponse>("/auth/register", payload);
    console.log("[AUTH] Register completed.", {
      endpoint: "/auth/register",
      response: response.data,
    });
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Register failed.", {
      endpoint: "/auth/register",
      payload,
      status: axiosError.response?.status,
      errorData: axiosError.response?.data,
      message: axiosError.message,
    });
    throw error;
  }
};

export const loginUser = async (payload: LoginPayload): Promise<AuthTokensResponse> => {
  console.log("[AUTH] Login started.", {
    endpoint: "/auth/login",
    payload,
  });

  try {
    const response = await apiClient.post<AuthTokensResponse>("/auth/login", payload);

    console.log("[AUTH] Login completed.", {
      endpoint: "/auth/login",
      response: response.data,
      tokenFound: Boolean(response.data.accessToken),
    });

    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Login failed.", {
      endpoint: "/auth/login",
      payload,
      status: axiosError.response?.status,
      errorData: axiosError.response?.data,
      message: axiosError.message,
    });
    throw error;
  }
};

export const refreshAuth = async (payload: RefreshPayload): Promise<AuthTokensResponse> => {
  console.log("[AUTH] Refresh started.", {
    endpoint: "/auth/refresh",
    payload,
  });

  try {
    const response = await apiClient.post<AuthTokensResponse>("/auth/refresh", payload);

    console.log("[AUTH] Refresh completed.", {
      endpoint: "/auth/refresh",
      response: response.data,
      tokenFound: Boolean(response.data.accessToken),
    });

    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Refresh failed.", {
      endpoint: "/auth/refresh",
      payload,
      status: axiosError.response?.status,
      errorData: axiosError.response?.data,
      message: axiosError.message,
    });
    throw error;
  }
};

export const logoutUser = async (): Promise<void> => {
  console.log("[AUTH] Logout started.", {
    endpoint: "/auth/logout",
  });

  try {
    await apiClient.post("/auth/logout");
    console.log("[AUTH] Logout completed.", {
      endpoint: "/auth/logout",
    });
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[AUTH] Logout failed.", {
      endpoint: "/auth/logout",
      status: axiosError.response?.status,
      errorData: axiosError.response?.data,
      message: axiosError.message,
    });
    throw error;
  }
};

export const getCurrentUser = async (): Promise<CurrentUser> => {
  console.log("[PROFILE] Load current user started.", {
    endpoint: "/users/me",
  });

  try {
    const response = await apiClient.get<CurrentUser>("/users/me");

    console.log("[PROFILE] Current user loaded.", {
      endpoint: "/users/me",
      response: response.data,
    });

    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError;
    console.error("[PROFILE] Load current user failed.", {
      endpoint: "/users/me",
      status: axiosError.response?.status,
      errorData: axiosError.response?.data,
      message: axiosError.message,
    });
    throw error;
  }
};

