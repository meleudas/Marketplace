"use client";

import { useCustomMutation, useList } from "@refinedev/core";
import type { CompanyDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CompaniesPendingScreen() {
  const { query } = useList<CompanyDto>({ resource: "companies-pending" });
  const { mutateAsync: runCustom, mutation } = useCustomMutation();
  const isPending = mutation.isPending;

  const companies = query.data?.data ?? [];

  const handleApprove = async (id: string) => {
    await runCustom({
      url: `/admin/companies/${id}/approve`,
      method: "post",
      values: {},
    });
    await query.refetch();
  };

  const handleRevoke = async (id: string) => {
    await runCustom({
      url: `/admin/companies/${id}/revoke-approval`,
      method: "post",
      values: {},
    });
    await query.refetch();
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

      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Slug</th>
              <th>Contact email</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {companies.map((company) => (
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

