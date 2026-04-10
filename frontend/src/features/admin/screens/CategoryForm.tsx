"use client";

import { Controller, type Control, type FieldErrors, type UseFormRegister } from "react-hook-form";
import styles from "@/features/admin/screens/AdminScreens.module.css";
import type { CategoryFormValues } from "@/features/admin/validation/category-form.schema";

interface CategoryFormProps {
  register: UseFormRegister<CategoryFormValues>;
  control: Control<CategoryFormValues>;
  errors: FieldErrors<CategoryFormValues>;
  includeIsActive?: boolean;
  disabled?: boolean;
}

export function CategoryForm({
  register,
  control,
  errors,
  includeIsActive = true,
  disabled = false,
}: CategoryFormProps) {

  return (
    <div className={styles.formGrid}>
      <label className={styles.field}>
        Name
        <input
          className={`${styles.input} ${errors.name ? styles.inputInvalid : ""}`}
          {...register("name")}
          disabled={disabled}
        />
        {errors.name ? <span className={styles.fieldError}>{errors.name.message}</span> : null}
      </label>

      <label className={styles.field}>
        Slug
        <input
          className={`${styles.input} ${errors.slug ? styles.inputInvalid : ""}`}
          {...register("slug")}
          disabled={disabled}
        />
        {errors.slug ? <span className={styles.fieldError}>{errors.slug.message}</span> : null}
      </label>

      <label className={styles.field}>
        Image URL
        <input
          className={styles.input}
          {...register("imageUrl")}
          disabled={disabled}
        />
      </label>

      <label className={styles.field}>
        Parent category ID
        <Controller
          name="parentCategoryId"
          control={control}
          render={({ field }) => (
            <input
              type="number"
              className={`${styles.input} ${errors.parentCategoryId ? styles.inputInvalid : ""}`}
              value={field.value ?? ""}
              onChange={(event) => {
                const value = event.target.value;
                field.onChange(value === "" ? null : Number(value));
              }}
              onBlur={field.onBlur}
              disabled={disabled}
              placeholder="empty for root"
            />
          )}
        />
        {errors.parentCategoryId ? (
          <span className={styles.fieldError}>{errors.parentCategoryId.message}</span>
        ) : null}
      </label>

      <label className={styles.field}>
        Sort order
        <input
          type="number"
          className={styles.input}
          {...register("sortOrder", { valueAsNumber: true })}
          disabled={disabled}
        />
        {errors.sortOrder ? <span className={styles.fieldError}>{errors.sortOrder.message}</span> : null}
      </label>

      {includeIsActive ? (
        <label className={styles.field}>
          Active
          <input type="checkbox" {...register("isActive")} disabled={disabled} />
        </label>
      ) : null}

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Description
        <textarea
          className={styles.textarea}
          {...register("description")}
          disabled={disabled}
        />
      </label>

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Meta raw
        <textarea
          className={styles.textarea}
          {...register("metaRaw")}
          disabled={disabled}
        />
      </label>
    </div>
  );
}

