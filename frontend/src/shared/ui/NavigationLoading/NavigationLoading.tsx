"use client";

import {
  createContext,
  type Dispatch,
  type ReactNode,
  type SetStateAction,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import { usePathname } from "next/navigation";
import { PageLoadingScreen } from "@/shared/ui/PageLoadingScreen";

const SHOW_DELAY_MS = 120;
const SAFETY_TIMEOUT_MS = 10_000;

const NavigationLoadingContext = createContext<Dispatch<SetStateAction<boolean>> | null>(null);

function isModifiedClick(event: MouseEvent): boolean {
  return event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0;
}

function resolveInternalPathname(href: string | null): string | null {
  if (!href || href.startsWith("#") || href.startsWith("mailto:") || href.startsWith("tel:")) {
    return null;
  }

  try {
    const url = new URL(href, window.location.origin);
    if (url.origin !== window.location.origin) {
      return null;
    }
    return url.pathname;
  } catch {
    return null;
  }
}

interface NavigationLoadingProps {
  children: ReactNode;
}

export function NavigationLoading({ children }: NavigationLoadingProps) {
  const pathname = usePathname();
  const [isNavigating, setIsNavigating] = useState(false);
  const [isManualLoading, setManualLoading] = useState(false);
  const pathnameRef = useRef(pathname);

  useEffect(() => {
    if (pathnameRef.current !== pathname) {
      pathnameRef.current = pathname;
      setIsNavigating(false);
    }
  }, [pathname]);

  useEffect(() => {
    if (!isNavigating && !isManualLoading) {
      return;
    }

    const safetyTimerId = window.setTimeout(() => {
      setIsNavigating(false);
      setManualLoading(false);
    }, SAFETY_TIMEOUT_MS);

    return () => {
      window.clearTimeout(safetyTimerId);
    };
  }, [isManualLoading, isNavigating]);

  useEffect(() => {
    let showTimerId: number | null = null;

    const startPending = () => {
      if (showTimerId !== null) {
        window.clearTimeout(showTimerId);
      }

      showTimerId = window.setTimeout(() => {
        setIsNavigating(true);
      }, SHOW_DELAY_MS);
    };

    const onDocumentClick = (event: MouseEvent) => {
      if (isModifiedClick(event)) {
        return;
      }

      const target = event.target;
      if (!(target instanceof Element)) {
        return;
      }

      const anchor = target.closest("a");
      if (!anchor || anchor.hasAttribute("download") || anchor.getAttribute("target") === "_blank") {
        return;
      }

      const nextPathname = resolveInternalPathname(anchor.getAttribute("href"));
      if (!nextPathname || nextPathname === pathnameRef.current) {
        return;
      }

      startPending();
    };

    const onPopState = () => {
      startPending();
    };

    document.addEventListener("click", onDocumentClick);
    window.addEventListener("popstate", onPopState);

    return () => {
      document.removeEventListener("click", onDocumentClick);
      window.removeEventListener("popstate", onPopState);
      if (showTimerId !== null) {
        window.clearTimeout(showTimerId);
      }
    };
  }, []);

  return (
    <NavigationLoadingContext.Provider value={setManualLoading}>
      {children}
      {isNavigating || isManualLoading ? <PageLoadingScreen variant="overlay" /> : null}
    </NavigationLoadingContext.Provider>
  );
}

export function useNavigationLoading() {
  const context = useContext(NavigationLoadingContext);

  if (!context) {
    throw new Error("useNavigationLoading must be used within NavigationLoading");
  }

  return context;
}
