/**
 * Mail/token helpers for auth flows that depend on out-of-band email delivery.
 *
 * TODO: Integrate one of the following when available in the test environment:
 * - Mailhog / mail capture API (E2E_MAIL_CAPTURE_URL)
 * - Docker log scraping for LoggingEmailSender output
 * - Test-only backend endpoint that returns the latest token for a given email
 */

export class MailHelperNotConfiguredError extends Error {
  constructor(method: string, email: string) {
    super(
      `[mail.helper] ${method} is not configured for "${email}". ` +
        "Set E2E_CONFIRM_EMAIL_TOKEN, E2E_RESET_PASSWORD_TOKEN, or E2E_2FA_CODE, " +
        "or implement E2E_MAIL_CAPTURE_URL integration.",
    );
    this.name = "MailHelperNotConfiguredError";
  }
}

function readEnvToken(name: string): string | null {
  const value = process.env[name]?.trim();
  return value ? value : null;
}

export function isMailHelperConfigured(kind: "confirm" | "reset" | "2fa"): boolean {
  if (kind === "confirm") {
    return Boolean(readEnvToken("E2E_CONFIRM_EMAIL_TOKEN"));
  }

  if (kind === "reset") {
    return Boolean(readEnvToken("E2E_RESET_PASSWORD_TOKEN"));
  }

  return Boolean(readEnvToken("E2E_2FA_CODE"));
}

export async function getConfirmEmailToken(_email: string): Promise<string> {
  const token = readEnvToken("E2E_CONFIRM_EMAIL_TOKEN");

  if (!token) {
    throw new MailHelperNotConfiguredError("getConfirmEmailToken", _email);
  }

  return token;
}

export async function getResetPasswordToken(_email: string): Promise<string> {
  const token = readEnvToken("E2E_RESET_PASSWORD_TOKEN");

  if (!token) {
    throw new MailHelperNotConfiguredError("getResetPasswordToken", _email);
  }

  return token;
}

export async function getTwoFactorCode(_email: string): Promise<string> {
  const code = readEnvToken("E2E_2FA_CODE");

  if (!code) {
    throw new MailHelperNotConfiguredError("getTwoFactorCode", _email);
  }

  return code;
}
