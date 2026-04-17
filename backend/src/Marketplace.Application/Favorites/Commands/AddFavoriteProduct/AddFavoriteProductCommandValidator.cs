using FluentValidation;

namespace Marketplace.Application.Favorites.Commands.AddFavoriteProduct;

public sealed class AddFavoriteProductCommandValidator : AbstractValidator<AddFavoriteProductCommand>
{
    public AddFavoriteProductCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
