"use client";

import type { UserDto } from "@/shared/types/user.types";
import styles from "./ProfileCard.module.css";

interface ProfileCardProps {
  user: UserDto;
  onLogout: () => Promise<void> | void;
  loading?: boolean;
}

export function ProfileCard({ user, onLogout, loading = false }: ProfileCardProps) {
  return (
    <section className={styles.card}>
      <h2 className={styles.title}>Profile</h2>
      <p className={styles.subtitle}>You are authenticated.</p>

      <div className={styles.infoBlock}>
        <p className={styles.infoRow}>
          <span className={styles.infoLabel}>Name:</span> {user.firstName} {user.lastName}
        </p>
        <p className={styles.infoRow}>
          <span className={styles.infoLabel}>Role:</span> {user.role}
        </p>
        <p className={styles.infoRow}>
          <span className={styles.infoLabel}>Verified:</span> {user.isVerified ? "Yes" : "No"}
        </p>
        <p className={styles.infoRow}>
          <span className={styles.infoLabel}>Last login:</span> {user.lastLoginAt ?? "-"}
        </p>
      </div>

      <button
        type="button"
        onClick={() => {
          void onLogout();
        }}
        disabled={loading}
        className={styles.logoutButton}
      >
        {loading ? "Logging out..." : "Logout"}
      </button>
    </section>
  );
}



