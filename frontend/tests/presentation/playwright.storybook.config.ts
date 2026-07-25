import path from "node:path";
import { defineConfig, devices } from "@playwright/test";

const frontendRoot = path.resolve(__dirname, "../..");

/** Storybook-only recorder — avoids next/mock API webServers. */
export default defineConfig({
  testDir: "./storybook",
  fullyParallel: false,
  workers: 1,
  timeout: 120_000,
  reporter: [["list"]],
  outputDir: path.join(__dirname, ".record-raw-storybook"),
  use: {
    ...devices["Desktop Chrome"],
    baseURL: "http://127.0.0.1:6006",
    locale: "uk-UA",
    viewport: { width: 1280, height: 720 },
    video: "off",
  },
  webServer: {
    command: "npx --yes http-server storybook-static -p 6006 -a 127.0.0.1 -c-1 --silent",
    cwd: frontendRoot,
    url: "http://127.0.0.1:6006/iframe.html",
    reuseExistingServer: !process.env.RECORD_FORCE,
    timeout: 120_000,
  },
});
