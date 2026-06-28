using Marketplace.Application.Categories.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Categories.Queries.GetAdminCategoryById;

public sealed record GetAdminCategoryByIdQuery(long CategoryId) : IRequest<Result<CategoryDto>>;
