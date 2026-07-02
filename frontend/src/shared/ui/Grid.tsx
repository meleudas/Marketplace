import type { CSSProperties, HTMLAttributes } from "react";
import styles from "./Grid.module.css";

type GridGap = "sm" | "md" | "lg" | "grid";
type GridLayout = "auto" | "columns";

interface GridProps extends HTMLAttributes<HTMLDivElement> {
  /** `auto` — responsive minmax columns; `columns` — fixed mobile grid (5 cols). */
  layout?: GridLayout;
  /** Minimum column width when layout is `auto`. */
  minColumnWidth?: string;
  gap?: GridGap;
}

export function Grid({
  layout = "auto",
  minColumnWidth = "16rem",
  gap = "md",
  className,
  style,
  children,
  ...props
}: GridProps) {
  const classes = [
    styles.grid,
    layout === "columns" ? styles.columns : styles.auto,
    gap === "grid" ? styles.gridGap : styles[gap],
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  const gridStyle = {
    ...style,
    ...(layout === "auto" ? { ["--ui-grid-min" as string]: minColumnWidth } : {}),
  } as CSSProperties;

  return (
    <div className={classes} style={gridStyle} {...props}>
      {children}
    </div>
  );
}
