import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

function resolveFfmpeg(): string {
  const fromEnv = process.env.FFMPEG_PATH;
  if (fromEnv && fs.existsSync(fromEnv)) {
    return fromEnv;
  }
  return "ffmpeg";
}

/**
 * Convert Playwright WebM to silent H.264 MP4 @ 1280×720 30 FPS.
 */
export function convertWebmToMp4(webmPath: string, mp4Path: string): void {
  fs.mkdirSync(path.dirname(mp4Path), { recursive: true });
  if (fs.existsSync(mp4Path)) {
    fs.unlinkSync(mp4Path);
  }

  execFileSync(
    resolveFfmpeg(),
    [
      "-y",
      "-i",
      webmPath,
      "-an",
      "-c:v",
      "libx264",
      "-preset",
      "veryfast",
      "-pix_fmt",
      "yuv420p",
      "-r",
      "30",
      "-vf",
      "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2",
      "-movflags",
      "+faststart",
      mp4Path,
    ],
    { stdio: ["ignore", "pipe", "pipe"] },
  );

  if (!fs.existsSync(mp4Path) || fs.statSync(mp4Path).size < 1000) {
    throw new Error(`ffmpeg failed to produce MP4: ${mp4Path}`);
  }
}

export function getMp4DurationSeconds(mp4Path: string): number {
  const out = execFileSync(
    resolveFfmpeg(),
    ["-i", mp4Path],
    { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] },
  );
  // ffmpeg prints probe info to stderr; execFileSync with pipe may throw — handle below
  void out;
  return 0;
}

export function probeDurationSeconds(mediaPath: string): number {
  try {
    execFileSync(resolveFfmpeg(), ["-i", mediaPath], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    });
    return 0;
  } catch (error) {
    const message = error instanceof Error ? (error as { stderr?: Buffer }).stderr?.toString("utf8") ?? error.message : String(error);
    const match = /Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)/.exec(message);
    if (!match) {
      return 0;
    }
    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    const seconds = Number(match[3]);
    return hours * 3600 + minutes * 60 + seconds;
  }
}
