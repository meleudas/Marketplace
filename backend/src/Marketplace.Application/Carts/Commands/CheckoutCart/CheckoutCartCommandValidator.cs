using FluentValidation;

namespace Marketplace.Application.Carts.Commands.CheckoutCart;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Address.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Address.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Address.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Address.Street).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Address.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address.PostalCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Address.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
