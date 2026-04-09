"use client";

import { useCustomMutation, useList } from "@refinedev/core";
import type { CategoryDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CategoriesActiveScreen() {
  const { query } = useList<CategoryDto>({ resource: "categories-active" });
  const { mutateAsync: runCustom, mutation } = useCustomMutation();
  const isPending = mutation.isPending;

  const categories = query.data?.data ?? [];

  const handleDeactivate = async (id: number) => {
    await runCustom({
      url: `/admin/categories/${id}/deactivate`,
      method: "post",
      values: {},
    });
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

  return (
    <section className={styles.panel}>
      <div className={styles.titleRow}>
        <div>
          <h2 className={styles.title}>Active categories</h2>
          <p className={styles.subtitle}>Quick moderation list for currently active categories.</p>
        </div>
      </div>

      {query.isLoading ? <p className={styles.stateText}>Loading active categories...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load active categories.</p> : null}

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
                    <button
                      type="button"
                      className={styles.ghostButton}
                      disabled={isPending}
                      onClick={() => {
                        void handleDeactivate(category.id);
                      }}
                    >
                      Deactivate
                    </button>
                    <button
                      type="button"
                      className={styles.successButton}
                      disabled={isPending}
                      onClick={() => {
                        void handleActivate(category.id);
                      }}
                    >
                      Activate
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

