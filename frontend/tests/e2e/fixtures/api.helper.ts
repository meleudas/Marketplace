import { getApiBaseUrl } from "./backend.helper";
import { testUsers } from "./users.fixture";

interface AuthTokensResponse {
  accessToken: string;
  refreshToken?: string;
  accessTokenExpiresAt?: string;
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
