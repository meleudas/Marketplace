"use client";

import Link from "next/link";
import { useCustomMutation, useDelete, useList } from "@refinedev/core";
import type { CategoryDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CategoriesListScreen() {
  const { query } = useList<CategoryDto>({ resource: "categories" });
  const { mutateAsync: deleteOne, mutation: deleteMutation } = useDelete();
  const { mutateAsync: runCustom, mutation: customMutation } = useCustomMutation();
  const isDeleting = deleteMutation.isPending;
  const isMutating = customMutation.isPending;

  const categories = query.data?.data ?? [];

  const handleDelete = async (id: number) => {
    await deleteOne({ resource: "categories", id });
    await query.refetch();
  };

  const handleActivate = async (id: number) => {
    await runCustom({
      url: `/admin/categories/${id}/activate`,
      method: "post",
      values: {},
    });
    await query.refetch();
  };

  const handleDeactivate = async (id: number) => {
    await runCustom({
      url: `/admin/categories/${id}/deactivate`,
      method: "post",
      values: {},
    });
    await query.refetch();
  };

  return (
    <section className={styles.panel}>
      <div className={styles.titleRow}>
        <div>
          <h2 className={styles.title}>Categories</h2>
          <p className={styles.subtitle}>CRUD and activation actions for categories.</p>
        </div>
        <Link href="/admin/categories/create" className={styles.linkButton}>
          Create category
        </Link>
      </div>

      {query.isLoading ? <p className={styles.stateText}>Loading categories...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load categories.</p> : null}

      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>ID</th>
              <th>Name</th>
              <th>Slug</th>
              <th>Active</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {categories.map((category) => (
              <tr key={category.id}>
                <td>{category.id}</td>
                <td>{category.name}</td>
                <td>{category.slug}</td>
                <td>
                  <span className={category.isActive ? styles.badgeTrue : styles.badgeFalse}>
                    {category.isActive ? "Yes" : "No"}
                  </span>
                </td>
                <td>
                  <div className={styles.actionRow}>
                    <Link href={`/admin/categories/${category.id}/edit`} className={styles.linkButton}>
                      Edit
                    </Link>
                    <button
                      type="button"
                      className={styles.dangerButton}
                      disabled={isDeleting || isMutating}
                      onClick={() => {
                        void handleDelete(category.id);
                      }}
                    >
                      Delete
                    </button>
                    {category.isActive ? (
                      <button
                        type="button"
                        className={styles.ghostButton}
                        disabled={isMutating || isDeleting}
                        onClick={() => {
                          void handleDeactivate(category.id);
                        }}
                      >
                        Deactivate
                      </button>
                    ) : (
                      <button
                        type="button"
                        className={styles.successButton}
                        disabled={isMutating || isDeleting}
                        onClick={() => {
                          void handleActivate(category.id);
                        }}
                      >
                        Activate
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

