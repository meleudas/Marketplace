"use client";

import styles from "@/features/admin/screens/AdminScreens.module.css";
import type { CompanyFormState } from "@/features/admin/screens/companies.form";

interface CompanyFormProps {
  value: CompanyFormState;
  disabled?: boolean;
  onChange: (next: CompanyFormState) => void;
}

export function CompanyForm({ value, disabled = false, onChange }: CompanyFormProps) {
  const update = (key: keyof CompanyFormState, nextValue: string) => {
    onChange({
      ...value,
      [key]: nextValue,
    });
  };

  return (
    <div className={styles.formGrid}>
      <label className={styles.field}>
        Name
        <input
          className={styles.input}
          value={value.name}
          onChange={(event) => update("name", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Slug
        <input
          className={styles.input}
          value={value.slug}
          onChange={(event) => update("slug", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Description
        <textarea
          className={styles.textarea}
          value={value.description}
          onChange={(event) => update("description", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Image URL
        <input
          className={styles.input}
          value={value.imageUrl}
          onChange={(event) => update("imageUrl", event.target.value)}
          disabled={disabled}
        />
      </label>

      <label className={styles.field}>
        Contact email
        <input
          className={styles.input}
          value={value.contactEmail}
          onChange={(event) => update("contactEmail", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Contact phone
        <input
          className={styles.input}
          value={value.contactPhone}
          onChange={(event) => update("contactPhone", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Street
        <input
          className={styles.input}
          value={value.street}
          onChange={(event) => update("street", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        City
        <input
          className={styles.input}
          value={value.city}
          onChange={(event) => update("city", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        State
        <input
          className={styles.input}
          value={value.state}
          onChange={(event) => update("state", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Postal code
        <input
          className={styles.input}
          value={value.postalCode}
          onChange={(event) => update("postalCode", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={styles.field}>
        Country
        <input
          className={styles.input}
          value={value.country}
          onChange={(event) => update("country", event.target.value)}
          disabled={disabled}
          required
        />
      </label>

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Meta raw
        <textarea
          className={styles.textarea}
          value={value.metaRaw}
          onChange={(event) => update("metaRaw", event.target.value)}
          disabled={disabled}
        />
      </label>
    </div>
  );
}

