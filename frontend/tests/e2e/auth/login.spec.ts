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

  test("guest can open / and see the auth UI", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Auth MVP" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Login" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Continue with Google" })).toBeVisible();
  });

  test("guest can open /auth and see the auth UI", async ({ page }) => {
    await page.goto("/auth");
    await expect(page.getByRole("heading", { name: "Auth MVP" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Login" })).toBeVisible();
  });

  test("login form shows validation errors for empty email and password", async ({ page }) => {
    await waitForAuthUi(page);
    await page.getByRole("button", { name: "Login", exact: true }).click();

    await expect(page.getByText("Enter a valid email")).toBeVisible();
    await expect(page.getByText("Password is required")).toBeVisible();
  });

  test("login form shows validation error for invalid email format", async ({ page }) => {
    await waitForAuthUi(page);
    const emailInput = page.getByLabel("Email");
    await emailInput.fill("not-an-email");
    await page.getByLabel("Password").fill("Admin123!");
    await page.getByRole("button", { name: "Login", exact: true }).click();

    const validationMessage = await emailInput.evaluate(
      (element) => (element as HTMLInputElement).validationMessage,
    );
    expect(validationMessage.length).toBeGreaterThan(0);
  });

  test("login with valid credentials redirects to /home", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await expect(page).toHaveURL(/\/home$/);
  });

  test("after successful login localStorage accessToken exists", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await loginViaUi(page, credentials);
    await expectAccessTokenExists(page);
  });
});
