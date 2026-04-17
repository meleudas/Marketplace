namespace Marketplace.Application.Catalog.Cache;

public static class CatalogCacheKeys
{
    public const string ApprovedCompanies = "catalog:companies:approved";
    public const string ActiveCategories = "catalog:categories:active";
    public const string AllCategories = "admin:categories:all";
    public const string ProductList = "catalog:products:list";
    public const string ProductDetailPrefix = "catalog:products:detail:";
    public const string CatalogCompanyByIdPrefix = "catalog:companies:id:";
    public const string CatalogCompanyBySlugPrefix = "catalog:companies:slug:";
    public const string AdminCompanyByIdPrefix = "admin:companies:id:";
    public const string CatalogCategoryByIdPrefix = "catalog:categories:id:";
    public const string AdminCategoryByIdPrefix = "admin:categories:id:";
}
