import type { ButtonHTMLAttributes, MouseEvent, ReactNode } from "react";
import styles from "./Button.module.css";

export type ButtonVariant = "primary" | "secondary" | "gradient" | "dark" | "filter";

export type ButtonSize = "sm" | "lg" | "icon";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  fullWidth?: boolean;
  selectable?: boolean;
  selected?: boolean;
  leadingIcon?: ReactNode;
  className?: string;
  children?: ReactNode;
}

export function Button({
  variant = "primary",
  size = "sm",
  fullWidth = false,
  selectable = false,
  selected = false,
  leadingIcon,
  className,
  type = "button",
  children,
  onClick,
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
    selectable ? styles.selectable : "",
    selectable && selected ? styles.selected : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  const handleClick = (event: MouseEvent<HTMLButtonElement>) => {
    if (selectable && selected && variant === "dark") {
      const target = event.currentTarget;
      target.blur();
      requestAnimationFrame(() => target.blur());
    }

    onClick?.(event);
  };

  return (
    <button type={type} className={classes} onClick={handleClick} {...props}>
      {leadingIcon ? <span className={styles.leadingIcon}>{leadingIcon}</span> : null}
      {children ? <span className={styles.label}>{children}</span> : null}
    </button>
  );
}
