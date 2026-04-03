using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Favorites.Entities;

public sealed class Favorite : AuditableSoftDeleteAggregateRoot<FavoriteId>
{
    private Favorite() { }

    public Guid UserId { get; private set; }
    public ProductId ProductId { get; private set; } = null!;
    public DateTime AddedAt { get; private set; }
    public Money? PriceAtAdd { get; private set; }
    public bool IsAvailable { get; private set; }
    public JsonBlob Notifications { get; private set; } = JsonBlob.Empty;
    public JsonBlob? Meta { get; private set; }

    public static Favorite Reconstitute(
        FavoriteId id,
        Guid userId,
        ProductId productId,
        DateTime addedAt,
        Money? priceAtAdd,
        bool isAvailable,
        JsonBlob notifications,
        JsonBlob? meta,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            ProductId = productId,
            AddedAt = addedAt,
            PriceAtAdd = priceAtAdd,
            IsAvailable = isAvailable,
            Notifications = notifications,
            Meta = meta,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
