using FluentValidation;

namespace Marketplace.Application.Shipping.Commands.SetDefaultUserAddress;

public sealed class SetDefaultUserAddressCommandValidator : AbstractValidator<SetDefaultUserAddressCommand>
{
    public SetDefaultUserAddressCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.AddressId).GreaterThan(0);
    }
}
