import type { HTMLAttributes, ReactNode } from "react";
import styles from "./SurfaceCard.module.css";

export interface SurfaceCardProps extends HTMLAttributes<HTMLElement> {
  children: ReactNode;
  className?: string;
}

export function SurfaceCard({ children, className, ...props }: SurfaceCardProps) {
  return (
    <section className={[styles.card, className].filter(Boolean).join(" ")} {...props}>
      {children}
    </section>
  );
}
