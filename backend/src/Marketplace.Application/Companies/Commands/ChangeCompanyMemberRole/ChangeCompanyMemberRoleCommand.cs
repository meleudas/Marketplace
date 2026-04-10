using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Companies.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.ChangeCompanyMemberRole;

public sealed record ChangeCompanyMemberRoleCommand(
    Guid CompanyId,
    Guid TargetUserId,
    CompanyMembershipRole Role,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<CompanyMemberDto>>;
