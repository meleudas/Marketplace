using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    string? ImageUrl,
    long? ParentCategoryId,
    string? Description,
    string? MetaRaw,
    int SortOrder,
    bool IsActive) : IRequest<Result<CategoryDto>>;
