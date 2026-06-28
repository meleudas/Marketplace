using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetCatalogCompanyByIdOrSlug;

public sealed record GetCatalogCompanyByIdOrSlugQuery(string IdOrSlug) : IRequest<Result<CompanyDto>>;
