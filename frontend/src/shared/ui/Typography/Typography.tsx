import type { ElementType, ReactNode } from "react";
import styles from "./Typography.module.css";

export type TypographyVariant =
  | "h1"
  | "h2"
  | "h3"
  | "body1"
  | "body2"
  | "body3"
  | "body4";

const defaultTags: Record<TypographyVariant, ElementType> = {
  h1: "h1",
  h2: "h2",
  h3: "h3",
  body1: "p",
  body2: "p",
  body3: "p",
  body4: "p",
};

export interface TypographyProps {
  variant?: TypographyVariant;
  children: ReactNode;
  className?: string;
  as?: ElementType;
}

export function Typography({
  variant = "body1",
  children,
  className,
  as,
}: TypographyProps) {
  const Component = as ?? defaultTags[variant];
  const classes = [styles[variant], className].filter(Boolean).join(" ");

  return <Component className={classes}>{children}</Component>;
}
