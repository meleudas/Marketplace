import type { ComponentProps, ReactNode } from "react";
import { Container } from "./Container";
import { Footer } from "./Footer";
import { Header } from "./Header";
import styles from "./PageLayout.module.css";

const PAGE_GLOW_STEP_PX = 480;
const PAGE_GLOW_START_PX = 126;
const PAGE_GLOW_COUNT = 32;

interface PageLayoutProps {
  children: ReactNode;
  className?: string;
  headerProps?: ComponentProps<typeof Header>;
  footerProps?: ComponentProps<typeof Footer>;
}

export function PageLayout({ children, className, headerProps, footerProps }: PageLayoutProps) {
  const mainClassName = [styles.main, className].filter(Boolean).join(" ");

  return (
    <div className={styles.page}>
      <div className={styles.pageBackground} aria-hidden="true">
        {Array.from({ length: PAGE_GLOW_COUNT }, (_, index) => (
          <span
            key={index}
            className={`${styles.pageGlow} ${index % 2 === 0 ? styles.pageGlowLeft : styles.pageGlowRight}`}
            style={{ top: index * PAGE_GLOW_STEP_PX + PAGE_GLOW_START_PX }}
          />
        ))}
      </div>

      <div className={styles.pageContent}>
        <Header {...headerProps} />
        <Container as="main" className={mainClassName}>
          {children}
        </Container>
        <Footer {...footerProps} />
      </div>
    </div>
  );
}
