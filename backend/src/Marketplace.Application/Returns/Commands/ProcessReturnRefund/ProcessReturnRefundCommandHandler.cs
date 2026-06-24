using Marketplace.Application.Payments.Services;
using Marketplace.Application.Returns.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Returns.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Returns.Commands.ProcessReturnRefund;

public sealed record ProcessReturnRefundCommand(long ReturnId, Guid ActorUserId, decimal? Amount) : IRequest<Result<ReturnRequestDetailDto>>;

public sealed class ProcessReturnRefundCommandHandler : IRequestHandler<ProcessReturnRefundCommand, Result<ReturnRequestDetailDto>>
{
    private readonly IReturnRequestRepository _returnRepository;
    private readonly IReturnLineItemRepository _returnLineItemRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentRefundExecutor _refundExecutor;

    public ProcessReturnRefundCommandHandler(
        IReturnRequestRepository returnRepository,
        IReturnLineItemRepository returnLineItemRepository,
        IPaymentRepository paymentRepository,
        IPaymentRefundExecutor refundExecutor)
    {
        _returnRepository = returnRepository;
        _returnLineItemRepository = returnLineItemRepository;
        _paymentRepository = paymentRepository;
        _refundExecutor = refundExecutor;
    }

    public async Task<Result<ReturnRequestDetailDto>> Handle(ProcessReturnRefundCommand request, CancellationToken ct)
    {
        var entity = await _returnRepository.GetByIdAsync(ReturnRequestId.From(request.ReturnId), ct);
        if (entity is null)
            return Result<ReturnRequestDetailDto>.Failure("Return request not found");

        var payment = await _paymentRepository.GetByOrderIdAsync(entity.OrderId, ct);
        if (payment is null)
            return Result<ReturnRequestDetailDto>.Failure("Payment not found");

        try
        {
            var refundResult = await _refundExecutor.ExecuteAsync(
                new PaymentRefundRequest(payment.Id.Value, request.Amount, $"RMA:{entity.Id.Value}", request.ActorUserId),
                ct);
            if (!refundResult.IsSuccess || refundResult.Value is null)
                return Result<ReturnRequestDetailDto>.Failure(refundResult.Error ?? "Refund failed");

            entity.MarkRefunded(refundResult.Value.RefundId);
            await _returnRepository.UpdateAsync(entity, ct);

            var lines = await _returnLineItemRepository.ListByReturnRequestIdAsync(entity.Id, ct);
            return Result<ReturnRequestDetailDto>.Success(ReturnMapper.ToDetail(entity, lines));
        }
        catch (Exception ex)
        {
            return Result<ReturnRequestDetailDto>.Failure(ex.Message);
        }
    }
}
