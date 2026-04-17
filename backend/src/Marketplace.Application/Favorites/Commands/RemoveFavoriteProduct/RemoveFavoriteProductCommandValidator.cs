using FluentValidation;

namespace Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;

public sealed class RemoveFavoriteProductCommandValidator : AbstractValidator<RemoveFavoriteProductCommand>
{
    public RemoveFavoriteProductCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
    }
}
