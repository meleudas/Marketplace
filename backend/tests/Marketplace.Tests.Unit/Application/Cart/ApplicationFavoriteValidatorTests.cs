using Marketplace.Application.Favorites.Commands.AddFavoriteProduct;
using Marketplace.Application.Favorites.Commands.RemoveFavoriteProduct;
using Marketplace.Application.Favorites.Queries.GetMyFavorites;

namespace Marketplace.Tests;

[Trait("Suite", "Favorites")]
public class ApplicationFavoriteValidatorTests
{
    [Fact]
    public void AddFavorite_Validator_Rejects_Invalid_Data()
    {
        var validator = new AddFavoriteProductCommandValidator();
        var result = validator.Validate(new AddFavoriteProductCommand(Guid.Empty, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
        Assert.Contains(result.Errors, e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void RemoveFavorite_Validator_Rejects_Invalid_Data()
    {
        var validator = new RemoveFavoriteProductCommandValidator();
        var result = validator.Validate(new RemoveFavoriteProductCommand(Guid.Empty, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
        Assert.Contains(result.Errors, e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void GetMyFavorites_Validator_Rejects_Empty_User()
    {
        var validator = new GetMyFavoritesQueryValidator();
        var result = validator.Validate(new GetMyFavoritesQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
    }
}
