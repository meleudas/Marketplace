using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.ApproveCompany;

public sealed record ApproveCompanyCommand(Guid CompanyId, Guid AdminUserId) : IRequest<Result>;
