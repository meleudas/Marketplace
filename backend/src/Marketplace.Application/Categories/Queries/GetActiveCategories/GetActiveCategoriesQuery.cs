using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Queries.GetActiveCategories;

public sealed record GetActiveCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;
