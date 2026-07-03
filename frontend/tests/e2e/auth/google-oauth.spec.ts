import { expect, test } from "@playwright/test";
import { clearAuthState, waitForAuthUi } from "../fixtures/auth.fixture";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";

const mockAccessToken = "mock-google-access-token";
const mockUser = {
  id: "00000000-0000-0000-0000-000000000001",
  firstName: "Google",
  lastName: "User",
  role: "buyer",
  birthday: null,
  avatar: null,
  isVerified: true,
  verificationDocument: null,
  lastLoginAt: null,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  isDeleted: false,
  deletedAt: null,
};

test.describe("Auth Google OAuth (mocked)", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("clicking Continue with Google starts the expected redirect flow", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await waitForAuthUi(page);

    await Promise.all([
      page.waitForURL(
        (url) =>
          url.href.includes("/auth/google") || url.hostname.includes("accounts.google.com"),
        { timeout: 15_000 },
      ),
      page.getByRole("button", { name: "Continue with Google" }).click(),
    ]);

    expect(page.url()).toMatch(/\/auth\/google|accounts\.google\.com/);
  });

  test("successful mocked callback redirects to /home", async ({ page }) => {
    await page.route("**/auth/google/callback", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: mockAccessToken,
          accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        }),
      });
    });

    await page.route("**/users/me", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockUser),
      });
    });

    await page.goto("/auth/callback?code=test-code");

    await expect(page.getByText(/Completing sign-in/i)).toBeVisible();
    await expect(page).toHaveURL(/\/home$/, { timeout: 10_000 });

    const token = await page.evaluate(() => window.localStorage.getItem("accessToken"));
    expect(token).toBe(mockAccessToken);
  });

  test("callback calls Google exchange endpoint and /users/me", async ({ page }) => {
    const callbackRequestPromise = page.waitForRequest(
      (request) =>
        request.method() === "POST" && request.url().includes("/auth/google/callback"),
    );
    const meRequestPromise = page.waitForRequest(
      (request) => request.method() === "GET" && request.url().includes("/users/me"),
    );

    await page.route("**/auth/google/callback", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: mockAccessToken,
          accessTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        }),
      });
    });

    await page.route("**/users/me", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(mockUser),
      });
    });

    await page.goto("/auth/callback?code=test-code");

    const callbackRequest = await callbackRequestPromise;
    expect(callbackRequest.postDataJSON()).toEqual({ code: "test-code" });

    const meRequest = await meRequestPromise;
    expect(meRequest.headers().authorization).toBe(`Bearer ${mockAccessToken}`);

    await expect(page).toHaveURL(/\/home$/, { timeout: 10_000 });
  });

  test("missing code shows Missing Google callback code error UI", async ({ page }) => {
    await page.goto("/auth/callback");

    await expect(page.getByText("Missing Google callback code.")).toBeVisible();
    await expect(page.getByRole("button", { name: "Back to login" })).toBeVisible();
  });

  test("invalid callback code shows error UI and back-to-login option", async ({ page }) => {
    await page.route("**/auth/google/callback", async (route) => {
      await route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({
          title: "Unauthorized",
          detail: "Invalid or expired exchange code.",
          status: 401,
        }),
      });
    });

    await page.goto("/auth/callback?code=invalid-code");

    await expect(page.getByRole("heading", { name: "Google sign-in" })).toBeVisible();
    await expect(
      page.getByText(
        /Invalid or expired exchange code|Network error|Unauthorized|Refresh token is required/i,
      ),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Back to login" })).toBeVisible();
  });
});
