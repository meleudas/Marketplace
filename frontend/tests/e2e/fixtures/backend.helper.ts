export function getApiBaseUrl(): string {
  return process.env.PLAYWRIGHT_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";
}

export async function isBackendAvailable(apiBaseUrl = getApiBaseUrl()): Promise<boolean> {
  try {
    const response = await fetch(`${apiBaseUrl}/health`, {
      signal: AbortSignal.timeout(5_000),
    });

    return response.ok;
  } catch {
    return false;
  }
}

function isBackendAvailableFromEnv(): boolean {
  return process.env.E2E_BACKEND_AVAILABLE === "true";
}

function skipIfBackendUnavailable(): boolean {
  return !isBackendAvailableFromEnv();
}

function skipIfAuthRateLimited(): boolean {
  return process.env.E2E_AUTH_RATE_LIMITED === "true";
}

export function skipIfBackendAuthUnavailable(): boolean {
  return skipIfBackendUnavailable() || skipIfAuthRateLimited();
}
