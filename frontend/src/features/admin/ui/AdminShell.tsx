"use client";

import Link from "next/link";
import { useEffect } from "react";
import { usePathname } from "next/navigation";
import { AdminRefineProvider } from "@/features/admin/providers/AdminRefineProvider";
import { useAuth } from "@/features/auth/model/auth.store";
import styles from "./AdminShell.module.css";

interface AdminShellProps {
  children: React.ReactNode;
}

const navLinks = [
  { href: "/admin", label: "Dashboard" },
  { href: "/admin/companies", label: "Companies" },
  { href: "/admin/companies/pending", label: "Pending companies" },
  { href: "/admin/categories", label: "Categories" },
  { href: "/admin/categories/active", label: "Active categories" },
];

export function AdminShell({ children }: AdminShellProps) {
  const pathname = usePathname();
  const initialized = useAuth((state) => state.initialized);
  const isAuthenticated = useAuth((state) => state.isAuthenticated);
  const user = useAuth((state) => state.user);
  const loadMe = useAuth((state) => state.loadMe);

  useEffect(() => {
    void loadMe();
  }, [loadMe]);

  if (!initialized) {
    return (
      <div className={styles.centerState}>
        <section className={styles.card}>
          <h1 className={styles.cardTitle}>Admin</h1>
          <p className={styles.cardText}>Loading admin access...</p>
        </section>
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return (
      <div className={styles.centerState}>
        <section className={styles.card}>
          <h1 className={styles.cardTitle}>Access denied</h1>
          <p className={styles.cardText}>You need to sign in before opening admin tools.</p>
          <p className={styles.cardText}>
            <Link href="/" className={styles.backLink}>
              Back to app
            </Link>
          </p>
        </section>
      </div>
    );
  }

  if (user.role !== "admin") {
    return (
      <div className={styles.centerState}>
        <section className={styles.card}>
          <h1 className={styles.cardTitle}>Access denied</h1>
          <p className={styles.cardText}>Only admin users can open this section.</p>
          <p className={styles.cardText}>
            <Link href="/" className={styles.backLink}>
              Back to app
            </Link>
          </p>
        </section>
      </div>
    );
  }

  return (
    <AdminRefineProvider>
      <div className={styles.shell}>
        <aside className={styles.sidebar}>
          <h2 className={styles.brand}>Marketplace Admin</h2>
          <nav className={styles.nav}>
            {navLinks.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={styles.navLink}
                aria-current={pathname === item.href ? "page" : undefined}
              >
                {item.label}
              </Link>
            ))}
          </nav>
        </aside>

        <div className={styles.main}>
          <header className={styles.header}>
            <h1 className={styles.headerTitle}>Admin section</h1>
            <Link href="/" className={styles.backLink}>
              Back to app
            </Link>
          </header>
          <div className={styles.content}>{children}</div>
        </div>
      </div>
    </AdminRefineProvider>
  );
}

