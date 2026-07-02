import type { ButtonHTMLAttributes, ReactNode } from "react";
import styles from "./Button.module.css";

export type ButtonVariant = "primary" | "secondary" | "gradient" | "dark";

export type ButtonSize = "sm" | "lg" | "icon";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  fullWidth?: boolean;
  leadingIcon?: ReactNode;
  className?: string;
  children?: ReactNode;
}

export function Button({
  variant = "primary",
  size = "sm",
  fullWidth = false,
  leadingIcon,
  className,
  type = "button",
  children,
  ...props
}: ButtonProps) {
  const isIconOnly = size === "icon";
  const isSpread = variant === "dark" && Boolean(leadingIcon);

  const classes = [
    styles.button,
    styles[variant],
    styles[size],
    isSpread ? styles.spread : "",
    isIconOnly ? styles.iconOnly : "",
    fullWidth ? styles.fullWidth : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button type={type} className={classes} {...props}>
      {leadingIcon ? <span className={styles.leadingIcon}>{leadingIcon}</span> : null}
      {children ? <span className={styles.label}>{children}</span> : null}
    </button>
  );
}
