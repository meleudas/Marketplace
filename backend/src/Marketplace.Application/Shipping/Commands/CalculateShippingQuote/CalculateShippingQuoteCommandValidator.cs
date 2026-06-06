using FluentValidation;

namespace Marketplace.Application.Shipping.Commands.CalculateShippingQuote;

public sealed class CalculateShippingQuoteCommandValidator : AbstractValidator<CalculateShippingQuoteCommand>
{
    public CalculateShippingQuoteCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ShippingMethodId).GreaterThan(0);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(255);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}
