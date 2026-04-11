import { AxiosError } from "axios";
import type { FieldValues, Path, UseFormSetError } from "react-hook-form";

interface ProblemDetailsLike {
  title?: unknown;
  detail?: unknown;
  errors?: unknown;
}

interface AdminFormErrorResult {
  message: string;
  fieldErrors: Record<string, string>;
}

export const parseAdminFormError = (error: unknown, fallbackMessage: string): AdminFormErrorResult => {
  const axiosError = error as AxiosError;
  const data = axiosError.response?.data;

  if (typeof data === "string") {
    return {
      message: data,
      fieldErrors: {},
    };
  }

  if (data && typeof data === "object") {
    const problem = data as ProblemDetailsLike;
    const fieldErrors: Record<string, string> = {};

    if (problem.errors && typeof problem.errors === "object") {
      for (const [key, value] of Object.entries(problem.errors as Record<string, unknown>)) {
        if (Array.isArray(value) && typeof value[0] === "string") {
          fieldErrors[key] = value[0];
          continue;
        }

        if (typeof value === "string") {
          fieldErrors[key] = value;
        }
      }
    }

    if (typeof problem.detail === "string" && problem.detail.trim()) {
      return { message: problem.detail, fieldErrors };
    }

    if (typeof problem.title === "string" && problem.title.trim()) {
      return { message: problem.title, fieldErrors };
    }

    if (Object.keys(fieldErrors).length > 0) {
      return { message: fallbackMessage, fieldErrors };
    }
  }

  return {
    message: axiosError.message || fallbackMessage,
    fieldErrors: {},
  };
};

export const applyServerFieldErrors = <TFieldValues extends FieldValues>(
  setError: UseFormSetError<TFieldValues>,
  fieldErrors: Record<string, string>,
) => {
  const normalizeFieldPath = (rawKey: string): Path<TFieldValues> => {
    const normalized = rawKey
      .replace(/\[(\d+)]/g, ".$1")
      .split(".")
      .map((segment) => {
        if (!segment) {
          return segment;
        }

        return segment.charAt(0).toLowerCase() + segment.slice(1);
      })
      .join(".");

    return normalized as Path<TFieldValues>;
  };

  for (const [key, value] of Object.entries(fieldErrors)) {
    setError(normalizeFieldPath(key), {
      type: "server",
      message: value,
    });
  }
};

