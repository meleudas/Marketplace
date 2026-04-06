using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.RevokeCompanyApproval;

public sealed record RevokeCompanyApprovalCommand(Guid CompanyId) : IRequest<Result>;
