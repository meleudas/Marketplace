import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  expectAccessTokenExists,
  getAuthorizationHeader,
  loginViaUi,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";

test.describe("Auth session restore", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("user logs in successfully", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await expectAccessTokenExists(page);
  });

  test("user reloads the page and remains authenticated", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.reload();
    await expect(page).toHaveURL(/\/($|\?)/);
    await expectAccessTokenExists(page);
  });

  test("user opens /me and sees authenticated profile content", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.goto("/me");

    await expect(page.getByRole("heading", { name: "Персональні дані" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Контакти" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Вийти з профілю" })).toBeVisible();
    await expect(page.getByText("Увійдіть, щоб переглянути свій профіль")).not.toBeVisible();
  });

  test("user revisits / and stays on home", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.goto("/");
    await expect(page).toHaveURL(/\/($|\?)/);
  });

  test("session restore calls /users/me with Bearer token via loadMe", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.goto("/me");

    const meResponsePromise = page.waitForResponse(
      (response) =>
        response.url().includes("/users/me") &&
        response.request().method() === "GET" &&
        response.status() === 200,
    );

    await page.reload();
    const meResponse = await meResponsePromise;

    const authHeader = getAuthorizationHeader(meResponse.request().headers());
    expect(authHeader).toMatch(/^Bearer .+/);

    await expect(page.getByText("Увійдіть, щоб переглянути свій профіль")).not.toBeVisible();
    await expect(page.getByRole("heading", { name: "Персональні дані" })).toBeVisible();
  });
});
