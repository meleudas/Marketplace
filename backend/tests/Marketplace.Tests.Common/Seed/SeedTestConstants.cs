namespace Marketplace.Tests.Common.Seed;

public static class SeedTestConstants
{
    public const string Password = "Admin123!";

    public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SellerUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BuyerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public const string AdminEmail = "admin@marketplace.test";
    public const string SellerEmail = "seller@marketplace.test";
    public const string BuyerEmail = "buyer@marketplace.test";
    public const string UnverifiedEmail = "unverified@marketplace.test";
    public const string TwoFactorEmail = "twofa@marketplace.test";

    public static readonly Guid UnverifiedUserId = Guid.Parse("b0000001-0000-4000-8000-000000000001");
    public static readonly Guid TwoFactorUserId = Guid.Parse("b0000002-0000-4000-8000-000000000002");

    public static readonly Guid TechStoreCompanyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid HomeComfortCompanyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public const long OrderShippedId = 2;
    public const long OrderDeliveredId = 3;
    public const long OrderPaidSplitId = 4;

    public const long ProductPhoneId = 1;
    public const long WarehouseKyivId = 1;
    public const long WarehouseLvivId = 2;

    public const long SettlementBatchReadyId = 1;
    public const long ShipmentOrder2Wh1Id = 1;

    public const string CouponCode = "SEED10";
    public const string ProductSlug = "seed-phone-alpha";

    public static readonly Guid OrderChatId = Guid.Parse("c1000001-0000-4000-8000-000000000001");

    public const long ReturnRequestId = 1;
}
