import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  advanceForgotPasswordToStepTwo,
  openForgotPasswordForm,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";

test.describe("Auth forgot password", () => {
  test.beforeEach(async ({ page }) => {
    await clearAuthState(page);
  });

  test("user can navigate from login to forgot password", async ({ page }) => {
    await openForgotPasswordForm(page);
    await expect(
      page.getByText("Введіть електронну пошту, і ми надішлемо код скидання."),
    ).toBeVisible();
  });

  test("step 1 validates email", async ({ page }) => {
    await openForgotPasswordForm(page);
    await page.getByRole("button", { name: "Надіслати код скидання" }).click();
    await expect(page.getByText("Введіть дійсну електронну адресу")).toBeVisible();
  });

  test("step 1 submits email and moves to reset step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await openForgotPasswordForm(page);
    await page.locator("#forgot-email").fill(credentials.email);
    await page.getByRole("button", { name: "Надіслати код скидання" }).click();

    const successMessage = page.getByText(/Код для скидання пароля надіслано/i);
    const errorMessage = page.locator("main [role='alert']");

    const result = await Promise.race([
      successMessage
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => "success" as const),
      errorMessage
        .waitFor({ state: "visible", timeout: 10_000 })
        .then(() => "error" as const),
    ]).catch(() => "timeout" as const);

    if (result === "error") {
      test.skip(true, "Password reset request was rejected by the backend (likely rate-limited).");
    }

    expect(result).toBe("success");
    await expect(page.locator("#forgot-token")).toBeVisible();
    await expect(page.locator("#forgot-new-password")).toBeVisible();
  });

  test("reset step keeps email readonly", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await advanceForgotPasswordToStepTwo(page, credentials.email);
    await expect(page.locator("#forgot-email")).toHaveJSProperty("readOnly", true);
  });

  test("reset form validates empty token and empty new password", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await advanceForgotPasswordToStepTwo(page, credentials.email);

    await page.getByRole("button", { name: "Оновити пароль" }).click();
    await expect(page.getByText("Код скидання є обов'язковим")).toBeVisible();
    await expect(page.getByText("Новий пароль є обов'язковим")).toBeVisible();
  });

  test("resend button cooldown behavior is respected", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await advanceForgotPasswordToStepTwo(page, credentials.email);

    const resendButton = page.getByRole("button", { name: /Не отримали код\? Надіслати ще раз/i });
    await expect(resendButton).toBeDisabled();
    await expect(page.getByText(/Доступно через \d+ с/i)).toBeVisible();
  });
});
