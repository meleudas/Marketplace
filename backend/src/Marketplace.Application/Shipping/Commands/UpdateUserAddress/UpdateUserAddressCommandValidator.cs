using FluentValidation;
using Marketplace.Domain.Shipping.Enums;

namespace Marketplace.Application.Shipping.Commands.UpdateUserAddress;

public sealed class UpdateUserAddressCommandValidator : AbstractValidator<UpdateUserAddressCommand>
{
    public UpdateUserAddressCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.AddressId).GreaterThan(0);
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(x => Enum.TryParse<UserAddressType>(x, true, out _))
            .WithMessage("Invalid address type");
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
