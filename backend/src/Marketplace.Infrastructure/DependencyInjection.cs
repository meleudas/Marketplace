using Marketplace.Application.Auth.Ports;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Infrastructure.Caching;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.External.OAuth;
using Marketplace.Infrastructure.External.Sms;
using Marketplace.Infrastructure.Identity;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Managers;
using Marketplace.Infrastructure.Identity.Services;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Interceptors;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuthenticationBuilder>? configureAuthentication = null)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SendGridOptions>(configuration.GetSection(SendGridOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddUserManager<CustomUserManager>();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing.");

        _ = JwtParameterHelper.CreateSigningKey(jwt);

        var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtParameterHelper.CreateValidationParameters(jwt);
                options.RequireHttpsMetadata = true;
            });

        configureAuthentication?.Invoke(authenticationBuilder);

        services.AddAuthorization();

        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSingleton<MemoryCacheService>();
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
            services.AddSingleton<RedisCacheService>();
            services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<RedisCacheService>());
        }
        else
        {
            services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
        }

        services.AddScoped<IdentityUserService>();
        services.AddScoped<ITokenPort, TokenService>();
        services.AddScoped<IAuthenticationPort, IdentityAuthService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<GoogleOAuthService>();

        var sendGridOptions = configuration.GetSection(SendGridOptions.SectionName).Get<SendGridOptions>() ?? new SendGridOptions();
        var hasSendGrid = !string.IsNullOrWhiteSpace(sendGridOptions.ApiKey) &&
                          !string.IsNullOrWhiteSpace(sendGridOptions.FromEmail);

        if (hasSendGrid)
        {
            services.AddScoped<SendGridEmailSender>();
            services.AddScoped<IEmailPort>(sp => sp.GetRequiredService<SendGridEmailSender>());
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SendGridEmailSender>());
        }
        else
        {
            services.AddScoped<LoggingEmailSender>();
            services.AddScoped<IEmailPort>(sp => sp.GetRequiredService<LoggingEmailSender>());
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<LoggingEmailSender>());
        }

        services.AddScoped<LoggingSmsSender>();
        services.AddScoped<ISmsPort>(sp => sp.GetRequiredService<LoggingSmsSender>());
        services.AddScoped<ISmsSender>(sp => sp.GetRequiredService<LoggingSmsSender>());

        return services;
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(ct);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        const string roleName = "User";
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var r = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!r.Succeeded)
                throw new InvalidOperationException(string.Join(" ", r.Errors.Select(e => e.Description)));
        }
    }
}
