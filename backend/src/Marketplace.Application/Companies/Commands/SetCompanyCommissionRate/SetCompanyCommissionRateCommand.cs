using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;

public sealed record SetCompanyCommissionRateCommand(
    Guid CompanyId,
    decimal CommissionPercent,
    DateTime EffectiveFrom,
    string? Reason,
    Guid AdminUserId) : IRequest<Result>;
