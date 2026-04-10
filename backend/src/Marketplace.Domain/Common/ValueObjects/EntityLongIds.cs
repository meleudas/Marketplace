using Marketplace.Domain.Common.Models;
using System.Collections.Generic;

namespace Marketplace.Domain.Common.ValueObjects;

// Strongly-typed long identifiers (узгоджено з bigint PK у схемі).

public sealed record CompanyId : ValueObject
{
    public Guid Value { get; }
    private CompanyId(Guid value) => Value = value;
    public static CompanyId From(Guid value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ProductId : ValueObject
{
    public long Value { get; }
    private ProductId(long value) => Value = value;
    public static ProductId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CategoryId : ValueObject
{
    public long Value { get; }
    private CategoryId(long value) => Value = value;
    public static CategoryId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CartId : ValueObject
{
    public long Value { get; }
    private CartId(long value) => Value = value;
    public static CartId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CartItemId : ValueObject
{
    public long Value { get; }
    private CartItemId(long value) => Value = value;
    public static CartItemId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record OrderId : ValueObject
{
    public long Value { get; }
    private OrderId(long value) => Value = value;
    public static OrderId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record OrderItemId : ValueObject
{
    public long Value { get; }
    private OrderItemId(long value) => Value = value;
    public static OrderItemId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record OrderStatusHistoryId : ValueObject
{
    public long Value { get; }
    private OrderStatusHistoryId(long value) => Value = value;
    public static OrderStatusHistoryId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record OrderAddressId : ValueObject
{
    public long Value { get; }
    private OrderAddressId(long value) => Value = value;
    public static OrderAddressId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ShippingMethodId : ValueObject
{
    public long Value { get; }
    private ShippingMethodId(long value) => Value = value;
    public static ShippingMethodId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record UserAddressId : ValueObject
{
    public long Value { get; }
    private UserAddressId(long value) => Value = value;
    public static UserAddressId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record PaymentId : ValueObject
{
    public long Value { get; }
    private PaymentId(long value) => Value = value;
    public static PaymentId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record RefundId : ValueObject
{
    public long Value { get; }
    private RefundId(long value) => Value = value;
    public static RefundId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CouponId : ValueObject
{
    public long Value { get; }
    private CouponId(long value) => Value = value;
    public static CouponId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record OrderCouponId : ValueObject
{
    public long Value { get; }
    private OrderCouponId(long value) => Value = value;
    public static OrderCouponId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ProductReviewId : ValueObject
{
    public long Value { get; }
    private ProductReviewId(long value) => Value = value;
    public static ProductReviewId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CompanyReviewId : ValueObject
{
    public long Value { get; }
    private CompanyReviewId(long value) => Value = value;
    public static CompanyReviewId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record FavoriteId : ValueObject
{
    public long Value { get; }
    private FavoriteId(long value) => Value = value;
    public static FavoriteId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ProductViewId : ValueObject
{
    public long Value { get; }
    private ProductViewId(long value) => Value = value;
    public static ProductViewId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record SearchHistoryId : ValueObject
{
    public long Value { get; }
    private SearchHistoryId(long value) => Value = value;
    public static SearchHistoryId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record UserBehaviorDailyId : ValueObject
{
    public long Value { get; }
    private UserBehaviorDailyId(long value) => Value = value;
    public static UserBehaviorDailyId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ProductImageId : ValueObject
{
    public long Value { get; }
    private ProductImageId(long value) => Value = value;
    public static ProductImageId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ProductDetailId : ValueObject
{
    public long Value { get; }
    private ProductDetailId(long value) => Value = value;
    public static ProductDetailId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CompanyScheduleId : ValueObject
{
    public long Value { get; }
    private CompanyScheduleId(long value) => Value = value;
    public static CompanyScheduleId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record WarehouseId : ValueObject
{
    public long Value { get; }
    private WarehouseId(long value) => Value = value;
    public static WarehouseId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record WarehouseStockId : ValueObject
{
    public long Value { get; }
    private WarehouseStockId(long value) => Value = value;
    public static WarehouseStockId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record StockMovementId : ValueObject
{
    public long Value { get; }
    private StockMovementId(long value) => Value = value;
    public static StockMovementId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record InventoryReservationId : ValueObject
{
    public long Value { get; }
    private InventoryReservationId(long value) => Value = value;
    public static InventoryReservationId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record CompanyFollowerId : ValueObject
{
    public long Value { get; }
    private CompanyFollowerId(long value) => Value = value;
    public static CompanyFollowerId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record MessageId : ValueObject
{
    public long Value { get; }
    private MessageId(long value) => Value = value;
    public static MessageId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record NotificationId : ValueObject
{
    public long Value { get; }
    private NotificationId(long value) => Value = value;
    public static NotificationId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ReportId : ValueObject
{
    public long Value { get; }
    private ReportId(long value) => Value = value;
    public static ReportId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record SupportTicketId : ValueObject
{
    public long Value { get; }
    private SupportTicketId(long value) => Value = value;
    public static SupportTicketId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record SupportTicketMessageId : ValueObject
{
    public long Value { get; }
    private SupportTicketMessageId(long value) => Value = value;
    public static SupportTicketMessageId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record AuditLogId : ValueObject
{
    public long Value { get; }
    private AuditLogId(long value) => Value = value;
    public static AuditLogId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record ActivityLogId : ValueObject
{
    public long Value { get; }
    private ActivityLogId(long value) => Value = value;
    public static ActivityLogId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record AnalyticsEventId : ValueObject
{
    public long Value { get; }
    private AnalyticsEventId(long value) => Value = value;
    public static AnalyticsEventId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}

public sealed record UserRefreshTokenId : ValueObject
{
    public long Value { get; }
    private UserRefreshTokenId(long value) => Value = value;
    public static UserRefreshTokenId From(long value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
