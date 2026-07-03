import { getApiBaseUrl } from "./backend.helper";
import { createUniqueTestEmail, createUniqueUsername, defaultTestPassword, testUsers } from "./users.fixture";

export interface RegisterViaApiPayload {
  email?: string;
  password?: string;
  userName?: string;
  phoneNumber?: string | null;
}

export interface AuthTokensResponse {
  accessToken: string;
  refreshToken?: string;
  accessTokenExpiresAt?: string;
}

export async function registerUserViaApi(payload: RegisterViaApiPayload = {}): Promise<{
  email: string;
  password: string;
  userName: string;
}> {
  const email = payload.email ?? createUniqueTestEmail("register");
  const password = payload.password ?? defaultTestPassword;
  const userName = payload.userName ?? createUniqueUsername();

  const response = await fetch(`${getApiBaseUrl()}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      email,
      password,
      userName,
      phoneNumber: payload.phoneNumber ?? null,
    }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`registerUserViaApi failed (${response.status}): ${body}`);
  }

  return { email, password, userName };
}

export async function loginUserViaApi(email: string, password: string): Promise<AuthTokensResponse> {
  const response = await fetch(`${getApiBaseUrl()}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({
      email,
      password,
      rememberMe: false,
      twoFactorCode: null,
    }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`loginUserViaApi failed (${response.status}): ${body}`);
  }

  return (await response.json()) as AuthTokensResponse;
}

export async function confirmEmailViaApi(email: string, token: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/account/confirm-email`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, token }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`confirmEmailViaApi failed (${response.status}): ${body}`);
  }
}

export async function requestPasswordResetViaApi(email: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/account/forgot-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`requestPasswordResetViaApi failed (${response.status}): ${body}`);
  }
}

export async function resetPasswordViaApi(
  email: string,
  token: string,
  newPassword: string,
): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/account/reset-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, token, newPassword }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`resetPasswordViaApi failed (${response.status}): ${body}`);
  }
}

let cachedVerifiedCredentials: { email: string; password: string } | null = null;

/**
 * Returns verified credentials prepared in global setup or configured through env vars.
 */
export async function getVerifiedTestCredentials(): Promise<{ email: string; password: string }> {
  if (cachedVerifiedCredentials) {
    return cachedVerifiedCredentials;
  }

  cachedVerifiedCredentials = {
    email: testUsers.verified.email,
    password: testUsers.verified.password,
  };

  return cachedVerifiedCredentials;
}

export async function getAdminTestCredentials(): Promise<{ email: string; password: string }> {
  try {
    await loginUserViaApi(testUsers.admin.email, testUsers.admin.password);
    return {
      email: testUsers.admin.email,
      password: testUsers.admin.password,
    };
  } catch {
    throw new Error(
      "Admin seed user is unavailable. Seed the database or set E2E_ADMIN_EMAIL/E2E_ADMIN_PASSWORD.",
    );
  }
}
