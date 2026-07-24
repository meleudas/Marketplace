import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  loginViaUi,
  submitLoginForm,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { testUsers } from "../fixtures/users.fixture";

test.describe("Auth email 2FA login", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("user with email 2FA enabled enters valid email and password and sees 2FA step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await expect(page.locator("#login-two-factor-code")).toBeVisible();
    await expect(page.getByRole("button", { name: "Підтвердити і увійти" })).toBeVisible();
  });

  test("submitting 2FA step without code shows validation error", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await page.getByRole("button", { name: "Підтвердити і увійти" }).click();
    await expect(page.getByText("Потрібен код підтвердження")).toBeVisible();
  });

  test("invalid 2FA code shows error", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await page.locator("#login-two-factor-code").fill("000000");
    await page.getByRole("button", { name: "Підтвердити і увійти" }).click();
    await expect(page.locator("main [role='alert']")).toHaveText(/Invalid 2FA code/i);
  });

  test("Use different account returns to email and password step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await submitLoginForm(page, {
      email: testUsers.twoFactor.email,
      password: testUsers.twoFactor.password,
    });

    await page.getByRole("button", { name: "Використати інший акаунт" }).click();
    await expect(page.locator("#login-email")).toBeVisible();
    await expect(page.locator("#login-password")).toBeVisible();
    await expect(page.locator("#login-two-factor-code")).not.toBeVisible();
  });
});

test.describe("Auth email 2FA settings", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("guest opening /settings redirects to /", async ({ page }) => {
    await page.goto("/settings");
    await expect(page).toHaveURL(/\/($|\?)/);
  });

  test("verified user can request email 2FA code", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.goto("/settings");

    await page.getByRole("button", { name: /Enable email 2FA/i }).click();
    await expect(page.getByText(/Verification code was sent to your email/i)).toBeVisible();
    await expect(page.getByPlaceholder("Enter code from email")).toBeVisible();
  });
});
