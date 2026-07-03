import {
  loginUserViaApi,
} from "./fixtures/api.helper";
import { getApiBaseUrl, isBackendAvailable } from "./fixtures/backend.helper";
import { testUsers } from "./fixtures/users.fixture";

export default async function globalSetup(): Promise<void> {
  const apiBaseUrl = getApiBaseUrl();
  const backendAvailable = await isBackendAvailable(apiBaseUrl);

  process.env.E2E_BACKEND_AVAILABLE = backendAvailable ? "true" : "false";

  if (!backendAvailable) {
    console.warn(
      `[E2E] Backend is not reachable at ${apiBaseUrl}. API-dependent tests will be skipped.`,
    );
    return;
  }

  try {
    await loginUserViaApi(testUsers.verified.email, testUsers.verified.password);
    process.env.E2E_VERIFIED_EMAIL = testUsers.verified.email;
    process.env.E2E_VERIFIED_PASSWORD = testUsers.verified.password;
    return;
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    if (message.includes("429")) {
      process.env.E2E_AUTH_RATE_LIMITED = "true";
      console.warn("[E2E] Auth API is rate-limited. API-dependent tests will be skipped.");
      return;
    }

    console.warn(
      `[E2E] Seed user ${testUsers.verified.email} is unavailable. ` +
        "Run: docker compose --profile tools run --rm db-seed",
    );
  }
}
