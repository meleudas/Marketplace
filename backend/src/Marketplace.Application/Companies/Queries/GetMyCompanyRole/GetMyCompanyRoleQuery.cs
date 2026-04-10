using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Queries.GetMyCompanyRole;

public sealed record GetMyCompanyRoleQuery(Guid CompanyId, Guid ActorUserId) : IRequest<Result<CompanyMemberDto>>;
