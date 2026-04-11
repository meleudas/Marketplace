"use client";

import { useState } from "react";
import { useMemo } from "react";
import { useCustomMutation, useList } from "@refinedev/core";
import type { CategoryDto } from "@/features/admin/model/admin.types";
import styles from "@/features/admin/screens/AdminScreens.module.css";

type SortKey = "id" | "name" | "slug" | "isActive";
type SortDirection = "asc" | "desc";

export function CategoriesActiveScreen() {
  const { query } = useList<CategoryDto>({ resource: "categories-active" });
  const { mutateAsync: runCustom, mutation } = useCustomMutation();
  const isPending = mutation.isPending;
  const [actionError, setActionError] = useState<string | null>(null);
  const [sortKey, setSortKey] = useState<SortKey>("name");
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");

  const sortedCategories = useMemo(() => {
    const source = query.data?.data ?? [];
    const copy = [...source];
    copy.sort((a, b) => {
      const direction = sortDirection === "asc" ? 1 : -1;

      if (sortKey === "id") {
        return (a.id - b.id) * direction;
      }

      if (sortKey === "isActive") {
        return (Number(a.isActive) - Number(b.isActive)) * direction;
      }

      const left = a[sortKey].toString().toLowerCase();
      const right = b[sortKey].toString().toLowerCase();
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

  const handleDeactivate = async (id: number) => {
    setActionError(null);
    const shouldDeactivate = window.confirm("Deactivate this category?");
    if (!shouldDeactivate) {
      return;
    }

    try {
      await runCustom({
        url: `/admin/categories/${id}/deactivate`,
        method: "post",
        values: {},
      });
      await query.refetch();
    } catch {
      setActionError("Failed to deactivate category.");
    }
  };

  const handleActivate = async (id: number) => {
    setActionError(null);
    const shouldActivate = window.confirm("Activate this category?");
    if (!shouldActivate) {
      return;
    }

    try {
      await runCustom({
        url: `/admin/categories/${id}/activate`,
        method: "post",
        values: {},
      });
      await query.refetch();
    } catch {
      setActionError("Failed to activate category.");
    }
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
      {actionError ? <p className={styles.error}>{actionError}</p> : null}

      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>
                <button type="button" className={styles.thButton} onClick={() => toggleSort("id")}>
                  ID <span className={styles.sortIndicator}>{sortIndicator("id")}</span>
                </button>
              </th>
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
                <button type="button" className={styles.thButton} onClick={() => toggleSort("isActive")}>
                  Active <span className={styles.sortIndicator}>{sortIndicator("isActive")}</span>
                </button>
              </th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sortedCategories.map((category) => (
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

