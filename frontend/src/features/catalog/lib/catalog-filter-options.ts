export interface CatalogFilterOption {
  label: string;
  value: string;
}

export const DEFAULT_CATALOG_MIN_PRICE = "1";
export const DEFAULT_CATALOG_MAX_PRICE = "10000";

/** Backend format facet values from ProductSeeder (attributes.format). */
export const FORMAT_FILTER_OPTIONS = [
  { label: "Електронна", value: "електронний" },
  { label: "Паперова", value: "паперовий" },
] as const satisfies readonly CatalogFilterOption[];

export type CatalogProductFormat = (typeof FORMAT_FILTER_OPTIONS)[number]["value"];

export const isCatalogProductFormat = (value: string): value is CatalogProductFormat =>
  FORMAT_FILTER_OPTIONS.some((option) => option.value === value);

export const resolveAppliedPriceFilter = (value: string, defaultValue: string): string => {
  const trimmed = value.trim();
  return trimmed === defaultValue ? "" : trimmed;
};
