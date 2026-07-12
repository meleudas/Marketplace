import Image from "next/image";
import type { ReactNode } from "react";
import styles from "./SideDecor.module.css";

interface SideDecorShellProps {
  children: ReactNode;
  contentClassName?: string;
}

function SideDecorPanels() {
  return (
    <div className={styles.decor} aria-hidden="true">
      <div className={`${styles.decorPanel} ${styles.decorPanelLeft}`}>
        <span className={styles.accent} />
        <p className={`${styles.quote} ${styles.quoteLeft}`}>Читай більше</p>
        <Image
          className={`${styles.cat} ${styles.catLeft}`}
          src="/about-cat.svg"
          alt=""
          width={127}
          height={172}
        />
      </div>

      <div className={`${styles.decorPanel} ${styles.decorPanelRight}`}>
        <span className={styles.accent} />
        <p className={`${styles.quote} ${styles.quoteRight}`}>Відкривай світ</p>
        <Image
          className={`${styles.cat} ${styles.catRight}`}
          src="/promo-cat.svg"
          alt=""
          width={85}
          height={98}
        />
      </div>
    </div>
  );
}

export function SideDecorShell({ children, contentClassName }: SideDecorShellProps) {
  const contentClass = [styles.content, contentClassName].filter(Boolean).join(" ");

  return (
    <div className={styles.shell}>
      <SideDecorPanels />
      <div className={contentClass}>{children}</div>
    </div>
  );
}
