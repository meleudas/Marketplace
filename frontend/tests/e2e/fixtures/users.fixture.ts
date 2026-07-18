/**
 * Test user credentials loaded from environment variables.
 * Defaults match `UserSeeder` (app startup seed): password `BookMarket1!`.
 *
 * Do not commit real production credentials.
 */
export const testUsers = {
  verified: {
    email: process.env.E2E_VERIFIED_EMAIL ?? process.env.E2E_USER_EMAIL ?? "user50@bookmarket.ua",
    password:
      process.env.E2E_VERIFIED_PASSWORD ?? process.env.E2E_USER_PASSWORD ?? "BookMarket1!",
  },
  admin: {
    email: process.env.E2E_ADMIN_EMAIL ?? "admin@bookmarket.ua",
    password: process.env.E2E_ADMIN_PASSWORD ?? "BookMarket1!",
  },
  twoFactor: {
    email: process.env.E2E_2FA_EMAIL ?? "twofa@bookmarket.ua",
    password: process.env.E2E_2FA_PASSWORD ?? "BookMarket1!",
  },
} as const;

export function createUniqueTestEmail(prefix = "e2e"): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.test`;
}

export function createUniqueUsername(prefix = "e2euser"): string {
  return `${prefix}${Date.now().toString().slice(-8)}`;
}
