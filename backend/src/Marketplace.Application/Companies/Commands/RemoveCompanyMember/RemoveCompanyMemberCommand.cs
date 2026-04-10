using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.RemoveCompanyMember;

public sealed record RemoveCompanyMemberCommand(
    Guid CompanyId,
    Guid TargetUserId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result>;
