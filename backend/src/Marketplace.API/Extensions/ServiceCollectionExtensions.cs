using Marketplace.API.Filters;
using Marketplace.API.Options;
using Marketplace.Application;
using Marketplace.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarketplaceApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CookieAuthOptions>(configuration.GetSection(CookieAuthOptions.SectionName));

        services.AddApplication();
        services.AddInfrastructure(configuration, ConfigureGoogleIfPresent(configuration));

        services.AddHttpContextAccessor();
        services.AddControllers(options => options.Filters.Add<ValidateModelAttribute>());

        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Marketplace API";
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });
        });

        services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length == 0)
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        }));

        return services;
    }

    private static Action<AuthenticationBuilder> ConfigureGoogleIfPresent(IConfiguration configuration) =>
        authenticationBuilder =>
        {
            var clientId = configuration["GoogleAuth:ClientId"];
            var clientSecret = configuration["GoogleAuth:ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                return;

            authenticationBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.CallbackPath = "/signin-google";
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = false;
            });
        };
}
