"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/model/auth.store";
import { ForgotPasswordForm } from "@/features/auth/ui/ForgotPasswordForm";
import { LoginForm } from "@/features/auth/ui/LoginForm";
import { RegisterForm } from "@/features/auth/ui/RegisterForm";
import styles from "./AuthHomeScreen.module.css";

export function AuthHomeScreen() {
  const router = useRouter();
  const [authMode, setAuthMode] = useState<"login" | "register" | "forgotPassword">("login");

  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const loading = useAuth((state) => state.loading);
  const initialized = useAuth((state) => state.initialized);
  const loadMe = useAuth((state) => state.loadMe);

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
      <div className={styles.container}>
        <header className={styles.header}>
          <h1 className={styles.title}>Auth MVP</h1>
          <p className={styles.subtitle}>
            Next.js + Axios + Zustand + localStorage token strategy.
          </p>
        </header>

        {!initialized && !loading ? (
          <div className={styles.initCard}>Initializing auth...</div>
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
      </div>

      {loading ? (
        <div className={styles.loadingOverlay}>
          <div className={styles.spinner} />
        </div>
      ) : null}
    </main>
  );
}
