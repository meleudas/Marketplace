import { expect, type Page } from "@playwright/test";

const ACCESS_TOKEN_KEY = "accessToken";
const AUTH_POLL_TIMEOUT = 30_000;
const LOGIN_PATH = "/auth/login";

function authAlert(page: Page) {
  return page.locator("main [role='alert']");
}

export async function clearAuthState(page: Page): Promise<void> {
  await page.goto("/");
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

export async function waitForAuthUi(page: Page): Promise<void> {
  await page.goto(LOGIN_PATH);
  await expect(page.getByRole("heading", { name: "Вхід" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Увійти", exact: true })).toBeVisible();
}

async function readLoginError(page: Page, status: number): Promise<string> {
  const errorMessage = authAlert(page);
  await errorMessage.waitFor({ state: "visible", timeout: 5_000 }).catch(() => undefined);
  return (await errorMessage.textContent()) ?? `HTTP ${status}`;
}

function isHomeUrl(url: URL): boolean {
  return url.pathname === "/";
}

export async function loginViaUi(
  page: Page,
  credentials: { email: string; password: string },
): Promise<void> {
  await waitForAuthUi(page);

  const emailInput = page.locator("#login-email");
  const passwordInput = page.locator("#login-password");

  await emailInput.fill(credentials.email);
  await passwordInput.fill(credentials.password);
  await expect(emailInput).toHaveValue(credentials.email);
  await expect(passwordInput).toHaveValue(credentials.password);

  const loginResponsePromise = page.waitForResponse(
    (response) =>
      response.url().includes("/auth/login") && response.request().method() === "POST",
    { timeout: AUTH_POLL_TIMEOUT },
  );

  await page.getByRole("button", { name: "Увійти", exact: true }).click();

  const loginResponse = await loginResponsePromise;
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

export async function advanceForgotPasswordToStepTwo(page: Page, email: string): Promise<void> {
  await openForgotPasswordForm(page);
  await page.locator("#forgot-email").fill(email);
  await page.getByRole("button", { name: "Надіслати код скидання" }).click();

  const successMessage = page.getByText(/Код для скидання пароля надіслано/i);
  const errorMessage = authAlert(page);

  await expect(successMessage.or(errorMessage)).toBeVisible({ timeout: 10_000 });

  if (await errorMessage.isVisible()) {
    throw new Error("Password reset request was rejected by the backend.");
  }
}

export async function logoutViaUi(page: Page): Promise<void> {
  await page.goto("/me");
  await expect(page.getByRole("heading", { name: "Персональні дані" })).toBeVisible();
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
}

export async function openForgotPasswordForm(page: Page): Promise<void> {
  await waitForAuthUi(page);
  await page.getByRole("link", { name: "Забули пароль?" }).click();
  await expect(page).toHaveURL(/\/auth\/forgot-password/);
  await expect(page.getByRole("heading", { name: "Відновлення пароля" })).toBeVisible();
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
  await page.locator("#login-email").fill(data.email);
  await page.locator("#login-password").fill(data.password);
  await page.getByRole("button", { name: "Увійти", exact: true }).click();
}

export function getAuthorizationHeader(requestHeaders: Record<string, string>): string | undefined {
  return requestHeaders.authorization ?? requestHeaders.Authorization;
}
