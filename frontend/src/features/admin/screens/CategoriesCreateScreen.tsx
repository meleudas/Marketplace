"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useCreate } from "@refinedev/core";
import { CategoryForm } from "@/features/admin/screens/CategoryForm";
import {
  buildCreateCategoryPayload,
  defaultCategoryFormState,
  type CategoryFormState,
} from "@/features/admin/screens/categories.form";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CategoriesCreateScreen() {
  const router = useRouter();
  const { mutateAsync, mutation } = useCreate();
  const isPending = mutation.isPending;
  const [form, setForm] = useState<CategoryFormState>(defaultCategoryFormState);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    try {
      await mutateAsync({
        resource: "categories",
        values: buildCreateCategoryPayload(form),
      });

      router.push("/admin/categories");
    } catch {
      setError("Failed to create category.");
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Create category</h2>
      <p className={styles.subtitle}>Create a category using admin API.</p>

      <form onSubmit={handleSubmit}>
        <CategoryForm value={form} onChange={setForm} disabled={isPending} />

        <div className={styles.formActions}>
          <button type="submit" className={styles.button} disabled={isPending}>
            {isPending ? "Creating..." : "Create"}
          </button>
          <button
            type="button"
            className={styles.ghostButton}
            disabled={isPending}
            onClick={() => router.push("/admin/categories")}
          >
            Cancel
          </button>
        </div>
      </form>

      {error ? <p className={styles.error}>{error}</p> : null}
    </section>
  );
}

