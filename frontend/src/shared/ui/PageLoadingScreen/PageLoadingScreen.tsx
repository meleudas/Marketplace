import { Spinner } from "@/shared/ui/Spinner";
import styles from "./PageLoadingScreen.module.css";

interface PageLoadingScreenProps {
  /** Full-viewport overlay (client navigations) vs full-page fallback (`loading.tsx`). */
  variant?: "overlay" | "page";
}

export function PageLoadingScreen({ variant = "page" }: PageLoadingScreenProps) {
  return (
    <div
      className={variant === "overlay" ? styles.overlay : styles.inline}
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <Spinner size="lg" aria-label="Завантаження сторінки" />
    </div>
  );
}
