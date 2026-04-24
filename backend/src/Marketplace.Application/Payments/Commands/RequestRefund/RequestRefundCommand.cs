using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.RequestRefund;

public sealed record RequestRefundCommand(
    long PaymentId,
    decimal Amount,
    string Reason,
    Guid AdminUserId) : IRequest<Result>;
