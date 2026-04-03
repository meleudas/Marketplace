using Marketplace.Domain.Cart.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Cart.Entities;

public sealed class Cart : AuditableSoftDeleteAggregateRoot<CartId>
{
    private Cart() { }

    public Guid UserId { get; private set; }
    public CartStatus Status { get; private set; }
    public DateTime LastActivityAt { get; private set; }

    public static Cart Reconstitute(
        CartId id,
        Guid userId,
        CartStatus status,
        DateTime lastActivityAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            Status = status,
            LastActivityAt = lastActivityAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
