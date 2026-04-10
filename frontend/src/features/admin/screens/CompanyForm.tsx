"use client";

import type { FieldErrors, UseFormRegister } from "react-hook-form";
import styles from "@/features/admin/screens/AdminScreens.module.css";
import type { CompanyFormValues } from "@/features/admin/validation/company-form.schema";

interface CompanyFormProps {
  register: UseFormRegister<CompanyFormValues>;
  errors: FieldErrors<CompanyFormValues>;
  disabled?: boolean;
}

export function CompanyForm({ register, errors, disabled = false }: CompanyFormProps) {

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

      <label className={`${styles.field} ${styles.fieldFull}`}>
        Description
        <textarea
          className={`${styles.textarea} ${errors.description ? styles.inputInvalid : ""}`}
          {...register("description")}
          disabled={disabled}
        />
        {errors.description ? <span className={styles.fieldError}>{errors.description.message}</span> : null}
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
        Contact email
        <input
          className={`${styles.input} ${errors.contactEmail ? styles.inputInvalid : ""}`}
          {...register("contactEmail")}
          disabled={disabled}
        />
        {errors.contactEmail ? <span className={styles.fieldError}>{errors.contactEmail.message}</span> : null}
      </label>

      <label className={styles.field}>
        Contact phone
        <input
          className={`${styles.input} ${errors.contactPhone ? styles.inputInvalid : ""}`}
          {...register("contactPhone")}
          disabled={disabled}
        />
        {errors.contactPhone ? <span className={styles.fieldError}>{errors.contactPhone.message}</span> : null}
      </label>

      <label className={styles.field}>
        Street
        <input
          className={`${styles.input} ${errors.address?.street ? styles.inputInvalid : ""}`}
          {...register("address.street")}
          disabled={disabled}
        />
        {errors.address?.street ? <span className={styles.fieldError}>{errors.address.street.message}</span> : null}
      </label>

      <label className={styles.field}>
        City
        <input
          className={`${styles.input} ${errors.address?.city ? styles.inputInvalid : ""}`}
          {...register("address.city")}
          disabled={disabled}
        />
        {errors.address?.city ? <span className={styles.fieldError}>{errors.address.city.message}</span> : null}
      </label>

      <label className={styles.field}>
        State
        <input
          className={`${styles.input} ${errors.address?.state ? styles.inputInvalid : ""}`}
          {...register("address.state")}
          disabled={disabled}
        />
        {errors.address?.state ? <span className={styles.fieldError}>{errors.address.state.message}</span> : null}
      </label>

      <label className={styles.field}>
        Postal code
        <input
          className={`${styles.input} ${errors.address?.postalCode ? styles.inputInvalid : ""}`}
          {...register("address.postalCode")}
          disabled={disabled}
        />
        {errors.address?.postalCode ? <span className={styles.fieldError}>{errors.address.postalCode.message}</span> : null}
      </label>

      <label className={styles.field}>
        Country
        <input
          className={`${styles.input} ${errors.address?.country ? styles.inputInvalid : ""}`}
          {...register("address.country")}
          disabled={disabled}
        />
        {errors.address?.country ? <span className={styles.fieldError}>{errors.address.country.message}</span> : null}
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

