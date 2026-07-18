import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  expectAccessTokenMissing,
  openRegisterForm,
  submitRegisterForm,
  waitForAuthUi,
} from "../fixtures/auth.fixture";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import {
  createUniqueTestEmail,
  createUniqueUsername,
} from "../fixtures/users.fixture";

const registrationPassword = "Admin123!Aa1";

test.describe("Auth register and confirm email", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("user can switch from login to register", async ({ page }) => {
    await waitForAuthUi(page);
    await page.getByRole("link", { name: "Створити" }).click();
    await expect(page).toHaveURL(/\/auth\/register/);
    await expect(page.getByRole("heading", { name: "Створіть акаунт" })).toBeVisible();
    await expect(page.locator("#register-userName")).toBeVisible();
  });

  test("register form shows validation errors", async ({ page }) => {
    await openRegisterForm(page);
    await page.getByRole("button", { name: "Створити акаунт", exact: true }).click();

    await expect(page.getByText("Ім'я користувача є обов'язковим")).toBeVisible();
    await expect(page.getByText("Введіть дійсну електронну адресу")).toBeVisible();
    await expect(page.getByText("Пароль є обов'язковим")).toBeVisible();
  });

  test("register with valid data shows confirm-email success message", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const email = createUniqueTestEmail("register-ui");
    const userName = createUniqueUsername();

    await submitRegisterForm(page, {
      userName,
      email,
      password: registrationPassword,
    });

    const successMessage = page.getByText(/Підтвердьте пошту|Підтвердіть пошту|confirm your email/i);
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
      password: registrationPassword,
    });

    await expect(page.getByRole("heading", { name: "Створіть акаунт" })).toBeVisible();
    await expect(page).toHaveURL(/\/auth\/register/);
  });

  test("after register localStorage accessToken should not exist", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    await submitRegisterForm(page, {
      userName: createUniqueUsername(),
      email: createUniqueTestEmail("register-token"),
      password: registrationPassword,
    });

    await expectAccessTokenMissing(page);
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
});
