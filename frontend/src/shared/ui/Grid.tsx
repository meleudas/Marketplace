import type { CSSProperties, HTMLAttributes } from "react";
import styles from "./Grid.module.css";

type GridGap = "sm" | "md" | "lg";

interface GridProps extends HTMLAttributes<HTMLDivElement> {
  /** Minimum column width; grid auto-fills responsively. */
  minColumnWidth?: string;
  gap?: GridGap;
}

export function Grid({
  minColumnWidth = "16rem",
  gap = "md",
  className,
  style,
  children,
  ...props
}: GridProps) {
  const classes = [styles.grid, styles[gap], className ?? ""].filter(Boolean).join(" ");
  const gridStyle = {
    ...style,
    ["--ui-grid-min" as string]: minColumnWidth,
  } as CSSProperties;

  return (
    <div className={classes} style={gridStyle} {...props}>
      {children}
    </div>
  );
}
