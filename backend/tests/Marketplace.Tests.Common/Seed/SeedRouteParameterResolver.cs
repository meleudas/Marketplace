using System.Text.RegularExpressions;

namespace Marketplace.Tests.Common.Seed;

public static partial class SeedRouteParameterResolver
{
    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex RouteParameterRegex();

    public static string Resolve(string normalizedPath)
    {
        var lower = normalizedPath.ToLowerInvariant();
        return RouteParameterRegex().Replace(lower, match =>
        {
            var name = match.Groups[1].Value.ToLowerInvariant();
            return ResolveParameter(name, lower);
        });
    }

    private static string ResolveParameter(string name, string path)
    {
        return name switch
        {
            "companyid" => path.Contains("/admin/companies/", StringComparison.Ordinal)
                ? SeedTestConstants.TechStoreCompanyId.ToString()
                : SeedTestConstants.TechStoreCompanyId.ToString(),
            "orderid" => path.Contains("/orders/4", StringComparison.Ordinal) || path.Contains("order-4", StringComparison.Ordinal)
                ? SeedTestConstants.OrderPaidSplitId.ToString()
                : SeedTestConstants.OrderShippedId.ToString(),
            "batchid" => SeedTestConstants.SettlementBatchReadyId.ToString(),
            "shipmentid" => SeedTestConstants.ShipmentOrder2Wh1Id.ToString(),
            "productid" => SeedTestConstants.ProductPhoneId.ToString(),
            "warehouseid" => SeedTestConstants.WarehouseKyivId.ToString(),
            "slug" => SeedTestConstants.ProductSlug,
            "chatid" => SeedTestConstants.OrderChatId.ToString(),
            "returnrequestid" or "returnid" => SeedTestConstants.ReturnRequestId.ToString(),
            "userid" => SeedTestConstants.BuyerUserId.ToString(),
            "code" => SeedTestConstants.CouponCode.ToLowerInvariant(),
            "id" when path.Contains("/chats/", StringComparison.Ordinal) => SeedTestConstants.OrderChatId.ToString(),
            "id" when path.Contains("/settlements/", StringComparison.Ordinal) => SeedTestConstants.SettlementBatchReadyId.ToString(),
            "id" when path.Contains("/products/", StringComparison.Ordinal) => SeedTestConstants.ProductPhoneId.ToString(),
            "id" when path.Contains("/orders/", StringComparison.Ordinal) => SeedTestConstants.OrderShippedId.ToString(),
            "id" => "1",
            _ => "1",
        };
    }
}
