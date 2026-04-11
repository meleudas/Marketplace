"use client";

import Link from "next/link";
import { useEffect } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./StorefrontLayout.module.css";

interface StorefrontLayoutProps {
  title: string;
  children: React.ReactNode;
}

export function StorefrontLayout({ title, children }: StorefrontLayoutProps) {
  const user = useAuth((state) => state.user);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const initialized = useAuth((state) => state.initialized);
  const loadMe = useAuth((state) => state.loadMe);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  const isSignedIn = initialized && isAuthenticated && Boolean(user);

  return (
    <main className={styles.main}>
      <header className={styles.header}>
        <h1 className={styles.brand}>{title}</h1>
        <nav className={styles.nav}>
          <Link href="/" className={styles.link}>
            Home
          </Link>
          <Link href="/products" className={styles.link}>
            Products
          </Link>
          <Link href="/companies" className={styles.link}>
            Companies
          </Link>

          {isSignedIn ? (
            <Link href="/me" className={styles.link}>
              Profile
            </Link>
          ) : (
            <Link href="/auth" className={styles.link}>
              Sign in
            </Link>
          )}

          {isSignedIn && user?.role === "admin" ? (
            <Link href="/admin" className={styles.link}>
              Open admin panel
            </Link>
          ) : null}
        </nav>
      </header>

      <section className={styles.content}>{children}</section>
    </main>
  );
}
