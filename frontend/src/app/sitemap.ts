import type { MetadataRoute } from "next";

const siteUrl = "https://booktop.ua";

// ── Static routes ──
const staticRoutes: MetadataRoute.Sitemap = [
  {
    url: siteUrl,
    lastModified: new Date(),
    changeFrequency: "weekly",
    priority: 1,
  },
  {
    url: `${siteUrl}/catalog`,
    lastModified: new Date(),
    changeFrequency: "daily",
    priority: 0.9,
  },
  {
    url: `${siteUrl}/about`,
    lastModified: new Date(),
    changeFrequency: "monthly",
    priority: 0.5,
  },
  {
    url: `${siteUrl}/cart`,
    lastModified: new Date(),
    changeFrequency: "never",
    priority: 0.3,
  },
  {
    url: `${siteUrl}/checkout`,
    lastModified: new Date(),
    changeFrequency: "never",
    priority: 0.3,
  },
  {
    url: `${siteUrl}/auth`,
    lastModified: new Date(),
    changeFrequency: "never",
    priority: 0.2,
  },
  {
    url: `${siteUrl}/auth/login`,
    lastModified: new Date(),
    changeFrequency: "never",
    priority: 0.2,
  },
  {
    url: `${siteUrl}/auth/register`,
    lastModified: new Date(),
    changeFrequency: "never",
    priority: 0.2,
  },
  {
    url: `${siteUrl}/companies`,
    lastModified: new Date(),
    changeFrequency: "weekly",
    priority: 0.7,
  },
];

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const entries: MetadataRoute.Sitemap = [...staticRoutes];

  // ── Product pages ──
  try {
    const { getCatalogProducts } = await import(
      "@/features/storefront/api/catalog.api"
    );
    const products = await getCatalogProducts();

    for (const product of products) {
      entries.push({
        url: `${siteUrl}/products/${product.slug}`,
        lastModified: new Date(product.updatedAt ?? product.createdAt),
        changeFrequency: "weekly",
        priority: 0.8,
      });
    }
  } catch {
    // Gracefully fallback — sitemap still works with static routes
    console.warn("Failed to fetch products for sitemap");
  }

  // ── Category pages ──
  try {
    const { getCatalogCategories } = await import(
      "@/features/storefront/api/catalog.api"
    );
    const categories = await getCatalogCategories();

    for (const category of categories) {
      entries.push({
        url: `${siteUrl}/catalog?categoryIds=${category.id}`,
        lastModified: new Date(),
        changeFrequency: "daily",
        priority: 0.7,
      });
    }
  } catch {
    console.warn("Failed to fetch categories for sitemap");
  }

  // ── Company pages ──
  try {
    const { getCatalogCompanies } = await import(
      "@/features/storefront/api/catalog.api"
    );
    const companies = await getCatalogCompanies();

    for (const company of companies) {
      entries.push({
        url: `${siteUrl}/companies/${company.slug}`,
        lastModified: new Date(),
        changeFrequency: "weekly",
        priority: 0.7,
      });
    }
  } catch {
    console.warn("Failed to fetch companies for sitemap");
  }

  return entries;
}
