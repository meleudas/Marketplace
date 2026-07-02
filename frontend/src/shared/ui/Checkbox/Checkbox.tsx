import { forwardRef, type InputHTMLAttributes, type ReactNode } from "react";
import { CheckboxCheckedIcon, CheckboxIcon } from "../icons";
import styles from "./Checkbox.module.css";

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label?: ReactNode;
  onCheckedChange?: (checked: boolean) => void;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(function Checkbox(
  { label, className, disabled, id, onChange, onCheckedChange, ...props },
  ref,
) {
  const inputId = id ?? props.name;

  return (
    <label
      className={[styles.root, disabled ? styles.disabled : "", className ?? ""].filter(Boolean).join(" ")}
    >
      <input
        ref={ref}
        id={inputId}
        type="checkbox"
        className={styles.input}
        disabled={disabled}
        onChange={(event) => {
          onChange?.(event);
          onCheckedChange?.(event.target.checked);
        }}
        {...props}
      />

      <span className={styles.control}>
        <CheckboxIcon className={styles.uncheckedIcon} />
        <span className={styles.checked}>
          <CheckboxCheckedIcon className={styles.checkedIcon} />
        </span>
      </span>

      {label ? <span className={styles.label}>{label}</span> : null}
    </label>
  );
});
