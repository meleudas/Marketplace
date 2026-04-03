using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Auth.Entities;

/// <summary>Збережений refresh-токен (таблиця refresh_tokens у схемі).</summary>
public sealed class UserRefreshToken : AuditableSoftDeleteAggregateRoot<UserRefreshTokenId>
{
    private UserRefreshToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public static UserRefreshToken Reconstitute(
        UserRefreshTokenId id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt,
        string? replacedByTokenHash,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            ReplacedByTokenHash = replacedByTokenHash,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
