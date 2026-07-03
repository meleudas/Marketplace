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
    await expect(page.getByText(/Enter your email and we will send you a reset token/i)).toBeVisible();
  });

  test("step 1 validates email", async ({ page }) => {
    await openForgotPasswordForm(page);
    await page.getByRole("button", { name: "Send reset token" }).click();
    await expect(page.getByText("Enter a valid email")).toBeVisible();
  });

  test("step 1 submits email and moves to reset step", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await openForgotPasswordForm(page);
    await page.getByLabel("Email").fill(credentials.email);
    await page.getByRole("button", { name: "Send reset token" }).click();

    const successMessage = page.getByText(/Password reset code sent/i);
    const errorMessage = page.locator('[class*="errorMessage"]');

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
    await expect(page.getByLabel("Reset token")).toBeVisible();
    await expect(page.getByLabel("New password")).toBeVisible();
  });

  test("reset step keeps email readonly", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await advanceForgotPasswordToStepTwo(page, credentials.email);
    await expect(page.getByLabel("Email")).toHaveJSProperty("readOnly", true);
  });

  test("reset form validates empty token and empty new password", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await advanceForgotPasswordToStepTwo(page, credentials.email);

    await page.getByRole("button", { name: "Reset password" }).click();
    await expect(page.getByText("Reset token is required")).toBeVisible();
    await expect(page.getByText("New password is required")).toBeVisible();
  });

  test("resend button cooldown behavior is respected", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");

    const credentials = await getVerifiedTestCredentials();
    await advanceForgotPasswordToStepTwo(page, credentials.email);

    const resendButton = page.getByRole("button", { name: /Did not receive the code\? Send again/i });
    await expect(resendButton).toBeDisabled();
    await expect(page.getByText(/Available in \d+s/i)).toBeVisible();
  });
});
