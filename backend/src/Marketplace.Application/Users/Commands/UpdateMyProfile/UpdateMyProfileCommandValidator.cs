using FluentValidation;
using Marketplace.Application.Auth.Validation;

namespace Marketplace.Application.Users.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.IdentityUserId)
            .NotEmpty().WithMessage("User id is required");

        RuleFor(x => x.UserName)
            .ApplyUserNameRules();

        RuleFor(x => x.PhoneNumber)
            .ApplyOptionalPhoneNumberRules(x => x.PhoneNumber);
    }
}
