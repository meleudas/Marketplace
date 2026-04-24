using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.SyncPaymentStatus;

public sealed record SyncPaymentStatusCommand(long PaymentId) : IRequest<Result>;
