using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Reviews.Entities;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ReviewReplyRepository : IReviewReplyRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewReplyRepository(ApplicationDbContext context) => _context = context;

    public async Task<ReviewReply?> GetByProductReviewIdAsync(ProductReviewId reviewId, CancellationToken ct = default)
    {
        var row = await _context.ReviewReplies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductReviewId == reviewId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<ReviewReply?> GetByCompanyReviewIdAsync(CompanyReviewId reviewId, CancellationToken ct = default)
    {
        var row = await _context.ReviewReplies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyReviewId == reviewId.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<ReviewReply> AddAsync(ReviewReply reply, CancellationToken ct = default)
    {
        var row = ToRecord(reply);
        await _context.ReviewReplies.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(ReviewReply reply, CancellationToken ct = default)
    {
        var row = await _context.ReviewReplies.FirstOrDefaultAsync(x => x.Id == reply.Id.Value, ct)
            ?? throw new InvalidOperationException("Review reply not found");
        row.Body = reply.Body;
        row.IsEdited = reply.IsEdited;
        row.UpdatedAt = reply.UpdatedAt;
        row.IsDeleted = reply.IsDeleted;
        row.DeletedAt = reply.DeletedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static ReviewReply ToDomain(ReviewReplyRecord row) =>
        ReviewReply.Reconstitute(
            ReviewReplyId.From(row.Id),
            row.ProductReviewId.HasValue ? ProductReviewId.From(row.ProductReviewId.Value) : null,
            row.CompanyReviewId.HasValue ? CompanyReviewId.From(row.CompanyReviewId.Value) : null,
            CompanyId.From(row.CompanyId),
            row.AuthorUserId,
            row.Body,
            row.IsEdited,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ReviewReplyRecord ToRecord(ReviewReply reply) =>
        new()
        {
            Id = reply.Id.Value,
            ProductReviewId = reply.ProductReviewId?.Value,
            CompanyReviewId = reply.CompanyReviewId?.Value,
            CompanyId = reply.CompanyId.Value,
            AuthorUserId = reply.AuthorUserId,
            Body = reply.Body,
            IsEdited = reply.IsEdited,
            CreatedAt = reply.CreatedAt,
            UpdatedAt = reply.UpdatedAt,
            IsDeleted = reply.IsDeleted,
            DeletedAt = reply.DeletedAt
        };
}
