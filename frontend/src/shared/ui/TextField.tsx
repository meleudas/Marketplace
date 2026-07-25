import { forwardRef, type InputHTMLAttributes, type ReactNode } from "react";
import styles from "./TextField.module.css";

export type TextFieldKind = "text" | "email" | "password" | "tel" | "number" | "search" | "url";

const KIND_PRESETS: Record<
  TextFieldKind,
  Pick<InputHTMLAttributes<HTMLInputElement>, "type" | "inputMode" | "autoComplete" | "pattern">
> = {
  text: { type: "text", inputMode: "text", autoComplete: "off" },
  email: { type: "email", inputMode: "email", autoComplete: "email" },
  password: { type: "password", autoComplete: "current-password" },
  tel: { type: "tel", inputMode: "tel", autoComplete: "tel" },
  number: { type: "text", inputMode: "numeric", pattern: "[0-9]*", autoComplete: "off" },
  search: { type: "search", inputMode: "search", autoComplete: "off" },
  url: { type: "url", inputMode: "url", autoComplete: "url" },
};

export interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label?: string;
  hint?: string;
  error?: string;
  kind?: TextFieldKind;
  type?: InputHTMLAttributes<HTMLInputElement>["type"];
  leadingIcon?: ReactNode;
  trailingIcon?: ReactNode;
  /** Storybook / demos: keep hover border without relying on pointer :hover. */
  forceHover?: boolean;
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(function TextField(
  {
    label,
    hint,
    error,
    kind = "text",
    type,
    leadingIcon,
    trailingIcon,
    forceHover = false,
    id,
    className,
    inputMode,
    autoComplete,
    pattern,
    ...props
  },
  ref,
) {
  const preset = KIND_PRESETS[kind];
  const inputId = id ?? props.name;
  const hasError = Boolean(error);
  const describedBy = error
    ? `${inputId}-error`
    : hint
      ? `${inputId}-hint`
      : undefined;

  const fieldClasses = [
    styles.field,
    leadingIcon ? styles.hasLeading : "",
    trailingIcon ? styles.hasTrailing : "",
    hasError ? styles.fieldError : "",
  ]
    .filter(Boolean)
    .join(" ");

  const controlClasses = [styles.control, forceHover ? styles.controlForceHover : ""]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={[styles.wrapper, className ?? ""].filter(Boolean).join(" ")}>
      {label ? (
        <label className={styles.label} htmlFor={inputId}>
          {label}
        </label>
      ) : null}

      <div className={controlClasses} data-testid="text-field-control">
        {leadingIcon ? (
          <span className={`${styles.adornment} ${styles.leading}`} aria-hidden="true">
            {leadingIcon}
          </span>
        ) : null}

        <input
          ref={ref}
          id={inputId}
          className={fieldClasses}
          type={type ?? preset.type}
          inputMode={inputMode ?? preset.inputMode}
          autoComplete={autoComplete ?? preset.autoComplete}
          pattern={pattern ?? preset.pattern}
          aria-invalid={hasError || undefined}
          aria-describedby={describedBy}
          {...props}
        />

        {trailingIcon ? (
          <span className={`${styles.adornment} ${styles.trailing}`} aria-hidden="true">
            {trailingIcon}
          </span>
        ) : null}
      </div>

      {error ? (
        <span id={`${inputId}-error`} className={styles.error} role="alert">
          {error}
        </span>
      ) : hint ? (
        <span id={`${inputId}-hint`} className={styles.hint}>
          {hint}
        </span>
      ) : null}
    </div>
  );
});
