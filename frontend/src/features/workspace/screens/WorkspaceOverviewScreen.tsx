"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useAuth } from "@/features/auth/model/auth.store";
import { getMyCompanyMembership } from "@/features/workspace/api/workspace.api";
import { WORKSPACE_COMPANY_ID } from "@/features/workspace/config/workspace.constants";
import type { CompanyMembershipDto } from "@/features/workspace/model/workspace.types";
import { WorkspaceMembershipError } from "@/features/workspace/model/workspace.types";
import styles from "./WorkspaceScreen.module.css";

const formatValue = (value: string | null | undefined): string => value ?? "-";

export function WorkspaceOverviewScreen() {
  const user = useAuth((state) => state.user);
  const isGlobalAdmin = user?.role === "admin";

  const [loading, setLoading] = useState(true);
  const [membership, setMembership] = useState<CompanyMembershipDto | null>(null);
  const [errorKind, setErrorKind] = useState<"forbidden" | "notFound" | "unknown" | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        setLoading(true);
        setErrorKind(null);

        const data = await getMyCompanyMembership(WORKSPACE_COMPANY_ID);
        if (!cancelled) {
          setMembership(data);
        }
      } catch (error) {
        if (cancelled) {
          return;
        }

        if (error instanceof WorkspaceMembershipError) {
          if (isGlobalAdmin && (error.kind === "forbidden" || error.kind === "notFound")) {
            setMembership(null);
            return;
          }

          setErrorKind(error.kind);
          return;
        }

        setErrorKind("unknown");
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [isGlobalAdmin]);

  if (loading) {
    return <p className={styles.state}>Loading workspace data...</p>;
  }

  if ((errorKind === "forbidden" || errorKind === "notFound") && !isGlobalAdmin) {
    return <p className={styles.state}>You do not have access to this company workspace</p>;
  }

  if (errorKind) {
    return <p className={styles.state}>Failed to load workspace data</p>;
  }

  if (!membership && !isGlobalAdmin) {
    return <p className={styles.state}>No membership data</p>;
  }

  return (
    <div className={styles.stack}>
      <section className={styles.card}>
        <h2 className={styles.sectionTitle}>Overview</h2>
        <p className={styles.row}>
          <span className={styles.label}>Company ID:</span> {WORKSPACE_COMPANY_ID}
        </p>
      </section>

      <section className={styles.card}>
        <h2 className={styles.sectionTitle}>My role in company</h2>
        {isGlobalAdmin && !membership ? (
          <p className={styles.row}>Global admin mode: membership record was not found for this static company.</p>
        ) : null}
        <p className={styles.row}>
          <span className={styles.label}>Role:</span> {membership?.role ?? "admin"}
        </p>
        <p className={styles.row}>
          <span className={styles.label}>Is owner:</span> {membership?.isOwner ? "Yes" : "No"}
        </p>
        <p className={styles.row}>
          <span className={styles.label}>Created:</span> {formatValue(membership?.createdAt)}
        </p>
        <p className={styles.row}>
          <span className={styles.label}>Updated:</span> {formatValue(membership?.updatedAt)}
        </p>
      </section>

      <section className={styles.card}>
        <h2 className={styles.sectionTitle}>Quick links</h2>
        <div className={styles.links}>
          <Link href="/workspace/products" className={styles.linkButton}>
            Products
          </Link>
          <Link href="/workspace/inventory" className={styles.linkButton}>
            Inventory
          </Link>
          <Link href="/workspace/members" className={styles.linkButton}>
            Members
          </Link>
        </div>
      </section>
    </div>
  );
}

