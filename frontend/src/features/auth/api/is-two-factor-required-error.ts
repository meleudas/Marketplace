import { AxiosError } from "axios";

export const isTwoFactorRequiredError = (error: unknown): boolean => {
    const axiosError = error as AxiosError;
    if (axiosError.response?.status !== 401) {
        return false;
    }

    const data = axiosError.response.data;
    if (!data || typeof data !== "object") {
        return false;
    }

    const detail = (data as Record<string, unknown>).detail;
    if (typeof detail !== "string") {
        return false;
    }

    return detail.toLowerCase().includes("2fa code required");
};