using Marketplace.Domain.Companies.Entities;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class CompanyReviewRepository : ICompanyReviewRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyReviewRepository(ApplicationDbContext context) => _context = context;

    public async Task<CompanyReview?> GetByIdAsync(CompanyReviewId id, CancellationToken ct = default)
    {
        var row = await _context.CompanyReviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<CompanyReview?> GetByCompanyAndUserAsync(CompanyId companyId, Guid userId, CancellationToken ct = default)
    {
        var row = await _context.CompanyReviews.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId.Value && x.UserId == userId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<CompanyReview>> ListByCompanyAsync(CompanyId companyId, int page, int size, CancellationToken ct = default)
    {
        var skip = Math.Max(0, page - 1) * Math.Max(1, size);
        var take = Math.Clamp(size, 1, 100);
        var rows = await _context.CompanyReviews.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value && x.Status == (short)CompanyReviewStatus.Approved)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<(decimal? Average, int Count)> GetApprovedStatsAsync(CompanyId companyId, CancellationToken ct = default)
    {
        var query = _context.CompanyReviews.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value && x.Status == (short)CompanyReviewStatus.Approved);
        var count = await query.CountAsync(ct);
        if (count == 0)
            return (null, 0);
        var avg = await query.AverageAsync(x => x.OverallRating, ct);
        return (Math.Round(avg, 2), count);
    }

    public async Task<CompanyReview> AddAsync(CompanyReview review, CancellationToken ct = default)
    {
        var row = ToRecord(review);
        await _context.CompanyReviews.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(CompanyReview review, CancellationToken ct = default)
    {
        var row = await _context.CompanyReviews.FirstOrDefaultAsync(x => x.Id == review.Id.Value, ct)
            ?? throw new InvalidOperationException("Company review not found");
        row.Comment = review.Comment;
        row.OverallRating = review.OverallRating;
        row.RatingsRaw = review.Ratings.Raw ?? "{}";
        row.Status = (short)review.Status;
        row.ModeratedByUserId = review.ModeratedByUserId;
        row.ModeratedAt = review.ModeratedAt;
        row.UpdatedAt = review.UpdatedAt;
        row.IsDeleted = review.IsDeleted;
        row.DeletedAt = review.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(CompanyReviewId id, DateTime utcNow, CancellationToken ct = default)
    {
        var row = await _context.CompanyReviews.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (row is null || row.IsDeleted)
            return;
        row.IsDeleted = true;
        row.DeletedAt = utcNow;
        row.UpdatedAt = utcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CompanyReview>> ListByStatusAsync(CompanyReviewStatus status, int page, int size, CancellationToken ct = default)
    {
        var skip = Math.Max(0, page - 1) * Math.Max(1, size);
        var take = Math.Clamp(size, 1, 100);
        var rows = await _context.CompanyReviews.AsNoTracking()
            .Where(x => x.Status == (short)status)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    private static CompanyReview ToDomain(CompanyReviewRecord r) =>
        CompanyReview.Reconstitute(
            CompanyReviewId.From(r.Id),
            CompanyId.From(r.CompanyId),
            r.UserId,
            r.UserName,
            r.OrderId,
            r.IsVerifiedPurchase,
            new JsonBlob(r.RatingsRaw),
            r.OverallRating,
            r.Comment,
            (CompanyReviewStatus)r.Status,
            r.ModeratedByUserId,
            r.ModeratedAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static CompanyReviewRecord ToRecord(CompanyReview review) =>
        new()
        {
            Id = review.Id.Value,
            CompanyId = review.CompanyId.Value,
            UserId = review.UserId,
            UserName = review.UserName,
            OrderId = review.OrderId,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            RatingsRaw = review.Ratings.Raw ?? "{}",
            OverallRating = review.OverallRating,
            Comment = review.Comment,
            Status = (short)review.Status,
            ModeratedByUserId = review.ModeratedByUserId,
            ModeratedAt = review.ModeratedAt,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            IsDeleted = review.IsDeleted,
            DeletedAt = review.DeletedAt
        };
}
