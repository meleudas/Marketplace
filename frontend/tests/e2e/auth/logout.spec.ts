import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  expectAccessTokenExists,
  expectAccessTokenMissing,
  expectGuest,
  loginViaUi,
  logoutViaUi,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { getApiBaseUrl, skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";

test.describe("Auth logout", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("authenticated user can open /me", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.goto("/me");

    await expect(page.getByRole("heading", { name: "My profile" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Logout" })).toBeVisible();
  });

  test("user can log out from /me", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await logoutViaUi(page);

    await expect(page.getByRole("heading", { name: "Auth MVP" })).toBeVisible();
  });

  test("logout clears localStorage accessToken", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await logoutViaUi(page);
    await expectAccessTokenMissing(page);
  });

  test("logout redirects to /", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await page.goto("/me");
    await page.getByRole("button", { name: "Logout" }).click();
    await expect(page).toHaveURL(/\/$/);
  });

  test("after logout /me shows sign-in prompt", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await logoutViaUi(page);

    await page.goto("/me");
    await expect(page.getByText("You need to sign in first")).toBeVisible();
    await expect(page.getByRole("link", { name: "Sign in" })).toBeVisible();
  });

  test("after logout /settings redirects to /", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await logoutViaUi(page);

    await page.goto("/settings");
    await expect(page).toHaveURL(/\/$/);
  });

  test("logout still clears local session when /auth/logout returns 500", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);

    await page.route("**/auth/logout", async (route) => {
      await route.fulfill({ status: 500, body: "Internal Server Error" });
    });

    await page.goto("/me");
    await page.getByRole("button", { name: "Logout" }).click();
    await expect(page).toHaveURL(/\/$/);
    await expectAccessTokenMissing(page);
    await expectGuest(page);
  });
});

test.describe("Auth logout API endpoint", () => {
  test("uses configured API base URL for logout requests", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await clearAuthState(page);
    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);

    const logoutRequestPromise = page.waitForRequest(
      (request) =>
        request.method() === "POST" && request.url().includes("/auth/logout"),
    );

    await page.goto("/me");
    await page.getByRole("button", { name: "Logout" }).click();
    const logoutRequest = await logoutRequestPromise;

    expect(logoutRequest.url()).toContain(getApiBaseUrl());
  });
});
