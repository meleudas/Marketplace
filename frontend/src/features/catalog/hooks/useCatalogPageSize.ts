import { useEffect, useState } from "react";

const DESKTOP_MEDIA_QUERY = "(min-width: 1024px)";

export const MOBILE_CATALOG_PAGE_SIZE = 8;
export const DESKTOP_CATALOG_PAGE_SIZE = 21;

export function useCatalogPageSize(): number {
  const [pageSize, setPageSize] = useState(MOBILE_CATALOG_PAGE_SIZE);

  useEffect(() => {
    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const updatePageSize = () => {
      setPageSize(mediaQuery.matches ? DESKTOP_CATALOG_PAGE_SIZE : MOBILE_CATALOG_PAGE_SIZE);
    };

    updatePageSize();
    mediaQuery.addEventListener("change", updatePageSize);

    return () => mediaQuery.removeEventListener("change", updatePageSize);
  }, []);

  return pageSize;
}
