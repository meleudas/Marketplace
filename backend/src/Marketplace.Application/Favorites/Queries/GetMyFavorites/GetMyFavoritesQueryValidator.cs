using FluentValidation;

namespace Marketplace.Application.Favorites.Queries.GetMyFavorites;

public sealed class GetMyFavoritesQueryValidator : AbstractValidator<GetMyFavoritesQuery>
{
    public GetMyFavoritesQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
