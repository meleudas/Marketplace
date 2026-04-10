using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetCompanyMembers;

public sealed record GetCompanyMembersQuery(
    Guid CompanyId,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result<IReadOnlyList<CompanyMemberDto>>>;
