"use client";

import styles from "@/features/admin/screens/AdminScreens.module.css";
import type { CategoryFormState } from "@/features/admin/screens/categories.form";

interface CategoryFormProps {
  value: CategoryFormState;
  disabled?: boolean;
  onChange: (next: CategoryFormState) => void;
}

export function CategoryForm({ value, disabled = false, onChange }: CategoryFormProps) {
  const updateString = (key: keyof Omit<CategoryFormState, "isActive">, nextValue: string) => {
    onChange({
      ...value,
      [key]: nextValue,
    });
  };

  const updateBoolean = (nextValue: boolean) => {
    onChange({
      ...value,
      isActive: nextValue,
    });
  };

  return (
    <div className={styles.formGrid}>
      <label className={styles.field}>
        Name
        <input
          className={styles.input}
          value={value.name}
          onChange={(event) => updateString("name", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Slug
        <input
          className={styles.input}
          value={value.slug}
          onChange={(event) => updateString("slug", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Image URL
        <input
          className={styles.input}
          value={value.imageUrl}
          onChange={(event) => updateString("imageUrl", event.target.value)}
          disabled={disabled}
        />
      </label>

      <label className={styles.field}>
        Parent category ID
        <input
          className={styles.input}
          value={value.parentCategoryId}
          onChange={(event) => updateString("parentCategoryId", event.target.value)}
          disabled={disabled}
          placeholder="empty for root"
        />
      </label>

      <label className={styles.field}>
        Sort order
        <input
          className={styles.input}
          value={value.sortOrder}
          onChange={(event) => updateString("sortOrder", event.target.value)}
          disabled={disabled}
        />
      </label>

      <label className={styles.field}>
        Active
        <input
          type="checkbox"
          checked={value.isActive}
          onChange={(event) => updateBoolean(event.target.checked)}
          disabled={disabled}
        />
      </label>

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Description
        <textarea
          className={styles.textarea}
          value={value.description}
          onChange={(event) => updateString("description", event.target.value)}
          disabled={disabled}
        />
      </label>

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Meta raw
        <textarea
          className={styles.textarea}
          value={value.metaRaw}
          onChange={(event) => updateString("metaRaw", event.target.value)}
          disabled={disabled}
        />
      </label>
    </div>
  );
}

