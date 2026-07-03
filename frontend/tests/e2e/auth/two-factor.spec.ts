import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  expectAccessTokenExists,
  expectAccessTokenMissing,
  loginViaUiOrSkip,
  submitLoginForm,
  waitForAuthUi,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { getTwoFactorCode, isMailHelperConfigured } from "../fixtures/mail.helper";
import { isTwoFactorUserConfigured, testUsers } from "../fixtures/users.fixture";

test.describe("Auth email 2FA login", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("user with email 2FA enabled enters valid email and password and sees 2FA step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for 2FA login tests");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await expect(page.getByLabel("2FA Code")).toBeVisible();
    await expect(page.getByRole("button", { name: "Verify and login" })).toBeVisible();
  });

  test("submitting 2FA step without code shows validation error", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for 2FA login tests");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await page.getByRole("button", { name: "Verify and login" }).click();
    await expect(page.getByText("2FA code is required")).toBeVisible();
  });

  test("invalid 2FA code shows error", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for 2FA login tests");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await page.getByLabel("2FA Code").fill("000000");
    await page.getByRole("button", { name: "Verify and login" }).click();
    await expect(page.getByText(/Invalid 2FA code|invalid/i)).toBeVisible();
  });

  test("valid 2FA code completes login and redirects to /home", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for 2FA login tests");
    test.skip(!isMailHelperConfigured("2fa"), "Set E2E_2FA_CODE for valid 2FA login test");

    await waitForAuthUi(page);
    await page.getByLabel("Email").fill(testUsers.twoFactor.email);
    await page.getByLabel("Password").fill(testUsers.twoFactor.password);
    await page.getByRole("button", { name: "Login", exact: true }).click();

    await expect(page.getByLabel("2FA Code")).toBeVisible();
    const code = await getTwoFactorCode(testUsers.twoFactor.email);
    await page.getByLabel("2FA Code").fill(code);
    await page.getByRole("button", { name: "Verify and login" }).click();

    await expect(page).toHaveURL(/\/home$/);
    await expectAccessTokenExists(page);
  });

  test("Use different account returns to email and password step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for 2FA login tests");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await page.getByRole("button", { name: "Use different account" }).click();
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Password")).toBeVisible();
    await expect(page.getByLabel("2FA Code")).not.toBeVisible();
  });

  test("after successful 2FA login localStorage accessToken exists", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for 2FA login tests");
    test.skip(!isMailHelperConfigured("2fa"), "Set E2E_2FA_CODE for valid 2FA login test");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    const code = await getTwoFactorCode(testUsers.twoFactor.email);
    await page.getByLabel("2FA Code").fill(code);
    await page.getByRole("button", { name: "Verify and login" }).click();

    await expect(page).toHaveURL(/\/home$/);
    await expectAccessTokenExists(page);
  });
});

test.describe("Auth email 2FA settings", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("guest opening /settings redirects to /", async ({ page }) => {
    await page.goto("/settings");
    await expect(page).toHaveURL(/\/$/);
  });

  test("verified user can request email 2FA code", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUiOrSkip(page, credentials);
    await page.goto("/settings");

    await page.getByRole("button", { name: /Enable email 2FA/i }).click();
    await expect(page.getByText(/Verification code was sent to your email/i)).toBeVisible();
    await expect(page.getByPlaceholder("Enter code from email")).toBeVisible();
  });

  test("valid code enables email 2FA", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("2fa"), "Set E2E_2FA_CODE for settings enable test");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUiOrSkip(page, credentials);
    await page.goto("/settings");

    await page.getByRole("button", { name: /Enable email 2FA/i }).click();
    const code = await getTwoFactorCode(credentials.email);
    await page.getByPlaceholder("Enter code from email").fill(code);
    await page.getByRole("button", { name: "Enable", exact: true }).click();

    await expect(page.getByText("Email 2FA enabled.")).toBeVisible();
    await expect(page.getByText("Enabled")).toBeVisible();
  });

  test("user can disable email 2FA", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isTwoFactorUserConfigured(), "Set E2E_2FA_EMAIL for disable 2FA test");

    await loginViaUiOrSkip(page, testUsers.twoFactor);
    await page.goto("/settings");

    await page.getByRole("button", { name: /Disable email 2FA/i }).click();
    await expect(page.getByText("Email 2FA disabled.")).toBeVisible();
  });

  test("after enabling 2FA login requires 2FA step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("2fa"), "Set E2E_2FA_CODE for post-enable login test");
    test.skip(!process.env.E2E_2FA_ENABLE_EMAIL, "Set E2E_2FA_ENABLE_EMAIL for post-enable login test");
    test.skip(!process.env.E2E_2FA_ENABLE_PASSWORD, "Set E2E_2FA_ENABLE_PASSWORD for post-enable login test");

    const email = process.env.E2E_2FA_ENABLE_EMAIL!;
    const password = process.env.E2E_2FA_ENABLE_PASSWORD!;

    await loginViaUiOrSkip(page, { email, password });
    await page.goto("/settings");
    await page.getByRole("button", { name: /Enable email 2FA/i }).click();

    const code = await getTwoFactorCode(email);
    await page.getByPlaceholder("Enter code from email").fill(code);
    await page.getByRole("button", { name: "Enable", exact: true }).click();
    await expect(page.getByText("Email 2FA enabled.")).toBeVisible();

    await clearAuthState(page);
    await submitLoginForm(page, { email, password });
    await expect(page.getByLabel("2FA Code")).toBeVisible();
    await expectAccessTokenMissing(page);
  });
});
