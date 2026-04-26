using Marketplace.API.Filters;
using Marketplace.API.Options;
using Marketplace.Application;
using Marketplace.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using Marketplace.Infrastructure.Observability;

namespace Marketplace.API.Extensions;

public static class ServiceCollectionExtensions
{
    private const string CorsPolicyName = "MarketplaceCors";

    public static IServiceCollection AddMarketplaceApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CookieAuthOptions>(configuration.GetSection(CookieAuthOptions.SectionName));

        services.AddApplication();
        services.AddInfrastructure(configuration, ConfigureGoogleIfPresent(configuration));

        services.AddHttpContextAccessor();
        services.AddControllers(options => options.Filters.Add<ValidateModelAttribute>());
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Marketplace API",
                Version = "v1"
            });

            // HTTP Bearer: Swagger UI сам додає префікс "Bearer " — в поле вводу лише JWT (access token).
            var bearerScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Вставте лише access token (JWT). Префікс Bearer не потрібен — його додасть Swagger UI."
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);

            options.AddSecurityRequirement(document =>
            {
                var schemeRef = new OpenApiSecuritySchemeReference("Bearer", document, externalResource: null);
                return new OpenApiSecurityRequirement { [schemeRef] = [] };
            });
        });

        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Marketplace API";
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });
        });

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                var origins = ResolveAllowedOrigins(configuration);
                if (origins.Length == 0)
                {
                    throw new InvalidOperationException(
                        "CORS is not configured. Set Cors:AllowedOrigins or Frontend:BaseUrl.");
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(MarketplaceMetrics.MeterName)
                    .AddPrometheusExporter();
            });

        return services;
    }

    public static string GetCorsPolicyName() => CorsPolicyName;

    private static string[] ResolveAllowedOrigins(IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var frontendBaseUrl = configuration["Frontend:BaseUrl"];

        var origins = configuredOrigins
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim().TrimEnd('/'))
            .ToList();

        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            origins.Add(frontendBaseUrl.Trim().TrimEnd('/'));
        }

        return origins
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
