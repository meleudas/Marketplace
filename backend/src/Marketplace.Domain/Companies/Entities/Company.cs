using Marketplace.Domain.Common.Exceptions;
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

    public static Company Create(
        CompanyId id,
        string name,
        string slug,
        string description,
        string? imageUrl,
        string contactEmail,
        string contactPhone,
        Address address,
        JsonBlob? meta = null)
    {
        ValidateName(name);
        ValidateSlug(slug);
        ValidateDescription(description);
        ValidateContactEmail(contactEmail);
        ValidateContactPhone(contactPhone);

        var now = DateTime.UtcNow;
        return new Company
        {
            Id = id,
            Name = name.Trim(),
            Slug = slug.Trim(),
            Description = description.Trim(),
            ImageUrl = imageUrl,
            ContactEmail = contactEmail.Trim(),
            ContactPhone = contactPhone.Trim(),
            Address = address,
            IsApproved = false,
            ApprovedAt = null,
            ApprovedByUserId = null,
            Rating = null,
            ReviewCount = 0,
            FollowerCount = 0,
            Meta = meta ?? JsonBlob.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
            DeletedAt = null
        };
    }

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

    public void UpdateProfile(
        string name,
        string slug,
        string description,
        string? imageUrl,
        string contactEmail,
        string contactPhone,
        Address address,
        JsonBlob? meta = null)
    {
        EnsureNotDeleted();
        ValidateName(name);
        ValidateSlug(slug);
        ValidateDescription(description);
        ValidateContactEmail(contactEmail);
        ValidateContactPhone(contactPhone);

        Name = name.Trim();
        Slug = slug.Trim();
        Description = description.Trim();
        ImageUrl = imageUrl;
        ContactEmail = contactEmail.Trim();
        ContactPhone = contactPhone.Trim();
        Address = address;
        Meta = meta ?? JsonBlob.Empty;
        Touch();
    }

    public void Approve(string approvedByUserId)
    {
        EnsureNotDeleted();
        if (string.IsNullOrWhiteSpace(approvedByUserId))
            throw new DomainException("approvedByUserId cannot be empty");

        IsApproved = true;
        ApprovedAt = DateTime.UtcNow;
        ApprovedByUserId = approvedByUserId.Trim();
        Touch();
    }

    public void RevokeApproval()
    {
        EnsureNotDeleted();
        IsApproved = false;
        ApprovedAt = null;
        ApprovedByUserId = null;
        Touch();
    }

    public void SetReviewStats(decimal? rating, int reviewCount)
    {
        EnsureNotDeleted();
        if (reviewCount < 0)
            throw new DomainException("Review count cannot be negative");
        Rating = rating;
        ReviewCount = reviewCount;
        Touch();
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        MarkDeleted();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted company");
    }

    private static void ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Company name cannot be empty");
    }

    private static void ValidateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Company slug cannot be empty");
    }

    private static void ValidateDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Company description cannot be empty");
    }

    private static void ValidateContactEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Company contactEmail cannot be empty");
    }

    private static void ValidateContactPhone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Company contactPhone cannot be empty");
    }
}
