import { execFileSync } from "node:child_process";
import path from "node:path";
import { expect, type Page } from "@playwright/test";
import {
  submitRegisterForm,
  loginViaUi,
  clearAuthState,
} from "./auth.fixture";
import { createUniqueTestEmail, createUniqueUsername } from "./users.fixture";
import { formatExpectedCartPrice } from "./cart.fixture";

export const journeyPassword = "Admin123!Aa1";

export interface JourneyUser {
  email: string;
  password: string;
  userName: string;
}

export function createJourneyUser(): JourneyUser {
  return {
    email: createUniqueTestEmail("journey"),
    password: journeyPassword,
    userName: createUniqueUsername("journey"),
  };
}

function runPostgresSql(sql: string): void {
  const repoRoot = path.resolve(process.cwd(), "..");
  const composeFile = path.join(repoRoot, "docker-compose.dev.yml");

  execFileSync(
    "docker",
    [
      "compose",
      "-f",
      composeFile,
      "exec",
      "-T",
      "postgres",
      "psql",
      "-U",
      "postgres",
      "-d",
      "marketplace",
      "-v",
      "ON_ERROR_STOP=1",
      "-c",
      sql,
    ],
    { cwd: repoRoot, stdio: ["ignore", "pipe", "pipe"] },
  );
}

/** Ensures a freshly registered user can log in when RequireConfirmedEmail is enabled. */
export function markEmailConfirmedInDb(email: string): void {
  if (!email.includes("@") || email.includes("'")) {
    throw new Error("Invalid email for DB confirmation helper.");
  }

  runPostgresSql(
    `UPDATE "AspNetUsers" SET "EmailConfirmed" = TRUE WHERE "Email" = '${email}';`,
  );
}

export async function registerAndLoginJourneyUser(
  page: Page,
  user: JourneyUser = createJourneyUser(),
): Promise<JourneyUser> {
  await clearAuthState(page);
  await page.goto("/");
  await expect(
    page.getByRole("banner").getByRole("link", { name: "BOOK TOP — на головну" }),
  ).toBeVisible();

  await submitRegisterForm(page, {
    userName: user.userName,
    email: user.email,
    password: user.password,
  });

  const successMessage = page.getByText(/Підтвердьте пошту|Підтвердіть пошту|confirm your email/i);
  const errorMessage = page.locator("main [role='alert']");
  await expect(successMessage.or(errorMessage)).toBeVisible({ timeout: 20_000 });

  if (await errorMessage.isVisible()) {
    const text = (await errorMessage.textContent()) ?? "";
    throw new Error(`Registration failed: ${text}`);
  }

  markEmailConfirmedInDb(user.email);
  await loginViaUi(page, { email: user.email, password: user.password });
  return user;
}

export function formatExpectedCartTotal(lineTotals: number[]): string {
  const sum = lineTotals.reduce((acc, value) => acc + value, 0);
  const factor = 100;
  const rounded = Math.ceil(Number(sum.toFixed(10)) * factor) / factor;
  return `${rounded.toFixed(2)}грн`;
}

export function expectedLineTotalLabel(unitPrice: number, quantity: number): string {
  return formatExpectedCartPrice(unitPrice, quantity);
}
