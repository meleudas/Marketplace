using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.DeleteCompany;

public sealed record DeleteCompanyCommand(Guid CompanyId) : IRequest<Result>;
