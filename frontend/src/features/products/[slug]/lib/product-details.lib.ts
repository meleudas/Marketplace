import type { CatalogProductDetailDto } from "@/features/storefront/model/catalog.types";

interface ProductAttributesBlob {
  author?: string;
  genre?: string;
  format?: string;
}

interface ProductSpecificationsBlob {
  isbn?: string;
  pages?: number;
  year?: number;
  language?: string;
  format?: string;
}

export interface ProductCharacteristic {
  label: string;
  value: string;
}

function parseJsonBlob<T>(raw: string | null | undefined): T | null {
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}

export function getProductAuthor(detail: CatalogProductDetailDto["detail"]): string | null {
  const attributes = parseJsonBlob<ProductAttributesBlob>(detail?.attributesRaw);
  const author = attributes?.author?.trim();
  return author || null;
}

export function buildProductCharacteristics(
  detail: CatalogProductDetailDto["detail"],
): ProductCharacteristic[] {
  const attributes = parseJsonBlob<ProductAttributesBlob>(detail?.attributesRaw);
  const specifications = parseJsonBlob<ProductSpecificationsBlob>(detail?.specificationsRaw);

  const entries: Array<[string, unknown]> = [
    ["Автор", attributes?.author],
    ["Кількість сторінок", specifications?.pages],
    ["Жанр", attributes?.genre],
    ["Рік видання", specifications?.year],
    ["Формат", specifications?.format ?? attributes?.format],
    ["Мова", specifications?.language],
    ["ISBN", specifications?.isbn],
  ];

  return entries
    .filter(([, value]) => value !== undefined && value !== null && value !== "")
    .map(([label, value]) => ({ label, value: String(value) }));
}

export function stripFormatSuffix(name: string): string {
  return name.replace(/\s*\([^()]*\)\s*$/u, "").trim();
}

export function resolveProductFormat(
  detail: CatalogProductDetailDto["detail"],
  productName: string,
): { label: string; isElectronic: boolean } {
  const attributes = parseJsonBlob<ProductAttributesBlob>(detail?.attributesRaw);
  const specifications = parseJsonBlob<ProductSpecificationsBlob>(detail?.specificationsRaw);
  const rawFormat = specifications?.format ?? attributes?.format ?? productName;
  const isElectronic = rawFormat.toLowerCase().includes("електрон");

  return { label: isElectronic ? "Електронна" : "Паперова", isElectronic };
}

export function resolveAvailabilityLabel(product: {
  availabilityStatus: string;
  availableQty: number;
}): { inStock: boolean; label: string } {
  const inStock = product.availabilityStatus !== "out_of_stock" && product.availableQty > 0;
  return { inStock, label: inStock ? "В наявності" : "Немає в наявності" };
}

export function formatPriceWithUnit(value: number): string {
  return `${new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(value)} грн.`;
}

export function formatCompactPrice(value: number): string {
  return `${new Intl.NumberFormat("uk-UA", { maximumFractionDigits: 0 }).format(value)}грн`;
}
