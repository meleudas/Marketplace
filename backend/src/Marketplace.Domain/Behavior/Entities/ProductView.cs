using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Behavior.Entities;

public sealed class ProductView : AuditableSoftDeleteAggregateRoot<ProductViewId>
{
    private ProductView() { }

    public Guid? UserId { get; private set; }
    public ProductId ProductId { get; private set; } = null!;
    public string SessionId { get; private set; } = string.Empty;
    public DateTime ViewedAt { get; private set; }
    public int? TimeSpentSeconds { get; private set; }
    public ProductViewSource Source { get; private set; }
    public ViewDeviceType DeviceType { get; private set; }
    public JsonBlob Metadata { get; private set; } = JsonBlob.Empty;
    public string? RawContext { get; private set; }

    public static ProductView Reconstitute(
        ProductViewId id,
        Guid? userId,
        ProductId productId,
        string sessionId,
        DateTime viewedAt,
        int? timeSpentSeconds,
        ProductViewSource source,
        ViewDeviceType deviceType,
        JsonBlob metadata,
        string? rawContext,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            ProductId = productId,
            SessionId = sessionId,
            ViewedAt = viewedAt,
            TimeSpentSeconds = timeSpentSeconds,
            Source = source,
            DeviceType = deviceType,
            Metadata = metadata,
            RawContext = rawContext,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
