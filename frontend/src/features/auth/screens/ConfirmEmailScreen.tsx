"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { confirmEmailQuerySchema } from "@/features/auth/model/auth.form-schemas";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./ConfirmEmailScreen.module.css";

type ConfirmStatus = "loading" | "success" | "error";

const REDIRECT_DELAY_MS = 1200;

const normalizeQueryValue = (value: string): string => {
  // Some mail clients can unescape '+' in query strings into spaces.
  return value.replace(/ /g, "+").trim();
};

export function ConfirmEmailScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const confirmEmail = useAuth((state) => state.confirmEmail);
  const processedKeyRef = useRef<string | null>(null);

  const [status, setStatus] = useState<ConfirmStatus>("loading");
  const [message, setMessage] = useState("Підтверджуємо email...");

  const emailParam = searchParams.get("email") ?? "";
  const tokenParam = searchParams.get("token") ?? "";
  const parsedQuery = confirmEmailQuerySchema.safeParse({
    email: emailParam.trim(),
    token: normalizeQueryValue(tokenParam),
  });
  const hasRequiredParams = parsedQuery.success;
  const email = parsedQuery.success ? parsedQuery.data.email : "";
  const token = parsedQuery.success ? parsedQuery.data.token : "";

  useEffect(() => {
    if (!hasRequiredParams) {
      return;
    }

    const requestKey = `${email}:${token}`;
    if (processedKeyRef.current === requestKey) {
      return;
    }
    processedKeyRef.current = requestKey;

    let isUnmounted = false;

    const run = async () => {
      setStatus("loading");
      setMessage("Підтверджуємо email...");

      const result = await confirmEmail({ email, token });

      if (isUnmounted) {
        return;
      }

      if (!result.success) {
        setStatus("error");
        setMessage(result.message || "Посилання недійсне або прострочене.");
        return;
      }

      setStatus("success");
      setMessage("Email успішно підтверджено. Перенаправляємо...");

      window.setTimeout(() => {
        router.replace("/");
      }, REDIRECT_DELAY_MS);
    };

    void run();

    return () => {
      isUnmounted = true;
    };
  }, [confirmEmail, email, hasRequiredParams, router, token]);

  return (
    <main className={styles.main}>
      <section className={styles.card}>
        <h1 className={styles.title}>Підтвердження email</h1>
        {status === "loading" ? <p className={styles.message}>Підтверджуємо email...</p> : null}

        {!hasRequiredParams ? (
          <p className={styles.error}>Посилання недійсне або прострочене.</p>
        ) : null}

        {status === "success" ? <p className={styles.success}>{message}</p> : null}
        {status === "error" && hasRequiredParams ? <p className={styles.error}>{message}</p> : null}

        {(status === "error" || !hasRequiredParams) ? (
          <button type="button" className={styles.linkButton} onClick={() => router.replace("/")}>
            Повернутися до входу
          </button>
        ) : null}
      </section>
    </main>
  );
}


