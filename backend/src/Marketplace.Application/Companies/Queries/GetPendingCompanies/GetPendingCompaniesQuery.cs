using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetPendingCompanies;

public sealed record GetPendingCompaniesQuery : IRequest<Result<IReadOnlyList<CompanyDto>>>;
