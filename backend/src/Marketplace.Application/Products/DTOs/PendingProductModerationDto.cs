namespace Marketplace.Application.Products.DTOs;

public sealed record PendingProductModerationDto(
    long ProductId,
    Guid CompanyId,
    string Name,
    string Slug,
    Guid? SubmittedByUserId,
    DateTime CreatedAt);
