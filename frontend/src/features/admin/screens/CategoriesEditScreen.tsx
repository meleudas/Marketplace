"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { type BaseRecord, type HttpError, useOne, useUpdate } from "@refinedev/core";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "@refinedev/react-hook-form";
import type { CategoryDto } from "@/features/admin/model/admin.types";
import { CategoryForm } from "@/features/admin/screens/CategoryForm";
import {
  buildUpdateCategoryPayload,
  categoryDtoToFormValues,
} from "@/features/admin/screens/categories.form";
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

  const [error, setError] = useState<string | null>(null);
  const {
    register,
    control,
    handleSubmit,
    reset,
    setError: setFormError,
    formState: { errors, isSubmitting },
  } = useForm<BaseRecord, HttpError, CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: defaultCategoryFormValues,
  });

  useEffect(() => {
    if (!query.data?.data) {
      return;
    }

    reset(categoryDtoToFormValues(query.data.data));
  }, [query.data?.data, reset]);

  const onSubmit = async (values: CategoryFormValues) => {
    setError(null);

    try {
      await mutateAsync({
        resource: "categories",
        id,
        values: buildUpdateCategoryPayload(values),
      });

      router.push("/admin/categories");
    } catch (requestError) {
      const parsed = parseAdminFormError(requestError, "Failed to update category.");
      applyServerFieldErrors(setFormError, parsed.fieldErrors);
      setError(parsed.message);
    }
  };

  return (
    <section className={styles.panel}>
      <h2 className={styles.title}>Edit category</h2>
      <p className={styles.subtitle}>Update category details.</p>

      {query.isLoading ? <p className={styles.stateText}>Loading category...</p> : null}
      {query.isError ? <p className={styles.error}>Failed to load category.</p> : null}

      {!query.isLoading && !query.isError ? (
        <form onSubmit={handleSubmit(onSubmit)}>
          <CategoryForm
            register={register}
            control={control}
            errors={errors}
            includeIsActive={false}
            disabled={isPending || isSubmitting}
          />

          <div className={styles.formActions}>
            <button type="submit" className={styles.button} disabled={isPending || isSubmitting}>
              {isPending || isSubmitting ? "Saving..." : "Save"}
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
      ) : null}

      {error ? <p className={styles.error}>{error}</p> : null}
    </section>
  );
}


