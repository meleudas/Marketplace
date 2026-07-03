import { readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import path from "node:path";

const repoRoot = path.resolve(process.cwd(), "..");

export default async function globalTeardown(): Promise<void> {
  if (process.env.E2E_SKIP_USER_CLEANUP === "true") {
    return;
  }

  const composeFile = path.join(repoRoot, "docker-compose.dev.yml");
  const sqlFile = path.join(repoRoot, "backend/scripts/cleanup-e2e-users.sql");
  const sql = readFileSync(sqlFile, "utf8");

  try {
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
        "-f",
        "-",
      ],
      { cwd: repoRoot, stdio: ["pipe", "inherit", "inherit"], input: sql },
    );
    console.log("[E2E] Removed disposable @example.test users.");
  } catch {
    try {
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
          "-f",
          "-",
        ],
        { cwd: repoRoot, stdio: ["pipe", "inherit", "inherit"], input: sql },
      );
      console.log("[E2E] Removed disposable @example.test users.");
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      console.warn(`[E2E] Could not clean up disposable users: ${message}`);
    }
  }
}
