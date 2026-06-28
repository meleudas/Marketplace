import type { ButtonHTMLAttributes, ReactNode } from "react";
import styles from "./IconButton.module.css";

type IconButtonVariant = "solid" | "ghost";
type IconButtonSize = "sm" | "md" | "lg";

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Accessible label, required since the button is icon-only. */
  label: string;
  icon: ReactNode;
  variant?: IconButtonVariant;
  size?: IconButtonSize;
}

export function IconButton({
  label,
  icon,
  variant = "ghost",
  size = "md",
  className,
  type = "button",
  ...props
}: IconButtonProps) {
  const classes = [styles.iconButton, styles[variant], styles[size], className ?? ""]
    .filter(Boolean)
    .join(" ");

  return (
    <button type={type} className={classes} aria-label={label} title={label} {...props}>
      <span className={styles.icon} aria-hidden="true">
        {icon}
      </span>
    </button>
  );
}
