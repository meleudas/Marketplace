using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetAdminCompanyById;

public sealed record GetAdminCompanyByIdQuery(Guid CompanyId) : IRequest<Result<CompanyDto>>;
