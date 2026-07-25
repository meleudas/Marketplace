import { expect, test } from "@playwright/test";
import { clearAuthState, loginViaUi, openAuthenticatedSession } from "../fixtures/auth.fixture";
import {
  getAdminTestCredentials,
  getVerifiedTestCredentials,
  loginUserViaApi,
} from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";

async function authenticateVerifiedUser(page: Parameters<typeof openAuthenticatedSession>[0]) {
  const credentials = await getVerifiedTestCredentials();
  const tokens = await loginUserViaApi(credentials.email, credentials.password);
  await openAuthenticatedSession(page, tokens.accessToken);
}

test.describe("Auth protected routes - guest", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("/me shows sign-in prompt for guest", async ({ page }) => {
    await page.goto("/me");
    await expect(page.getByText("Увійдіть, щоб переглянути свій профіль")).toBeVisible();
    await expect(page.getByRole("link", { name: "Увійти" })).toBeVisible();
  });

  test("/settings redirects guest to /", async ({ page }) => {
    await page.goto("/settings");
    await expect(page).toHaveURL(/\/($|\?)/);
  });

  test("/workspace shows unauthenticated message for guest", async ({ page }) => {
    await page.goto("/workspace");
    await expect(page.getByText("You need to sign in to access workspace.")).toBeVisible();
  });

  test("/admin shows unauthenticated message for guest", async ({ page }) => {
    await page.goto("/admin");
    await expect(page.getByText("You need to sign in before opening admin tools.")).toBeVisible();
  });

  test("/ is accessible as guest", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveURL(/\/($|\?)/);
    await expect(
      page.getByRole("banner").getByRole("link", { name: "BOOK TOP — на головну" }),
    ).toBeVisible();
  });
});

test.describe("Auth protected routes - authenticated", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("authenticated user can open /me", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await authenticateVerifiedUser(page);
    await page.goto("/me");
    await expect(page.getByRole("heading", { name: "Персональні дані" })).toBeVisible();
    await expect(page.getByText("Увійдіть, щоб переглянути свій профіль")).not.toBeVisible();
  });

  test("authenticated user can open /settings", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await authenticateVerifiedUser(page);
    await page.goto("/settings");
    await expect(page.getByRole("heading", { name: "Security Settings" })).toBeVisible();
  });

  test("authenticated user can open /workspace", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await authenticateVerifiedUser(page);
    await page.goto("/workspace");
    await expect(page.getByText("You need to sign in to access workspace.")).not.toBeVisible({
      timeout: 20_000,
    });
    await expect(page.getByRole("navigation").getByRole("link", { name: "Overview" })).toBeVisible({
      timeout: 20_000,
    });
  });

  test("non-admin user cannot access /admin", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await authenticateVerifiedUser(page);
    await page.goto("/admin");
    await expect(page.getByText("Only admin users can open this section.")).toBeVisible();
  });

  test("admin user can access /admin", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    let adminCredentials: { email: string; password: string };
    try {
      adminCredentials = await getAdminTestCredentials();
    } catch {
      test.skip(true, "Admin seed user is unavailable");
      return;
    }

    // Admin path still exercises UI login once (role-specific credentials).
    await loginViaUi(page, adminCredentials);
    await page.goto("/admin");
    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Companies", exact: true })).toBeVisible();
  });
});
