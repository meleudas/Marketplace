import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { defineConfig, devices, type PlaywrightTestConfig } from "@playwright/test";

function loadEnvFile(filePath: string): void {
  if (!existsSync(filePath)) {
    return;
  }

  for (const line of readFileSync(filePath, "utf8").split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      continue;
    }

    const separatorIndex = trimmed.indexOf("=");
    if (separatorIndex === -1) {
      continue;
    }

    const key = trimmed.slice(0, separatorIndex).trim();
    const value = trimmed.slice(separatorIndex + 1).trim();
    if (!(key in process.env)) {
      process.env[key] = value;
    }
  }
}

const frontendRoot = path.resolve(__dirname, "../..");
loadEnvFile(path.resolve(frontendRoot, ".env.e2e"));
loadEnvFile(path.resolve(frontendRoot, ".env"));

// Chromium/Playwright often resolve localhost → 127.0.0.1; keep both sides on IPv4.
const apiUrl = (
  process.env.PLAYWRIGHT_API_URL ??
  process.env.NEXT_PUBLIC_API_URL ??
  "http://127.0.0.1:8080"
).replace("://localhost", "://127.0.0.1");
const baseURL = (
  process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:3000"
).replace("://localhost", "://127.0.0.1");

process.env.PLAYWRIGHT_API_URL = apiUrl;
process.env.NEXT_PUBLIC_API_URL = apiUrl;
process.env.PLAYWRIGHT_BASE_URL = baseURL;
process.env.E2E_BACKEND_AVAILABLE = "true";

const config: PlaywrightTestConfig = {
  testDir: "./flows-real",
  testMatch: ["**/*.spec.ts"],
  fullyParallel: false,
  workers: 1,
  // Seeded catalog search can be slow right after a full DB recreate.
  timeout: 300_000,
  expect: { timeout: 45_000 },
  retries: 0,
  reporter: [["list"]],
  outputDir: path.join(__dirname, ".record-raw-flows-real"),
  use: {
    ...devices["Desktop Chrome"],
    locale: "uk-UA",
    baseURL,
    viewport: { width: 1280, height: 720 },
    trace: "off",
    screenshot: "off",
    video: "off",
  },
};

// Production standalone only — `next dev` shows the Next.js issue badge in recordings.
// Run `npm run build` first (see record:presentation:flows). Set RECORD_REUSE_FRONTEND=1
// to attach to an already-running prod server on the same URL.
config.webServer = {
  command: "node scripts/start-record-frontend.mjs",
  cwd: frontendRoot,
  url: baseURL,
  reuseExistingServer: process.env.RECORD_REUSE_FRONTEND === "1",
  timeout: 180_000,
  env: {
    ...process.env,
    NODE_ENV: "production",
    PORT: "3000",
    HOSTNAME: "127.0.0.1",
    NEXT_PUBLIC_API_URL: apiUrl,
  },
};

export default defineConfig(config);
