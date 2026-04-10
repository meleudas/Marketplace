using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetCatalogProductBySlug;

public sealed record GetCatalogProductBySlugQuery(string Slug) : IRequest<Result<ProductDto>>;
