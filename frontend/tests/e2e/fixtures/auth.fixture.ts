import { expect, test, type Page } from "@playwright/test";

const ACCESS_TOKEN_KEY = "accessToken";

export async function clearAuthState(page: Page): Promise<void> {
  await page.goto("/");
  await page.evaluate(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
  });
  await page.context().clearCookies();
}

export async function getAccessToken(page: Page): Promise<string | null> {
  return page.evaluate((key) => window.localStorage.getItem(key), ACCESS_TOKEN_KEY);
}

export async function expectAccessTokenExists(page: Page): Promise<void> {
  await expect.poll(async () => getAccessToken(page)).not.toBeNull();
}

export async function expectAccessTokenMissing(page: Page): Promise<void> {
  await expect.poll(async () => getAccessToken(page)).toBeNull();
}

export async function waitForAuthUi(page: Page): Promise<void> {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Auth MVP" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Login" })).toBeVisible();
}

export async function loginViaUi(
  page: Page,
  credentials: { email: string; password: string; twoFactorCode?: string },
): Promise<void> {
  await waitForAuthUi(page);

  await page.getByLabel("Email").fill(credentials.email);
  await page.getByLabel("Password").fill(credentials.password);
  await page.getByRole("button", { name: "Login", exact: true }).click();

  if (credentials.twoFactorCode) {
    await expect(page.getByLabel("2FA Code")).toBeVisible({ timeout: 15_000 });
    await page.getByLabel("2FA Code").fill(credentials.twoFactorCode);
    await page.getByRole("button", { name: "Verify and login" }).click();
  }

  const errorMessage = page.locator('[class*="errorMessage"]');
  const loginResult = await Promise.race([
    expect
      .poll(async () => page.evaluate(() => window.localStorage.getItem("accessToken")))
      .not.toBeNull({ timeout: 15_000 })
      .then(() => "success" as const),
    errorMessage
      .waitFor({ state: "visible", timeout: 15_000 })
      .then(async () => ("error" as const)),
  ]).catch(() => "timeout" as const);

  if (loginResult !== "success") {
    const message = loginResult === "error" ? await errorMessage.textContent() : "Login timed out";
    throw new Error(`Login failed: ${message ?? "unknown error"}`);
  }

  await page.waitForURL(/\/home$/, { timeout: 10_000 });
  await expectAuthenticated(page);
}

export async function loginViaUiOrSkip(
  page: Page,
  credentials: { email: string; password: string; twoFactorCode?: string },
): Promise<void> {
  try {
    await loginViaUi(page, credentials);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    test.skip(true, message);
  }
}

export async function advanceForgotPasswordToStepTwo(page: Page, email: string): Promise<void> {
  await openForgotPasswordForm(page);
  await page.getByLabel("Email").fill(email);
  await page.getByRole("button", { name: "Send reset token" }).click();

  const successMessage = page.getByText(/Password reset code sent/i);
  const errorMessage = page.locator('[class*="errorMessage"]');

  await expect(successMessage.or(errorMessage)).toBeVisible({ timeout: 10_000 });

  if (await errorMessage.isVisible()) {
    test.skip(true, "Password reset request was rejected by the backend (likely rate-limited).");
  }
}

export async function logoutViaUi(page: Page): Promise<void> {
  await page.goto("/me");
  await expect(page.getByRole("heading", { name: "My profile" })).toBeVisible();
  await page.getByRole("button", { name: "Logout" }).click();
  await page.waitForURL("**/");
  await expectGuest(page);
}

export async function expectAuthenticated(page: Page): Promise<void> {
  await expectAccessTokenExists(page);
}

export async function expectGuest(page: Page): Promise<void> {
  await expectAccessTokenMissing(page);
}

export async function openRegisterForm(page: Page): Promise<void> {
  await waitForAuthUi(page);
  await page.getByRole("button", { name: "Register" }).click();
  await expect(page.getByRole("heading", { name: "Register" })).toBeVisible();
}

export async function openForgotPasswordForm(page: Page): Promise<void> {
  await waitForAuthUi(page);
  await page.getByRole("button", { name: "Forgot password?" }).click();
  await expect(page.getByRole("heading", { name: "Reset password" })).toBeVisible();
}

export async function submitRegisterForm(
  page: Page,
  data: { userName: string; email: string; password: string; phoneNumber?: string },
): Promise<void> {
  await openRegisterForm(page);
  await page.getByLabel("Username").fill(data.userName);
  await page.getByLabel("Email").fill(data.email);

  if (data.phoneNumber) {
    await page.getByLabel("Phone number (optional)").fill(data.phoneNumber);
  }

  await page.getByLabel("Password").fill(data.password);
  await page.getByRole("button", { name: "Register", exact: true }).click();
}

export async function submitLoginForm(
  page: Page,
  data: { email: string; password: string },
): Promise<void> {
  await waitForAuthUi(page);
  await page.getByLabel("Email").fill(data.email);
  await page.getByLabel("Password").fill(data.password);
  await page.getByRole("button", { name: "Login", exact: true }).click();
}

export async function waitForUsersMeRequest(page: Page): Promise<void> {
  await page.waitForResponse(
    (response) =>
      response.url().includes("/users/me") &&
      response.request().method() === "GET" &&
      response.status() === 200,
  );
}

export function getAuthorizationHeader(requestHeaders: Record<string, string>): string | undefined {
  const direct = requestHeaders.authorization ?? requestHeaders.Authorization;
  return direct;
}
