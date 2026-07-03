import { expect, test } from "@playwright/test";
import {
  clearAuthState,
  advanceForgotPasswordToStepTwo,
  loginViaUiOrSkip,
  openForgotPasswordForm,
  submitLoginForm,
  waitForAuthUi,
} from "../fixtures/auth.fixture";
import { getVerifiedTestCredentials } from "../fixtures/api.helper";
import { skipIfBackendAuthUnavailable } from "../fixtures/backend.helper";
import { getResetPasswordToken, isMailHelperConfigured } from "../fixtures/mail.helper";
import {
  createUniqueTestEmail,
  defaultTestPassword,
} from "../fixtures/users.fixture";

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

    const movedToStepTwo = page.getByLabel("Reset token");
    const successMessage = page.getByText(/Password reset code sent/i);
    const errorMessage = page.locator('[class*="errorMessage"]');

    await expect(movedToStepTwo.or(successMessage).or(errorMessage)).toBeVisible();

    if (await errorMessage.isVisible()) {
      test.skip(true, "Password reset request was rejected by the backend (likely rate-limited).");
    }

    await expect(successMessage).toBeVisible();
    await expect(movedToStepTwo).toBeVisible();
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

  test("reset with valid token shows a success message", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("reset"), "Set E2E_RESET_PASSWORD_TOKEN for this test");
    test.skip(!process.env.E2E_RESET_PASSWORD_EMAIL, "Set E2E_RESET_PASSWORD_EMAIL for reset flow test");

    const email = process.env.E2E_RESET_PASSWORD_EMAIL!;
    const token = await getResetPasswordToken(email);
    const newPassword = process.env.E2E_RESET_PASSWORD_NEW ?? "ResetPass2!Aa";

    await openForgotPasswordForm(page);
    await page.getByLabel("Email").fill(email);
    await page.getByRole("button", { name: "Send reset token" }).click();
    await page.getByLabel("Reset token").fill(token);
    await page.getByLabel("New password").fill(newPassword);
    await page.getByRole("button", { name: "Reset password" }).click();

    await expect(page.getByText(/Password has been reset|You can login now/i)).toBeVisible();
  });

  test("old password no longer works after reset", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("reset"), "Set E2E_RESET_PASSWORD_TOKEN for this test");
    test.skip(!process.env.E2E_RESET_PASSWORD_EMAIL, "Set E2E_RESET_PASSWORD_EMAIL for reset flow test");
    test.skip(!process.env.E2E_RESET_PASSWORD_OLD, "Set E2E_RESET_PASSWORD_OLD for old-password check");

    const email = process.env.E2E_RESET_PASSWORD_EMAIL!;
    const oldPassword = process.env.E2E_RESET_PASSWORD_OLD!;

    await waitForAuthUi(page);
    await submitLoginForm(page, { email, password: oldPassword });
    await expect(page.getByText(/Invalid email or password|invalid/i)).toBeVisible();
  });

  test("new password works after reset", async ({ page }) => {
    test.skip(skipIfBackendAuthUnavailable(), "Backend auth API is unavailable or rate-limited");
    test.skip(!isMailHelperConfigured("reset"), "Set E2E_RESET_PASSWORD_TOKEN for this test");
    test.skip(!process.env.E2E_RESET_PASSWORD_EMAIL, "Set E2E_RESET_PASSWORD_EMAIL for reset flow test");
    test.skip(!process.env.E2E_RESET_PASSWORD_NEW, "Set E2E_RESET_PASSWORD_NEW for new-password login test");

    await loginViaUiOrSkip(page, {
      email: process.env.E2E_RESET_PASSWORD_EMAIL!,
      password: process.env.E2E_RESET_PASSWORD_NEW!,
    });

    await expect(page).toHaveURL(/\/home$/);
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
