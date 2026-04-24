namespace Marketplace.Application.Reviews.DTOs;

public sealed record ReviewReplyDto(
    long Id,
    Guid CompanyId,
    Guid AuthorUserId,
    string Body,
    bool IsEdited,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ReviewDto(
    long Id,
    string TargetType,
    long? ProductId,
    Guid? CompanyId,
    Guid UserId,
    string UserName,
    byte? Rating,
    decimal? OverallRating,
    string? Title,
    string Comment,
    bool IsVerifiedPurchase,
    short Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ReviewReplyDto? Reply);

public sealed record ReviewListDto(
    int Page,
    int Size,
    IReadOnlyList<ReviewDto> Items);
