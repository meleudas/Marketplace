"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { type BaseRecord, type HttpError, useCreate } from "@refinedev/core";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "@refinedev/react-hook-form";
import { CategoryForm } from "@/features/admin/screens/CategoryForm";
import { buildCreateCategoryPayload } from "@/features/admin/screens/categories.form";
import {
  categoryFormSchema,
  defaultCategoryFormValues,
  type CategoryFormValues,
} from "@/features/admin/validation/category-form.schema";
import {
  applyServerFieldErrors,
  parseAdminFormError,
} from "@/features/admin/validation/admin-form-errors";
import styles from "@/features/admin/screens/AdminScreens.module.css";

export function CategoriesCreateScreen() {
  const router = useRouter();
  const { mutateAsync, mutation } = useCreate();
  const isPending = mutation.isPending;
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    control,
    handleSubmit,
    setError: setFormError,
    formState: { errors, isSubmitting },
  } = useForm<BaseRecord, HttpError, CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: defaultCategoryFormValues,
  });

  const onSubmit = async (values: CategoryFormValues) => {
    setError(null);

    try {
      await mutateAsync({
        resource: "categories",
        values: buildCreateCategoryPayload(values),
      });

      router.push("/admin/categories");
    } catch (requestError) {
      const parsed = parseAdminFormError(requestError, "Failed to create category.");
      applyServerFieldErrors(setFormError, parsed.fieldErrors);
      setError(parsed.message);
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Create category</h2>
      <p className={styles.subtitle}>Create a category using admin API.</p>

      <form onSubmit={handleSubmit(onSubmit)}>
        <CategoryForm
          register={register}
          control={control}
          errors={errors}
          includeIsActive
          disabled={isPending || isSubmitting}
        />

        <div className={styles.formActions}>
          <button type="submit" className={styles.button} disabled={isPending || isSubmitting}>
            {isPending || isSubmitting ? "Creating..." : "Create"}
          </button>
          <button
            type="button"
            className={styles.ghostButton}
            disabled={isPending || isSubmitting}
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

