using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Enums;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ProductReviewRepository : IProductReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ProductReviewRepository(ApplicationDbContext context) => _context = context;

    public async Task<ProductReview?> GetByIdAsync(ProductReviewId id, CancellationToken ct = default)
    {
        var row = await _context.ProductReviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<ProductReview?> GetByProductAndUserAsync(ProductId productId, Guid userId, CancellationToken ct = default)
    {
        var row = await _context.ProductReviews.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId.Value && x.UserId == userId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<ProductReview>> ListByProductAsync(ProductId productId, int page, int size, CancellationToken ct = default)
    {
        var skip = Math.Max(0, page - 1) * Math.Max(1, size);
        var take = Math.Clamp(size, 1, 100);
        var rows = await _context.ProductReviews.AsNoTracking()
            .Where(x => x.ProductId == productId.Value && x.Status == (short)ReviewModerationStatus.Approved)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<(decimal? Average, int Count)> GetApprovedStatsAsync(ProductId productId, CancellationToken ct = default)
    {
        var query = _context.ProductReviews.AsNoTracking()
            .Where(x => x.ProductId == productId.Value && x.Status == (short)ReviewModerationStatus.Approved);
        var count = await query.CountAsync(ct);
        if (count == 0)
            return (null, 0);
        var avg = await query.AverageAsync(x => (decimal)x.Rating, ct);
        return (Math.Round(avg, 2), count);
    }

    public async Task<ProductReview> AddAsync(ProductReview review, CancellationToken ct = default)
    {
        var row = ToRecord(review);
        await _context.ProductReviews.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(ProductReview review, CancellationToken ct = default)
    {
        var row = await _context.ProductReviews.FirstOrDefaultAsync(x => x.Id == review.Id.Value, ct)
            ?? throw new InvalidOperationException("Product review not found");
        row.Rating = review.Rating;
        row.Title = review.Title;
        row.Comment = review.Comment;
        row.ProsRaw = review.Pros.Raw ?? "{}";
        row.ConsRaw = review.Cons.Raw ?? "{}";
        row.Status = (short)review.Status;
        row.ModeratedByUserId = review.ModeratedByUserId;
        row.ModeratedAt = review.ModeratedAt;
        row.UpdatedAt = review.UpdatedAt;
        row.IsDeleted = review.IsDeleted;
        row.DeletedAt = review.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(ProductReviewId id, DateTime utcNow, CancellationToken ct = default)
    {
        var row = await _context.ProductReviews.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (row is null || row.IsDeleted)
            return;
        row.IsDeleted = true;
        row.DeletedAt = utcNow;
        row.UpdatedAt = utcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProductReview>> ListByStatusAsync(ReviewModerationStatus status, int page, int size, CancellationToken ct = default)
    {
        var skip = Math.Max(0, page - 1) * Math.Max(1, size);
        var take = Math.Clamp(size, 1, 100);
        var rows = await _context.ProductReviews.AsNoTracking()
            .Where(x => x.Status == (short)status)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    private static ProductReview ToDomain(ProductReviewRecord r) =>
        ProductReview.Reconstitute(
            ProductReviewId.From(r.Id),
            ProductId.From(r.ProductId),
            r.UserId,
            r.UserName,
            r.UserAvatar,
            r.Rating,
            r.Title,
            r.Comment,
            new JsonBlob(r.ImagesRaw),
            new JsonBlob(r.ProsRaw),
            new JsonBlob(r.ConsRaw),
            r.IsVerifiedPurchase,
            r.OrderId.HasValue ? OrderId.From(r.OrderId.Value) : null,
            new JsonBlob(r.HelpfulRaw),
            (ReviewModerationStatus)r.Status,
            r.ModeratedByUserId,
            r.ModeratedAt,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);

    private static ProductReviewRecord ToRecord(ProductReview review) =>
        new()
        {
            Id = review.Id.Value,
            ProductId = review.ProductId.Value,
            UserId = review.UserId,
            UserName = review.UserName,
            UserAvatar = review.UserAvatar,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            ImagesRaw = review.Images.Raw ?? "{}",
            ProsRaw = review.Pros.Raw ?? "{}",
            ConsRaw = review.Cons.Raw ?? "{}",
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            OrderId = review.OrderId?.Value,
            HelpfulRaw = review.Helpful.Raw ?? "{}",
            Status = (short)review.Status,
            ModeratedByUserId = review.ModeratedByUserId,
            ModeratedAt = review.ModeratedAt,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            IsDeleted = review.IsDeleted,
            DeletedAt = review.DeletedAt
        };
}
