import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  expectAccessTokenExists,
  loginViaUi,
  waitForAuthUi,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";

test.describe("Auth login", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("guest can open /auth/login and see the auth UI", async ({ page }) => {
    await page.goto("/auth/login");
    await expect(page.getByRole("heading", { name: "Ласкаво просимо назад" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Увійти", exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Продовжити через Google" })).toBeVisible();
  });

  test("guest can open /auth and is redirected to /auth/login", async ({ page }) => {
    await page.goto("/auth");
    await expect(page).toHaveURL(/\/auth\/login/);
    await expect(page.getByRole("heading", { name: "Ласкаво просимо назад" })).toBeVisible();
  });

  test("login form shows validation errors for empty email and password", async ({ page }) => {
    await waitForAuthUi(page);
    await page.getByRole("button", { name: "Увійти", exact: true }).click();

    await expect(page.getByText("Введіть дійсну електронну адресу")).toBeVisible();
    await expect(page.getByText("Пароль є обов'язковим")).toBeVisible();
  });

  test("login form shows validation error for invalid email format", async ({ page }) => {
    await waitForAuthUi(page);
    const emailInput = page.locator("#login-email");
    await emailInput.fill("not-an-email");
    await page.locator("#login-password").fill("BookMarket1!");
    await page.getByRole("button", { name: "Увійти", exact: true }).click();

    const validationMessage = await emailInput.evaluate(
      (element) => (element as HTMLInputElement).validationMessage,
    );
    expect(validationMessage.length).toBeGreaterThan(0);
  });

  test("login with valid credentials redirects to /", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await expect(page).toHaveURL(/\/($|\?)/);
  });

  test("after successful login localStorage accessToken exists", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await expectAccessTokenExists(page);
  });
});
