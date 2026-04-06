using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    long CategoryId,
    string Name,
    string Slug,
    string? ImageUrl,
    long? ParentCategoryId,
    string? Description,
    string? MetaRaw,
    int SortOrder) : IRequest<Result<CategoryDto>>;
