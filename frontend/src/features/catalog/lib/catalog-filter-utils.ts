export const toggleArrayFilter = (currentValues: string[], nextValue: string): string[] =>
  currentValues.includes(nextValue)
    ? currentValues.filter((value) => value !== nextValue)
    : [...currentValues, nextValue];

export const toggleSingleFilter = (currentValue: string | null, nextValue: string): string | null =>
  currentValue === nextValue ? null : nextValue;

export const parsePriceFilter = (value: string): number | undefined => {
  const normalized = value.trim().replace(",", ".");

  if (!normalized) {
    return undefined;
  }

  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
};
