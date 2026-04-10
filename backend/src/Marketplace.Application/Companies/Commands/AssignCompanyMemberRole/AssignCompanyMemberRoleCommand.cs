using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;

public sealed record AssignCompanyMemberRoleCommand(
    Guid CompanyId,
    Guid TargetUserId,
    CompanyMembershipRole Role,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<CompanyMemberDto>>;
