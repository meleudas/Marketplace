export interface CatalogFilterOption {
  label: string;
  value: string;
}

export const DEFAULT_CATALOG_MIN_PRICE = "1";
export const DEFAULT_CATALOG_MAX_PRICE = "10000";

/** Author display names from seed data; backend matches case-insensitively after normalize. */
export const AUTHOR_FILTER_OPTIONS: CatalogFilterOption[] = [
  { label: "Тарас Шевченко", value: "Тарас Шевченко" },
  { label: "Леся Українка", value: "Леся Українка" },
  { label: "Михайло Коцюбинський", value: "Михайло Коцюбинський" },
  { label: "Джордж Орвелл", value: "Джордж Орвелл" },
  { label: "J.R.R. Tolkien", value: "J.R.R. Tolkien" },
  { label: "Агата Крісті", value: "Agatha Christie" },
];

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
