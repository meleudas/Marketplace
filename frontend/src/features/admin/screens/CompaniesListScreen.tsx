"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useCustomMutation, useDelete, useList } from "@refinedev/core";
import type { CompanyDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

type SortKey = "name" | "slug" | "contactEmail" | "isApproved";
type SortDirection = "asc" | "desc";

export function CompaniesListScreen() {
  const { query } = useList<CompanyDto>({ resource: "companies" });
  const { mutateAsync: deleteOne, mutation: deleteMutation } = useDelete();
  const { mutateAsync: runCustom, mutation: customMutation } = useCustomMutation();
  const isDeleting = deleteMutation.isPending;
  const isMutating = customMutation.isPending;
  const [actionError, setActionError] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("name");
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");

  const sortedCompanies = useMemo(() => {
    const source = query.data?.data ?? [];
    const copy = [...source];
    copy.sort((a, b) => {
      const direction = sortDirection === "asc" ? 1 : -1;

      if (sortKey === "isApproved") {
        return (Number(a.isApproved) - Number(b.isApproved)) * direction;
      }

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

  const handleDelete = async (id: string) => {
    setActionError(null);
    const shouldDelete = window.confirm("Delete this company?");
    if (!shouldDelete) {
      return;
    }

    try {
      await deleteOne({ resource: "companies", id });
      await query.refetch();
    } catch {
      setActionError("Failed to delete company.");
    }
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

  const handleRevokeApproval = async (id: string) => {
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
          <h2 className={styles.title}>Companies</h2>
          <p className={styles.subtitle}>CRUD and moderation actions for all companies.</p>
        </div>
        <Link href="/admin/companies/create" className={styles.linkButton}>
          Create company
        </Link>
      </div>

      {query.isLoading ? <p className={styles.stateText}>Loading companies...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load companies.</p> : null}
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
                  Contact <span className={styles.sortIndicator}>{sortIndicator("contactEmail")}</span>
                </button>
              </th>
              <th>
                <button type="button" className={styles.thButton} onClick={() => toggleSort("isApproved")}>
                  Approved <span className={styles.sortIndicator}>{sortIndicator("isApproved")}</span>
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
                  <span className={company.isApproved ? styles.badgeTrue : styles.badgeFalse}>
                    {company.isApproved ? "Yes" : "No"}
                  </span>
                </td>
                <td>
                  <div className={styles.actionRow}>
                    <Link href={`/admin/companies/${company.id}/edit`} className={styles.linkButton}>
                      Edit
                    </Link>
                    <button
                      type="button"
                      className={styles.dangerButton}
                      disabled={isDeleting || isMutating}
                      onClick={() => {
                        void handleDelete(company.id);
                      }}
                    >
                      Delete
                    </button>
                    {!company.isApproved ? (
                      <button
                        type="button"
                        className={styles.successButton}
                        disabled={isMutating || isDeleting}
                        onClick={() => {
                          void handleApprove(company.id);
                        }}
                      >
                        Approve
                      </button>
                    ) : (
                      <button
                        type="button"
                        className={styles.ghostButton}
                        disabled={isMutating || isDeleting}
                        onClick={() => {
                          void handleRevokeApproval(company.id);
                        }}
                      >
                        Revoke approval
                      </button>
                    )}
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

