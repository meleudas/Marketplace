import type { ComponentProps, ReactNode } from "react";
import { Container } from "./Container";
import { Footer } from "./Footer";
import { Header } from "./Header";
import { PageBackground } from "./PageBackground";
import styles from "./PageLayout.module.css";

interface PageLayoutProps {
  children: ReactNode;
  className?: string;
  showHeader?: boolean;
  headerProps?: ComponentProps<typeof Header>;
  footerProps?: ComponentProps<typeof Footer>;
}

export function PageLayout({
  children,
  className,
  showHeader = true,
  headerProps,
  footerProps,
}: PageLayoutProps) {
  const mainClassName = [styles.main, className].filter(Boolean).join(" ");

  return (
    <div className={styles.page}>
      <PageBackground />

      <div className={styles.pageContent}>
        {showHeader ? <Header {...headerProps} /> : null}
        <Container as="main" className={mainClassName}>
          {children}
        </Container>
        <Footer {...footerProps} />
      </div>
    </div>
  );
}
