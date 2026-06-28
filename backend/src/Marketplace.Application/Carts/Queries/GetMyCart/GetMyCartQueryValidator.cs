using FluentValidation;

namespace Marketplace.Application.Carts.Queries.GetMyCart;

public sealed class GetMyCartQueryValidator : AbstractValidator<GetMyCartQuery>
{
    public GetMyCartQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
