import { AxiosError } from "axios";

const readLowerText = (data: unknown, field: string): string => {
  if (!data || typeof data !== "object") {
    return "";
  }

  const value = (data as Record<string, unknown>)[field];
  return typeof value === "string" ? value.toLowerCase() : "";
};

export const isTwoFactorRequiredError = (error: unknown): boolean => {
  const axiosError = error as AxiosError;
  const status = axiosError.response?.status;

  if (status !== 401) {
    return false;
  }

  const data = axiosError.response?.data;
  const message = readLowerText(data, "message");
  const code = readLowerText(data, "code");
  const errorText = readLowerText(data, "error");
  const detail = readLowerText(data, "detail");
  const title = readLowerText(data, "title");

  return [message, code, errorText, detail, title].some(
    (value) => value.includes("2fa") && value.includes("required"),
  );
};

