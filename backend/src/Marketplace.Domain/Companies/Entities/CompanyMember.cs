using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Enums;

namespace Marketplace.Domain.Companies.Entities;

/// <summary>Зв'язок користувача з компанією (PK company_id + user_id у БД).</summary>
public sealed class CompanyMember : Entity
{
    private CompanyMember() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public bool IsOwner { get; private set; }
    public CompanyMembershipRole Role { get; private set; }
    public JsonBlob Permissions { get; private set; } = JsonBlob.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static CompanyMember Create(
        CompanyId companyId,
        Guid userId,
        CompanyMembershipRole role,
        JsonBlob? permissions = null)
    {
        var now = DateTime.UtcNow;
        return new CompanyMember
        {
            CompanyId = companyId,
            UserId = userId,
            IsOwner = role == CompanyMembershipRole.Owner,
            Role = role,
            Permissions = permissions ?? JsonBlob.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public static CompanyMember Reconstitute(
        CompanyId companyId,
        Guid userId,
        bool isOwner,
        CompanyMembershipRole role,
        JsonBlob permissions,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            CompanyId = companyId,
            UserId = userId,
            IsOwner = isOwner,
            Role = role,
            Permissions = permissions,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void ChangeRole(CompanyMembershipRole role)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot change role for deleted company member.");

        Role = role;
        IsOwner = role == CompanyMembershipRole.Owner;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
