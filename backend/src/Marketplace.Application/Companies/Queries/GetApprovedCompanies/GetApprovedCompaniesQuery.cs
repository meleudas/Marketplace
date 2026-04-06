using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetApprovedCompanies;

public sealed record GetApprovedCompaniesQuery : IRequest<Result<IReadOnlyList<CompanyDto>>>;
