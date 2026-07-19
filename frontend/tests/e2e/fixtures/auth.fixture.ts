import { expect, test, type Page, type Response } from "@playwright/test";

const ACCESS_TOKEN_KEY = "accessToken";
const AUTH_POLL_TIMEOUT = 30_000;
const LOGIN_PATH = "/auth/login";
const AUTH_UI_GOTO_ATTEMPTS = 3;
const LOGIN_UI_ATTEMPTS = 2;

function authAlert(page: Page) {
  return page.locator("main [role='alert']");
}

async function gotoWithAbortRetry(page: Page, path: string): Promise<void> {
  let lastError: unknown;

  for (let attempt = 1; attempt <= AUTH_UI_GOTO_ATTEMPTS; attempt++) {
    try {
      // Prefer "load" so client components have time to hydrate before interactions.
      await page.goto(path, { waitUntil: "load" });
      return;
    } catch (error) {
      lastError = error;
      if (!isAbortedNavigationError(error) || attempt === AUTH_UI_GOTO_ATTEMPTS) {
        throw error;
      }

      // Abort races: retry with a lighter wait, then require hydrated UI separately.
      try {
        await page.goto(path, { waitUntil: "domcontentloaded" });
        return;
      } catch (retryError) {
        lastError = retryError;
        if (!isAbortedNavigationError(retryError) || attempt === AUTH_UI_GOTO_ATTEMPTS) {
          throw retryError;
        }
      }
    }
  }

  throw lastError;
}

export async function clearAuthState(page: Page): Promise<void> {
  await gotoWithAbortRetry(page, "/");
  await page.evaluate(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
  });
  await page.context().clearCookies();
}

async function getAccessToken(page: Page): Promise<string | null> {
  return page.evaluate((key) => window.localStorage.getItem(key), ACCESS_TOKEN_KEY);
}

export async function expectAccessTokenExists(page: Page): Promise<void> {
  await expect.poll(async () => getAccessToken(page), { timeout: AUTH_POLL_TIMEOUT }).not.toBeNull();
}

export async function expectAccessTokenMissing(page: Page): Promise<void> {
  await expect.poll(async () => getAccessToken(page), { timeout: AUTH_POLL_TIMEOUT }).toBeNull();
}

function isAbortedNavigationError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error);
  return /ERR_ABORTED|net::ERR_ABORTED/i.test(message);
}

/** Wait until the login client form is hydrated (RHF handlers attached). */
async function expectLoginFormHydrated(page: Page): Promise<void> {
  const emailInput = page.locator("#login-email");
  const submitButton = page.getByRole("button", { name: "Увійти", exact: true });

  await expect(page.getByRole("heading", { name: "Вхід" })).toBeVisible({
    timeout: AUTH_POLL_TIMEOUT,
  });
  await expect(emailInput).toBeVisible({ timeout: AUTH_POLL_TIMEOUT });
  await expect(emailInput).toBeEnabled();
  await expect(submitButton).toBeVisible();
  await expect(submitButton).toBeEnabled();
}

/**
 * Fill a controlled RHF input until the value sticks (guards remount races).
 */
async function fillStableInput(
  locator: ReturnType<Page["locator"]>,
  value: string,
): Promise<void> {
  await expect(async () => {
    await expect(locator).toBeEnabled();
    await locator.click({ timeout: 5_000 });
    await locator.fill("");
    await locator.fill(value);
    await expect(locator).toHaveValue(value);
  }).toPass({ timeout: 15_000 });
}

function isRetryableLoginError(error: unknown): boolean {
  if (isAbortedNavigationError(error)) {
    return true;
  }

  const message = error instanceof Error ? error.message : String(error);
  // Retry navigation/response waits and transient controlled-input fill races.
  return (
    (/waitForResponse|Timeout.*exceeded|page\.goto|toHaveValue|toPass/i.test(message) ||
      /unexpected value ""/i.test(message)) &&
    !/Login failed:/i.test(message)
  );
}

function isLoginPostResponse(response: Response): boolean {
  if (response.request().method() !== "POST") {
    return false;
  }

  try {
    return new URL(response.url()).pathname.endsWith("/auth/login");
  } catch {
    return /\/auth\/login(?:\?|$)/.test(response.url());
  }
}

export async function waitForAuthUi(page: Page): Promise<void> {
  let lastError: unknown;

  for (let attempt = 1; attempt <= AUTH_UI_GOTO_ATTEMPTS; attempt++) {
    try {
      await gotoWithAbortRetry(page, LOGIN_PATH);
      await expectLoginFormHydrated(page);
      return;
    } catch (error) {
      lastError = error;
      if (!isAbortedNavigationError(error) || attempt === AUTH_UI_GOTO_ATTEMPTS) {
        throw error;
      }
    }
  }

  throw lastError;
}

async function readLoginError(page: Page, status: number): Promise<string> {
  const errorMessage = authAlert(page);
  await errorMessage.waitFor({ state: "visible", timeout: 5_000 }).catch(() => undefined);
  return (await errorMessage.textContent()) ?? `HTTP ${status}`;
}

function isHomeUrl(url: URL): boolean {
  return url.pathname === "/";
}

