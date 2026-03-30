"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./GoogleAuthCallbackScreen.module.css";

export function GoogleAuthCallbackScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const completeGoogleLogin = useAuth((state) => state.completeGoogleLogin);
  const code = searchParams.get("code");

  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!code) {
      return;
    }

    const run = async () => {
      const result = await completeGoogleLogin(code);
      if (!result.success) {
        setError(result.message);
        return;
      }

      router.replace("/");
    };

    void run();
  }, [code, completeGoogleLogin, router]);

  const effectiveError = !code ? "Missing Google callback code." : error;

  return (
    <main className={styles.main}>
      <section className={styles.card}>
        <h1 className={styles.title}>Google sign-in</h1>
        <p className={styles.message}>Completing sign-in, please wait...</p>

        {effectiveError ? <p className={styles.error}>{effectiveError}</p> : null}

        {effectiveError ? (
          <button type="button" className={styles.linkButton} onClick={() => router.replace("/")}>
            Back to login
          </button>
        ) : null}
      </section>
    </main>
  );
}

