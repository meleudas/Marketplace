import type { ComponentProps, ReactNode } from "react";
import { Container } from "./Container";
import { Footer } from "./Footer";
import { Header } from "./Header";
import { PageBackground } from "./PageBackground";
import styles from "./PageLayout.module.css";

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
      <PageBackground />

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
