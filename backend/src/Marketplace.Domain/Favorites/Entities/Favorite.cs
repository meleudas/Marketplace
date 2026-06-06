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

    public static Favorite Create(
        FavoriteId id,
        Guid userId,
        ProductId productId,
        DateTime addedAt,
        Money? priceAtAdd,
        bool isAvailable = true,
        JsonBlob? notifications = null,
        JsonBlob? meta = null)
    {
        return new Favorite
        {
            Id = id,
            UserId = userId,
            ProductId = productId,
            AddedAt = addedAt,
            PriceAtAdd = priceAtAdd,
            IsAvailable = isAvailable,
            Notifications = notifications ?? JsonBlob.Empty,
            Meta = meta,
            CreatedAt = addedAt,
            UpdatedAt = addedAt,
            IsDeleted = false,
            DeletedAt = null
        };
    }

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

    public void Reactivate(DateTime utcNow, Money? priceAtAdd)
    {
        AddedAt = utcNow;
        PriceAtAdd = priceAtAdd;
        IsAvailable = true;
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = utcNow;
    }

    public void SoftDelete(DateTime utcNow)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = utcNow;
        UpdatedAt = utcNow;
    }
}
