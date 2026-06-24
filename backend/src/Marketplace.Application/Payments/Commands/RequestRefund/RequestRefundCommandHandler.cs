using Marketplace.Application.Payments.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Payments.Commands.RequestRefund;

public sealed class RequestRefundCommandHandler : IRequestHandler<RequestRefundCommand, Result>
{
    private readonly IPaymentRefundExecutor _refundExecutor;

    public RequestRefundCommandHandler(IPaymentRefundExecutor refundExecutor) => _refundExecutor = refundExecutor;

    public async Task<Result> Handle(RequestRefundCommand request, CancellationToken ct)
    {
        var result = await _refundExecutor.ExecuteAsync(
            new PaymentRefundRequest(request.PaymentId, request.Amount, request.Reason, request.AdminUserId),
            ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error ?? "Refund failed");
    }
}
