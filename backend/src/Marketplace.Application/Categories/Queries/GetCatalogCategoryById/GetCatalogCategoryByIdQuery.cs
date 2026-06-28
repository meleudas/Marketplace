using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Queries.GetCatalogCategoryById;

public sealed record GetCatalogCategoryByIdQuery(long CategoryId) : IRequest<Result<CategoryDto>>;
