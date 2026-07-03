"use client";

import Link from "next/link";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./MeScreen.module.css";

const formatValue = (value: string | null): string => value ?? "-";

export function MeScreen() {
  const router = useRouter();

  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const initialized = useAuth((state) => state.initialized);
  const loading = useAuth((state) => state.loading);
  const loadMe = useAuth((state) => state.loadMe);
  const logout = useAuth((state) => state.logout);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  if (!initialized || loading) {
    return (
      <main className={styles.main}>
        <section className={styles.card}>
          <h1 className={styles.title}>My profile</h1>
          <p className={styles.subtitle}>Loading account...</p>
        </section>
      </main>
    );
  }

  if (!isAuthenticated || !user) {
    return (
      <main className={styles.main}>
        <section className={styles.card}>
          <h1 className={styles.title}>My profile</h1>
          <p className={styles.subtitle}>You need to sign in first.</p>

          <div className={styles.actions}>
            <Link href="/auth" className={styles.primaryButton}>
              Sign in
            </Link>
            <Link href="/" className={styles.ghostButton}>
              Back to home
            </Link>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className={styles.main}>
      <section className={styles.card}>
        <h1 className={styles.title}>My profile</h1>
        <p className={styles.subtitle}>Account details and security settings.</p>

        <div className={styles.grid}>
          <p className={styles.row}>
            <span className={styles.label}>Name:</span> {user.firstName} {user.lastName}
          </p>
          <p className={styles.row}>
            <span className={styles.label}>Role:</span> {user.role}
          </p>
          <p className={styles.row}>
            <span className={styles.label}>Email verified:</span> {user.isVerified ? "Yes" : "No"}
          </p>
          <p className={styles.row}>
            <span className={styles.label}>Birthday:</span> {formatValue(user.birthday)}
          </p>
          <p className={styles.row}>
            <span className={styles.label}>Last login:</span> {formatValue(user.lastLoginAt)}
          </p>
          <p className={styles.row}>
            <span className={styles.label}>Created at:</span> {user.createdAt}
          </p>
          <p className={styles.row}>
            <span className={styles.label}>Updated at:</span> {user.updatedAt}
          </p>
        </div>

        <div className={styles.actions}>
          <Link href="/settings" className={styles.primaryButton}>
            Open settings
          </Link>
          {user.role === "admin" ? (
            <Link href="/admin" className={styles.secondaryButton}>
              Open admin panel
            </Link>
          ) : null}
          <Link href="/" className={styles.ghostButton}>
            Back to home
          </Link>
          <button
            type="button"
            className={styles.dangerButton}
            onClick={async () => {
              await logout();
              router.replace("/");
            }}
          >
            Logout
          </button>
        </div>
      </section>
    </main>
  );
}
