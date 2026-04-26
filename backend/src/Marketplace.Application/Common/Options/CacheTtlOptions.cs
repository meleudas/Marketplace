namespace Marketplace.Application.Common.Options;

public sealed class CacheTtlOptions
{
    public const string SectionName = "CacheTtl";

    public int CartSeconds { get; set; } = 60;
    public int FavoritesMinutes { get; set; } = 3;

    public int UsersProfileMinutes { get; set; } = 5;
    public int UsersListMinutes { get; set; } = 5;
    public int UsersSearchMinutes { get; set; } = 2;

    public int CatalogApprovedCompaniesMinutes { get; set; } = 5;
    public int CatalogActiveCategoriesMinutes { get; set; } = 10;
    public int CatalogProductListMinutes { get; set; } = 5;
    public int CatalogProductDetailMinutes { get; set; } = 5;
    public int CatalogProductReviewsMinutes { get; set; } = 5;
    public int CatalogCompanyMinutes { get; set; } = 10;
    public int CatalogCompanyReviewsMinutes { get; set; } = 10;
    public int CatalogCategoryMinutes { get; set; } = 10;

    public int AdminCompanyMinutes { get; set; } = 5;
    public int AdminCategoryMinutes { get; set; } = 10;
    public int AdminAllCategoriesMinutes { get; set; } = 10;
    public int OrdersListMinutes { get; set; } = 3;
    public int OrderDetailMinutes { get; set; } = 2;

    public TimeSpan Cart => TimeSpan.FromSeconds(Math.Clamp(CartSeconds, 5, 3600));
    public TimeSpan Favorites => TimeSpan.FromMinutes(Math.Clamp(FavoritesMinutes, 1, 120));
    public TimeSpan UsersProfile => TimeSpan.FromMinutes(Math.Clamp(UsersProfileMinutes, 1, 120));
    public TimeSpan UsersList => TimeSpan.FromMinutes(Math.Clamp(UsersListMinutes, 1, 120));
    public TimeSpan UsersSearch => TimeSpan.FromMinutes(Math.Clamp(UsersSearchMinutes, 1, 60));
    public TimeSpan CatalogApprovedCompanies => TimeSpan.FromMinutes(Math.Clamp(CatalogApprovedCompaniesMinutes, 1, 240));
    public TimeSpan CatalogActiveCategories => TimeSpan.FromMinutes(Math.Clamp(CatalogActiveCategoriesMinutes, 1, 240));
    public TimeSpan CatalogProductList => TimeSpan.FromMinutes(Math.Clamp(CatalogProductListMinutes, 1, 240));
    public TimeSpan CatalogProductDetail => TimeSpan.FromMinutes(Math.Clamp(CatalogProductDetailMinutes, 1, 240));
    public TimeSpan CatalogProductReviews => TimeSpan.FromMinutes(Math.Clamp(CatalogProductReviewsMinutes, 1, 240));
    public TimeSpan CatalogCompany => TimeSpan.FromMinutes(Math.Clamp(CatalogCompanyMinutes, 1, 240));
    public TimeSpan CatalogCompanyReviews => TimeSpan.FromMinutes(Math.Clamp(CatalogCompanyReviewsMinutes, 1, 240));
    public TimeSpan CatalogCategory => TimeSpan.FromMinutes(Math.Clamp(CatalogCategoryMinutes, 1, 240));
    public TimeSpan AdminCompany => TimeSpan.FromMinutes(Math.Clamp(AdminCompanyMinutes, 1, 240));
    public TimeSpan AdminCategory => TimeSpan.FromMinutes(Math.Clamp(AdminCategoryMinutes, 1, 240));
    public TimeSpan AdminAllCategories => TimeSpan.FromMinutes(Math.Clamp(AdminAllCategoriesMinutes, 1, 240));
    public TimeSpan OrdersList => TimeSpan.FromMinutes(Math.Clamp(OrdersListMinutes, 1, 60));
    public TimeSpan OrderDetail => TimeSpan.FromMinutes(Math.Clamp(OrderDetailMinutes, 1, 60));
}
