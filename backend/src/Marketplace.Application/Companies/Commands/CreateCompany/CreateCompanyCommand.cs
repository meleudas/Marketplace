using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.CreateCompany;

public sealed record CreateCompanyCommand(
    string Name,
    string Slug,
    string Description,
    string? ImageUrl,
    string ContactEmail,
    string ContactPhone,
    CompanyAddressDto Address,
    string? MetaRaw) : IRequest<Result<CompanyDto>>;
