using FluentValidation;

namespace Marketplace.Application.Carts.Commands.ClearCart;

public sealed class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
{
    public ClearCartCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
