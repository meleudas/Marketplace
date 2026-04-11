"use client";

import { useState } from "react";
import { useMemo } from "react";
import { useCustomMutation, useList } from "@refinedev/core";
import type { CompanyDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

type SortKey = "name" | "slug" | "contactEmail";
type SortDirection = "asc" | "desc";

export function CompaniesPendingScreen() {
  const { query } = useList<CompanyDto>({ resource: "companies-pending" });
  const { mutateAsync: runCustom, mutation } = useCustomMutation();
  const isPending = mutation.isPending;
  const [actionError, setActionError] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("name");
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");

  const sortedCompanies = useMemo(() => {
    const source = query.data?.data ?? [];
    const copy = [...source];
    copy.sort((a, b) => {
      const direction = sortDirection === "asc" ? 1 : -1;
      const left = (a[sortKey] ?? "").toString().toLowerCase();
      const right = (b[sortKey] ?? "").toString().toLowerCase();
      return left.localeCompare(right) * direction;
    });
    return copy;
  }, [query.data?.data, sortDirection, sortKey]);

  const toggleSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"));
      return;
    }

    setSortKey(key);
    setSortDirection("asc");
  };

  const sortIndicator = (key: SortKey): string => {
    if (sortKey !== key) {
      return "";
    }
    return sortDirection === "asc" ? "▲" : "▼";
  };

  const handleApprove = async (id: string) => {
    setActionError(null);
    const shouldApprove = window.confirm("Approve this company?");
    if (!shouldApprove) {
      return;
    }

    try {
      await runCustom({
        url: `/admin/companies/${id}/approve`,
        method: "post",
        values: {},
      });
      await query.refetch();
    } catch {
      setActionError("Failed to approve company.");
    }
  };

  const handleRevoke = async (id: string) => {
    setActionError(null);
    const shouldRevoke = window.confirm("Revoke company approval?");
    if (!shouldRevoke) {
      return;
    }

    try {
      await runCustom({
        url: `/admin/companies/${id}/revoke-approval`,
        method: "post",
        values: {},
      });
      await query.refetch();
    } catch {
      setActionError("Failed to revoke company approval.");
    }
  };

  return (
    <section className={styles.panel}>
      <div className={styles.titleRow}>
        <div>
          <h2 className={styles.title}>Pending companies</h2>
          <p className={styles.subtitle}>Moderate companies waiting for approval.</p>
        </div>
      </div>

      {query.isLoading ? <p className={styles.stateText}>Loading pending companies...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load pending companies.</p> : null}
      {actionError ? <p className={styles.error}>{actionError}</p> : null}

      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>
                <button type="button" className={styles.thButton} onClick={() => toggleSort("name")}>
                  Name <span className={styles.sortIndicator}>{sortIndicator("name")}</span>
                </button>
              </th>
              <th>
                <button type="button" className={styles.thButton} onClick={() => toggleSort("slug")}>
                  Slug <span className={styles.sortIndicator}>{sortIndicator("slug")}</span>
                </button>
              </th>
              <th>
                <button type="button" className={styles.thButton} onClick={() => toggleSort("contactEmail")}>
                  Contact email <span className={styles.sortIndicator}>{sortIndicator("contactEmail")}</span>
                </button>
              </th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sortedCompanies.map((company) => (
              <tr key={company.id}>
                <td>{company.name}</td>
                <td>{company.slug}</td>
                <td>{company.contactEmail}</td>
                <td>
                  <div className={styles.actionRow}>
                    <button
                      type="button"
                      className={styles.successButton}
                      disabled={isPending}
                      onClick={() => {
                        void handleApprove(company.id);
                      }}
                    >
                      Approve
                    </button>
                    <button
                      type="button"
                      className={styles.ghostButton}
                      disabled={isPending}
                      onClick={() => {
                        void handleRevoke(company.id);
                      }}
                    >
                      Revoke
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

