import type { HTMLAttributes } from "react";
import styles from "./Spinner.module.css";

export type SpinnerSize = "sm" | "lg";

export interface SpinnerProps extends HTMLAttributes<HTMLSpanElement> {
  size?: SpinnerSize;
}

export function Spinner({
  size = "sm",
  className,
  "aria-label": ariaLabel = "Завантаження",
  ...props
}: SpinnerProps) {
  const isDecorative = props["aria-hidden"] === true;

  return (
    <span
      className={[styles.spinner, styles[size], className].filter(Boolean).join(" ")}
      role={isDecorative ? undefined : "status"}
      aria-label={isDecorative ? undefined : ariaLabel}
      {...props}
    />
  );
}
