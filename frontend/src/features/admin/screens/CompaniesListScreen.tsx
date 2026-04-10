"use client";

import Link from "next/link";
import { useCustomMutation, useDelete, useList } from "@refinedev/core";
import type { CompanyDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CompaniesListScreen() {
  const { query } = useList<CompanyDto>({ resource: "companies" });
  const { mutateAsync: deleteOne, mutation: deleteMutation } = useDelete();
  const { mutateAsync: runCustom, mutation: customMutation } = useCustomMutation();
  const isDeleting = deleteMutation.isPending;
  const isMutating = customMutation.isPending;

  const companies = query.data?.data ?? [];

  const handleDelete = async (id: string) => {
    await deleteOne({ resource: "companies", id });
    await query.refetch();
  };

  const handleApprove = async (id: string) => {
    await runCustom({
      url: `/admin/companies/${id}/approve`,
      method: "post",
      values: {},
    });
    await query.refetch();
  };

  const handleRevokeApproval = async (id: string) => {
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
          <h2 className={styles.title}>Companies</h2>
          <p className={styles.subtitle}>CRUD and moderation actions for all companies.</p>
        </div>
        <Link href="/admin/companies/create" className={styles.linkButton}>
          Create company
        </Link>
      </div>

      {query.isLoading ? <p className={styles.stateText}>Loading companies...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load companies.</p> : null}

      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Slug</th>
              <th>Contact</th>
              <th>Approved</th>
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

