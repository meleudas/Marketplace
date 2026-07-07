"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/model/auth.store";
import { ForgotPasswordForm } from "@/features/auth/ui/ForgotPasswordForm";
import { LoginForm } from "@/features/auth/ui/LoginForm";
import { RegisterForm } from "@/features/auth/ui/RegisterForm";
import { PageBackground, Spinner } from "@/shared/ui";
import styles from "./AuthHomeScreen.module.css";

export function AuthHomeScreen() {
  const router = useRouter();
  const [authMode, setAuthMode] = useState<"login" | "register" | "forgotPassword">("login");

  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const loading = useAuth((state) => state.loading);
  const initialized = useAuth((state) => state.initialized);
  const loadMe = useAuth((state) => state.loadMe);
  const isRedirecting = initialized && isAuthenticated;

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  useEffect(() => {
    if (initialized && isAuthenticated) {
      router.replace("/");
    }
  }, [initialized, isAuthenticated, router]);

  return (
    <main className={styles.main}>
      <PageBackground />
      <div className={styles.container}>
        {isRedirecting ? (
          <div className={styles.redirectingState}>
            <Spinner size="lg" />
          </div>
        ) : (
          <>
            {authMode === "login" ? (
              <header className={styles.header}>
                <p className={styles.subtitle}>Введіть логін та пароль для входу в ваш акаунт</p>
              </header>
            ) : null}

            {!initialized && !loading ? (
              <div className={styles.initCard}>Готуємо вашу сесію...</div>
            ) : null}

            {initialized && !isAuthenticated ? (
              <section className={styles.authSection}>
                {authMode === "login" ? (
                  <LoginForm
                    onSwitchToRegister={() => setAuthMode("register")}
                    onForgotPassword={() => setAuthMode("forgotPassword")}
                  />
                ) : null}

                {authMode === "register" ? (
                  <RegisterForm onSwitchToLogin={() => setAuthMode("login")} />
                ) : null}

                {authMode === "forgotPassword" ? (
                  <ForgotPasswordForm onSwitchToLogin={() => setAuthMode("login")} />
                ) : null}
              </section>
            ) : null}
          </>
        )}
      </div>

      {loading && !isRedirecting ? (
        <div className={styles.loadingOverlay}>
          <Spinner size="lg" />
        </div>
      ) : null}
    </main>
  );
}