async function attemptLoginViaUi(
  page: Page,
  credentials: { email: string; password: string },
): Promise<void> {
  await waitForAuthUi(page);

  const emailInput = page.locator("#login-email");
  const passwordInput = page.locator("#login-password");
  const submitButton = page.getByRole("button", { name: "Увійти", exact: true });

  await fillStableInput(emailInput, credentials.email);
  await fillStableInput(passwordInput, credentials.password);
  await expect(submitButton).toBeEnabled();

  const loginResponsePromise = page.waitForResponse(isLoginPostResponse, {
    timeout: AUTH_POLL_TIMEOUT,
  });

  await submitButton.click();

  const loginResponse = await loginResponsePromise;

  if (loginResponse.status() === 429) {
    test.skip(true, "Login endpoint is rate-limited (429).");
  }

  if (!loginResponse.ok()) {
    if (await page.getByRole("heading", { name: "Підтвердіть вхід" }).isVisible()) {
      throw new Error("Login requires 2FA; use the two-factor credentials helper.");
    }
    throw new Error(`Login failed: ${await readLoginError(page, loginResponse.status())}`);
  }

  await expectAccessTokenExists(page);
  if (!isHomeUrl(new URL(page.url()))) {
    await page.waitForURL(isHomeUrl, { timeout: AUTH_POLL_TIMEOUT });
  }
}

export async function loginViaUi(
  page: Page,
  credentials: { email: string; password: string },
): Promise<void> {
  let lastError: unknown;

  for (let attempt = 1; attempt <= LOGIN_UI_ATTEMPTS; attempt++) {
    try {
      await attemptLoginViaUi(page, credentials);
      return;
    } catch (error) {
      lastError = error;
      if (!isRetryableLoginError(error) || attempt === LOGIN_UI_ATTEMPTS) {
        throw error;
      }
    }
  }

  throw lastError;
}

/** Sets an API-issued access token and reloads so auth store bootstraps. */
export async function openAuthenticatedSession(page: Page, accessToken: string): Promise<void> {
  await gotoWithAbortRetry(page, "/");
  await page.evaluate(
    ({ key, token }) => {
      window.localStorage.setItem(key, token);
    },
    { key: ACCESS_TOKEN_KEY, token: accessToken },
  );
  await page.reload({ waitUntil: "load" });
  await expectAccessTokenExists(page);
}

export async function advanceForgotPasswordToStepTwo(page: Page, email: string): Promise<void> {
  await openForgotPasswordForm(page);
  const emailInput = page.locator("#forgot-email");
  await fillStableInput(emailInput, email);
  await page.getByRole("button", { name: "Надіслати код скидання" }).click();

  const successMessage = page.getByText(/Код для скидання пароля надіслано/i);
  const errorMessage = authAlert(page);

  await expect(successMessage.or(errorMessage)).toBeVisible({ timeout: 10_000 });

  if (await errorMessage.isVisible()) {
    throw new Error("Password reset request was rejected by the backend.");
  }
}

export async function logoutViaUi(page: Page): Promise<void> {
  await gotoWithAbortRetry(page, "/me");
  await expect(page.getByRole("heading", { name: "Персональні дані" })).toBeVisible({
    timeout: AUTH_POLL_TIMEOUT,
  });
  await page.getByRole("button", { name: "Вийти з профілю" }).click();
  await page.waitForURL(isHomeUrl);
  await expectGuest(page);
}

export async function expectGuest(page: Page): Promise<void> {
  await expectAccessTokenMissing(page);
}

export async function openRegisterForm(page: Page): Promise<void> {
  await waitForAuthUi(page);
  await page.getByRole("link", { name: "Створити" }).click();
  await expect(page).toHaveURL(/\/auth\/register/);
  await expect(page.getByRole("heading", { name: "Створіть акаунт" })).toBeVisible();
  await expect(page.locator("#register-email")).toBeEnabled();
}

export async function openForgotPasswordForm(page: Page): Promise<void> {
  await waitForAuthUi(page);
  await page.getByRole("link", { name: "Забули пароль?" }).click();
  await expect(page).toHaveURL(/\/auth\/forgot-password/);
  await expect(page.getByRole("heading", { name: "Відновлення пароля" })).toBeVisible();
  await expect(page.locator("#forgot-email")).toBeEnabled();
}

export async function submitRegisterForm(
  page: Page,
  data: { userName: string; email: string; password: string },
): Promise<void> {
  await openRegisterForm(page);
  await page.locator("#register-userName").fill(data.userName);
  await page.locator("#register-email").fill(data.email);
  await page.locator("#register-password").fill(data.password);
  await page.getByRole("button", { name: "Створити акаунт", exact: true }).click();
}

export async function submitLoginForm(
  page: Page,
  data: { email: string; password: string },
): Promise<void> {
  await waitForAuthUi(page);
  await fillStableInput(page.locator("#login-email"), data.email);
  await fillStableInput(page.locator("#login-password"), data.password);
  await page.getByRole("button", { name: "Увійти", exact: true }).click();
}

export function getAuthorizationHeader(requestHeaders: Record<string, string>): string | undefined {
  return requestHeaders.authorization ?? requestHeaders.Authorization;
}
