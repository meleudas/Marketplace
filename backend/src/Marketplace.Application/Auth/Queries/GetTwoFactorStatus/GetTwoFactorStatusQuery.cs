using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Auth.Queries.GetTwoFactorStatus;

/// <param name="IdentityUserId">Значення claim <c>sub</c> у JWT.</param>
public record GetTwoFactorStatusQuery(Guid IdentityUserId) : IRequest<Result<TwoFactorStatusDto>>;
