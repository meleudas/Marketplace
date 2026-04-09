"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useOne, useUpdate } from "@refinedev/core";
import type { CategoryDto } from "@/features/admin/model/admin.types";
import { CategoryForm } from "@/features/admin/screens/CategoryForm";
import {
  buildUpdateCategoryPayload,
  categoryDtoToFormState,
  defaultCategoryFormState,
  type CategoryFormState,
} from "@/features/admin/screens/categories.form";
import styles from "@/features/admin/screens/AdminScreens.module.css";

interface CategoriesEditScreenProps {
  id: number;
}

export function CategoriesEditScreen({ id }: CategoriesEditScreenProps) {
  const router = useRouter();
  const { mutateAsync, mutation } = useUpdate();
  const isPending = mutation.isPending;
  const { query } = useOne<CategoryDto>({
    resource: "categories",
    id,
  });

  const [editedForm, setEditedForm] = useState<CategoryFormState | null>(null);
  const [error, setError] = useState<string | null>(null);

  const form =
    editedForm ??
    (query.data?.data ? categoryDtoToFormState(query.data.data) : defaultCategoryFormState);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    try {
      await mutateAsync({
        resource: "categories",
        id,
        values: buildUpdateCategoryPayload(form),
      });

      router.push("/admin/categories");
    } catch {
      setError("Failed to update category.");
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Edit category</h2>
      <p className={styles.subtitle}>Update category details.</p>

      {query.isLoading ? <p className={styles.stateText}>Loading category...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load category.</p> : null}

      {!query.isLoading && !query.isError ? (
        <form onSubmit={handleSubmit}>
          <CategoryForm value={form} onChange={setEditedForm} disabled={isPending} />

          <div className={styles.formActions}>
            <button type="submit" className={styles.button} disabled={isPending}>
              {isPending ? "Saving..." : "Save"}
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
      ) : null}

      {error ? <p className={styles.error}>{error}</p> : null}
    </section>
  );
}


