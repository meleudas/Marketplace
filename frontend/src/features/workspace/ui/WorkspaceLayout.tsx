"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./WorkspaceLayout.module.css";

interface WorkspaceLayoutProps {
  children: React.ReactNode;
}

const navItems = [
  { href: "/workspace", label: "Overview" },
  { href: "/workspace/products", label: "Products" },
  { href: "/workspace/inventory", label: "Inventory" },
  { href: "/workspace/members", label: "Members" },
];

export function WorkspaceLayout({ children }: WorkspaceLayoutProps) {
  const pathname = usePathname();

  const user = useAuth((state) => state.user);
  const initialized = useAuth((state) => state.initialized);
  const loading = useAuth((state) => state.loading);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const loadMe = useAuth((state) => state.loadMe);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  if (!initialized || loading) {
    return (
      <main className={styles.shell}>
        <div className={styles.stateCard}>Loading workspace...</div>
      </main>
    );
  }

  if (!isAuthenticated || !user) {
    return (
      <main className={styles.shell}>
        <div className={styles.stateCard}>
          <h1 className={styles.title}>Company Workspace</h1>
          <p className={styles.message}>You need to sign in to access workspace.</p>
          <div className={styles.actions}>
            <Link href="/" className={styles.linkButton}>
              Back to app
            </Link>
            <Link href="/" className={styles.linkButtonPrimary}>
              Sign in
            </Link>
          </div>
        </div>
      </main>
    );
  }

  return (
    <div className={styles.shell}>
      <header className={styles.header}>
        <div>
          <p className={styles.caption}>Internal area</p>
          <h1 className={styles.title}>Company Workspace</h1>
        </div>

        <Link href="/" className={styles.linkButton}>
          Back to app
        </Link>
      </header>

      <nav className={styles.nav}>
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={isActive ? styles.navLinkActive : styles.navLink}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>

      <section className={styles.content}>{children}</section>
    </div>
  );
}

