using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetAllCompanies;

public sealed record GetAllCompaniesQuery : IRequest<Result<IReadOnlyList<CompanyDto>>>;
