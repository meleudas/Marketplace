import type { InputHTMLAttributes, ReactNode } from "react";
import styles from "./TextField.module.css";

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
  leadingIcon?: ReactNode;
  trailingIcon?: ReactNode;
}

export function TextField({
  label,
  hint,
  error,
  leadingIcon,
  trailingIcon,
  id,
  className,
  ...props
}: TextFieldProps) {
  const inputId = id ?? props.name;
  const hasError = Boolean(error);

  const fieldClasses = [
    styles.field,
    leadingIcon ? styles.hasLeading : "",
    trailingIcon ? styles.hasTrailing : "",
    hasError ? styles.fieldError : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={[styles.wrapper, className ?? ""].filter(Boolean).join(" ")}>
      {label ? (
        <label className={styles.label} htmlFor={inputId}>
          {label}
        </label>
      ) : null}

      <div className={styles.control}>
        {leadingIcon ? (
          <span className={`${styles.adornment} ${styles.leading}`} aria-hidden="true">
            {leadingIcon}
          </span>
        ) : null}

        <input id={inputId} className={fieldClasses} {...props} />

        {trailingIcon ? (
          <span className={`${styles.adornment} ${styles.trailing}`} aria-hidden="true">
            {trailingIcon}
          </span>
        ) : null}
      </div>

      {error ? (
        <span className={styles.error}>{error}</span>
      ) : hint ? (
        <span className={styles.hint}>{hint}</span>
      ) : null}
    </div>
  );
}
