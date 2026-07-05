"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/features/auth/model/auth.store";
import { Button, PageBackground } from "@/shared/ui";
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

      router.replace("/home");
    };

    void run();
  }, [code, completeGoogleLogin, router]);

  const effectiveError = !code ? "Відсутній код зворотного виклику Google." : error;

  return (
    <main className={styles.main}>
      <PageBackground />
      <section className={styles.card}>
        <p className={styles.kicker}>Доступ до акаунта Marketplace</p>
        <h1 className={styles.title}>Завершуємо вхід через Google</h1>
        <p className={styles.message}>Зачекайте, поки ми завершуємо вхід.</p>

        {effectiveError ? <p className={styles.error}>{effectiveError}</p> : null}

        {effectiveError ? (
          <Button type="button" variant="dark" fullWidth onClick={() => router.replace("/")}>
            Повернутися до входу
          </Button>
        ) : null}
      </section>
    </main>
  );
}

