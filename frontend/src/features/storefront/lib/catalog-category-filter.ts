import type { CatalogCategoryDto } from "@/features/storefront/model/catalog.types";

function sortCategories(a: CatalogCategoryDto, b: CatalogCategoryDto): number {
  return a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, "uk");
}

function buildChildrenByParentId(
  categories: CatalogCategoryDto[],
): Map<number, CatalogCategoryDto[]> {
  const childrenByParentId = new Map<number, CatalogCategoryDto[]>();

  for (const category of categories) {
    if (category.parentId === null) {
      continue;
    }

    const siblings = childrenByParentId.get(category.parentId);
    if (siblings) {
      siblings.push(category);
    } else {
      childrenByParentId.set(category.parentId, [category]);
    }
  }

  for (const siblings of childrenByParentId.values()) {
    siblings.sort(sortCategories);
  }

  return childrenByParentId;
}

export function getRootCategories(categories: CatalogCategoryDto[]): CatalogCategoryDto[] {
  return categories.filter((category) => category.parentId === null).sort(sortCategories);
}

export function getChildCategories(
  categories: CatalogCategoryDto[],
  parentId: number,
): CatalogCategoryDto[] {
  return categories.filter((category) => category.parentId === parentId).sort(sortCategories);
}

export function resolveCategorySelection(
  categories: CatalogCategoryDto[],
  slug: string,
): { rootSlug: string | null; subcategorySlug: string | null } {
  const category = categories.find((item) => item.slug === slug);
  if (!category) {
    return { rootSlug: null, subcategorySlug: null };
  }

  if (category.parentId === null) {
    return { rootSlug: category.slug, subcategorySlug: null };
  }

  const parent = categories.find((item) => item.id === category.parentId);
  return {
    rootSlug: parent?.slug ?? null,
    subcategorySlug: category.slug,
  };
}

/** Category id plus all descendant ids (for root → subcategory filtering). */
export function getCategoryFilterIdsFromSlugs(
  categories: CatalogCategoryDto[],
  slugs: readonly string[],
): number[] {
  const ids = slugs
    .map((slug) => categories.find((category) => category.slug === slug))
    .filter((category): category is CatalogCategoryDto => Boolean(category))
    .flatMap((category) => getCategoryFilterIds(categories, category));

  return [...new Set(ids)];
}

export function getCategoryFilterIds(
  categories: CatalogCategoryDto[],
  category: CatalogCategoryDto,
): number[] {
  const childrenByParentId = buildChildrenByParentId(categories);
  const filterIds = [category.id];
  const stack = [...(childrenByParentId.get(category.id) ?? [])];

  while (stack.length > 0) {
    const current = stack.pop();
    if (!current) {
      continue;
    }

    filterIds.push(current.id);
    stack.push(...(childrenByParentId.get(current.id) ?? []));
  }

  return filterIds;
}

export function productMatchesCategoryFilter(
  productCategoryId: number,
  categoryFilterIds: readonly number[],
): boolean {
  return categoryFilterIds.includes(productCategoryId);
}
