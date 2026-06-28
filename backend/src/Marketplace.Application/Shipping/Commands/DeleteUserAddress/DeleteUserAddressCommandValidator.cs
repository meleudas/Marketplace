using FluentValidation;

namespace Marketplace.Application.Shipping.Commands.DeleteUserAddress;

public sealed class DeleteUserAddressCommandValidator : AbstractValidator<DeleteUserAddressCommand>
{
    public DeleteUserAddressCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.AddressId).GreaterThan(0);
    }
}
