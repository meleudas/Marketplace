using Marketplace.Application.Coupons.DTOs;
using Marketplace.Application.Coupons.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Coupons.Commands.ValidateCouponForCart;

public sealed record ValidateCouponForCartCommand(Guid ActorUserId, string Code) : IRequest<Result<CouponValidationResultDto>>;

public sealed class ValidateCouponForCartCommandHandler : IRequestHandler<ValidateCouponForCartCommand, Result<CouponValidationResultDto>>
{
    private readonly CouponCartValidationService _validationService;

    public ValidateCouponForCartCommandHandler(CouponCartValidationService validationService)
    {
        _validationService = validationService;
    }

    public async Task<Result<CouponValidationResultDto>> Handle(ValidateCouponForCartCommand request, CancellationToken ct)
    {
        var (result, _, _) = await _validationService.ValidateAsync(request.ActorUserId, request.Code, ct);
        if (result.IsValid)
            return Result<CouponValidationResultDto>.Success(result);

        var prefix = result.ErrorCode ?? "unprocessable";
        return Result<CouponValidationResultDto>.Failure($"{prefix}: {result.Message}");
    }
}
