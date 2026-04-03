using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Companies.Entities;

public sealed class Company : AuditableSoftDeleteAggregateRoot<CompanyId>
{
    private Company() { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;
    public Address Address { get; private set; } = Address.Empty;
    public bool IsApproved { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public decimal? Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public int FollowerCount { get; private set; }
    public JsonBlob Meta { get; private set; } = JsonBlob.Empty;

    public static Company Reconstitute(
        CompanyId id,
        string name,
        string slug,
        string description,
        string? imageUrl,
        string contactEmail,
        string contactPhone,
        Address address,
        bool isApproved,
        DateTime? approvedAt,
        string? approvedByUserId,
        decimal? rating,
        int reviewCount,
        int followerCount,
        JsonBlob meta,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            ImageUrl = imageUrl,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            Address = address,
            IsApproved = isApproved,
            ApprovedAt = approvedAt,
            ApprovedByUserId = approvedByUserId,
            Rating = rating,
            ReviewCount = reviewCount,
            FollowerCount = followerCount,
            Meta = meta,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
