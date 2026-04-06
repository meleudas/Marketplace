using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Queries.GetAllCategories;

public sealed record GetAllCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;
