import Link from "next/link";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function AdminDashboardScreen() {
  return (
    <section className={styles.panel}>
      <div className={styles.titleRow}>
        <div>
          <h2 className={styles.title}>Dashboard</h2>
          <p className={styles.subtitle}>Choose a resource to manage.</p>
        </div>
      </div>

      <div className={styles.formActions}>
        <Link href="/admin/companies" className={styles.linkButton}>
          Manage companies
        </Link>
        <Link href="/admin/companies/pending" className={styles.linkButton}>
          Review pending companies
        </Link>
        <Link href="/admin/categories" className={styles.linkButton}>
          Manage categories
        </Link>
        <Link href="/admin/categories/active" className={styles.linkButton}>
          View active categories
        </Link>
      </div>
    </section>
  );
}

