/**
 * Test user credentials loaded from environment variables.
 * Defaults match `backend/scripts/seed-test-data.sql` (db-seed).
 *
 * Do not commit real production credentials.
 */
export const testUsers = {
  verified: {
    email: process.env.E2E_VERIFIED_EMAIL ?? process.env.E2E_USER_EMAIL ?? "buyer@marketplace.test",
    password:
      process.env.E2E_VERIFIED_PASSWORD ?? process.env.E2E_USER_PASSWORD ?? "Admin123!",
  },
  nonAdmin: {
    email: process.env.E2E_VERIFIED_EMAIL ?? process.env.E2E_USER_EMAIL ?? "buyer@marketplace.test",
    password:
      process.env.E2E_VERIFIED_PASSWORD ?? process.env.E2E_USER_PASSWORD ?? "Admin123!",
  },
  admin: {
    email: process.env.E2E_ADMIN_EMAIL ?? "admin@marketplace.test",
    password: process.env.E2E_ADMIN_PASSWORD ?? "Admin123!",
  },
  unverified: {
    email: process.env.E2E_UNVERIFIED_EMAIL ?? "unverified@marketplace.test",
    password: process.env.E2E_UNVERIFIED_PASSWORD ?? "Admin123!",
  },
  twoFactor: {
    email: process.env.E2E_2FA_EMAIL ?? "twofa@marketplace.test",
    password: process.env.E2E_2FA_PASSWORD ?? "Admin123!",
  },
} as const;

export const defaultTestPassword = "Admin123!";

export function createUniqueTestEmail(prefix = "e2e"): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.test`;
}

export function createUniqueUsername(prefix = "e2euser"): string {
  return `${prefix}${Date.now().toString().slice(-8)}`;
}
