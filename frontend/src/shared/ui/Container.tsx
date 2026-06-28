import type { ElementType, HTMLAttributes } from "react";
import styles from "./Container.module.css";

interface ContainerProps extends HTMLAttributes<HTMLElement> {
  as?: ElementType;
}

export function Container({ as, className, children, ...props }: ContainerProps) {
  const Component = as ?? "div";
  const classes = [styles.container, className ?? ""].filter(Boolean).join(" ");

  return (
    <Component className={classes} {...props}>
      {children}
    </Component>
  );
}
