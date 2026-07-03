import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  expectAccessTokenMissing,
  loginViaUiOrSkip,
  openRegisterForm,
  submitRegisterForm,
  waitForAuthUi,
} from "../fixtures/auth.fixture";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { getConfirmEmailToken, isMailHelperConfigured } from "../fixtures/mail.helper";
import {
  createUniqueTestEmail,
  createUniqueUsername,
  defaultTestPassword,
  testUsers,
} from "../fixtures/users.fixture";

test.describe("Auth register and confirm email", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("user can switch from login to register", async ({ page }) => {
    await waitForAuthUi(page);
    await page.getByRole("button", { name: "Register" }).click();
    await expect(page.getByRole("heading", { name: "Register" })).toBeVisible();
    await expect(page.getByLabel("Username")).toBeVisible();
  });

  test("register form shows validation errors", async ({ page }) => {
    await openRegisterForm(page);
    await page.getByRole("button", { name: "Register", exact: true }).click();

    await expect(page.getByText("Username is required")).toBeVisible();
    await expect(page.getByText("Enter a valid email")).toBeVisible();
    await expect(page.getByText("Password is required")).toBeVisible();
  });

  test("register with valid data shows confirm-email success message", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const email = createUniqueTestEmail("register-ui");
    const userName = createUniqueUsername();

    await submitRegisterForm(page, {
      userName,
      email,
      password: defaultTestPassword,
    });

    const successMessage = page.getByText(/confirm your email|Підтвердіть пошту/i);
    const errorMessage = page.locator('[class*="errorMessage"]');

    await expect(successMessage.or(errorMessage)).toBeVisible({ timeout: 10_000 });

    if (await errorMessage.isVisible()) {
      const text = (await errorMessage.textContent()) ?? "";
      test.skip(text.includes("429"), "Register endpoint is rate-limited.");
    }

    await expect(successMessage).toBeVisible();
  });

  test("register must not automatically authenticate the user", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const email = createUniqueTestEmail("register-no-auth");
    await submitRegisterForm(page, {
      userName: createUniqueUsername(),
      email,
      password: defaultTestPassword,
    });

    await expect(page.getByRole("heading", { name: "Register" })).toBeVisible();
    await expect(page).not.toHaveURL(/\/home$/);
  });

  test("after register localStorage accessToken should not exist", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await submitRegisterForm(page, {
      userName: createUniqueUsername(),
      email: createUniqueTestEmail("register-token"),
      password: defaultTestPassword,
    });

    await expectAccessTokenMissing(page);
  });

  test("confirm email route works with valid email and token query params", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("confirm"), "Set E2E_CONFIRM_EMAIL_TOKEN for this test");

    const email = process.env.E2E_CONFIRM_EMAIL ?? testUsers.verified.email;
    const token = await getConfirmEmailToken(email);

    await page.goto(`/confirm-email?email=${encodeURIComponent(email)}&token=${encodeURIComponent(token)}`);

    await expect(page.getByText(/Email успішно підтверджено|Email confirmed/i)).toBeVisible();
    await expect(page).toHaveURL(/\/$/, { timeout: 5_000 });
  });

  test("missing email or token on /confirm-email shows error UI", async ({ page }) => {
    await page.goto("/confirm-email");
    await expect(page.getByText(/Посилання недійсне або прострочене|invalid/i)).toBeVisible();
    await expect(page.getByRole("button", { name: /Повернутися до входу|Back to login/i })).toBeVisible();
  });

  test("invalid or expired token shows an error UI", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const email = createUniqueTestEmail("confirm-invalid");
    await page.goto(
      `/confirm-email?email=${encodeURIComponent(email)}&token=${encodeURIComponent("invalid-token-value")}`,
    );

    await expect(page.getByRole("heading", { name: /Підтвердження email/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /Повернутися до входу|Back to login/i })).toBeVisible();
    await expect(page.getByText(/Email успішно підтверджено/i)).not.toBeVisible();
  });

  test("after successful confirmation user can log in", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("confirm"), "Set E2E_CONFIRM_EMAIL_TOKEN for this test");
    test.skip(!process.env.E2E_CONFIRM_EMAIL, "Set E2E_CONFIRM_EMAIL for post-confirm login test");
    test.skip(!process.env.E2E_CONFIRM_PASSWORD, "Set E2E_CONFIRM_PASSWORD for post-confirm login test");

    const email = process.env.E2E_CONFIRM_EMAIL!;
    const password = process.env.E2E_CONFIRM_PASSWORD!;
    const token = await getConfirmEmailToken(email);

    await page.goto(`/confirm-email?email=${encodeURIComponent(email)}&token=${encodeURIComponent(token)}`);
    await expect(page.getByText(/Email успішно підтверджено|Email confirmed/i)).toBeVisible();
    await page.waitForURL(/\/$/, { timeout: 5_000 });

    await loginViaUiOrSkip(page, { email, password });
    await expect(page).toHaveURL(/\/home$/);
  });
});
