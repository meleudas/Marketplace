using Marketplace.API.Middleware;

namespace Marketplace.API.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseMarketplaceMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ErrorHandlerMiddleware>();
        app.UseHttpsRedirection();
        app.UseCors(ServiceCollectionExtensions.GetCorsPolicyName());
        app.UseAuthentication();
        app.UseMiddleware<JwtCookieMiddleware>();
        app.UseAuthorization();
        return app;
    }
}
