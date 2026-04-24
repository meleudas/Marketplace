namespace Marketplace.Infrastructure.Persistence.Entities;

public sealed class ReviewReplyRecord
{
    public long Id { get; set; }
    public long? ProductReviewId { get; set; }
    public long? CompanyReviewId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
