namespace Marketplace.Application.Reviews.Cache;

public static class ReviewCacheKeys
{
    public static string ProductList(long productId, int page, int size) => $"catalog:products:reviews:{productId}:{page}:{size}";
    public static string CompanyList(Guid companyId, int page, int size) => $"catalog:companies:reviews:{companyId}:{page}:{size}";
}
