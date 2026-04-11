import { AxiosError } from "axios";
import type { ProblemDetails } from "@/shared/types/api.types";

export const getWorkspaceErrorMessage = (error: unknown, fallback = "Request failed"): string => {
  const axiosError = error as AxiosError;
  const data = axiosError.response?.data as ProblemDetails | string | undefined;

  if (typeof data === "string" && data.trim()) {
    return data;
  }

  if (data && typeof data === "object") {
    if (typeof data.detail === "string" && data.detail.trim()) {
      return data.detail;
    }

    if (typeof data.title === "string" && data.title.trim()) {
      return data.title;
    }
  }

  if (axiosError.message) {
    return axiosError.message;
  }

  return fallback;
};

