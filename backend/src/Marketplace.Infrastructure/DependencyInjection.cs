using Hangfire;
using Hangfire.MemoryStorage;
using Elastic.Clients.Elasticsearch;
using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Favorites.Repositories;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Infrastructure.Caching;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.External.OAuth;
using Marketplace.Infrastructure.External.Search;
using Marketplace.Infrastructure.External.Sms;
using Marketplace.Infrastructure.External.Telegram;
using Marketplace.Infrastructure.Identity;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Managers;
using Marketplace.Infrastructure.Identity.Services;
using Marketplace.Infrastructure.Jobs;
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
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));
        services.Configure<CacheTtlOptions>(configuration.GetSection(CacheTtlOptions.SectionName));
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));

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
                options.Password.RequiredLength = 4;
                options.Password.RequireDigit = false;
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
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers.Authorization.ToString();
                        if (authHeader.StartsWith("Bearer Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authHeader["Bearer Bearer ".Length..].Trim();
                        }
                        else if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(context.Token))
                        {
                            context.Token = authHeader["Bearer ".Length..].Trim();
                        }
                        return Task.CompletedTask;
                    }
                };
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

        services.AddHangfire(config =>
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage());
        services.AddHangfireServer();

        services.AddScoped<IdentityUserService>();
        services.AddScoped<ITokenPort, TokenService>();
        services.AddScoped<IAuthenticationPort, IdentityAuthService>();
        services.AddScoped<INotificationDispatcher, HangfireNotificationDispatcher>();
        services.AddScoped<NotificationJobs>();
        services.AddScoped<InventoryJobs>();
        services.AddScoped<SearchIndexJobs>();
        services.AddScoped<IAppCachePort, AppCachePort>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ElasticsearchOptions>>().Value;
            var settings = new ElasticsearchClientSettings(new Uri(options.Url));
            if (!string.IsNullOrWhiteSpace(options.Username) && !string.IsNullOrWhiteSpace(options.Password))
                settings = settings.Authentication(new Elastic.Transport.BasicAuthentication(options.Username, options.Password));
            return new ElasticsearchClient(settings);
        });
        services.AddScoped<IProductSearchService, ElasticsearchProductSearchService>();
        services.AddScoped<IProductSearchIndexer, ElasticsearchProductSearchService>();
        services.AddScoped<IProductSearchIndexDispatcher, HangfireProductSearchIndexDispatcher>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyMemberRepository, CompanyMemberRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductDetailRepository, ProductDetailRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IOrderAddressSnapshotRepository, OrderAddressSnapshotRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
        services.AddScoped<GoogleOAuthService>();
        services.AddScoped<ITelegramLinkCodeStore, TelegramLinkCodeStore>();
        services.AddHttpClient<TelegramBotSender>();

        var telegramOptions = configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>() ?? new TelegramOptions();
        if (!string.IsNullOrWhiteSpace(telegramOptions.BotToken))
        {
            services.AddScoped<ITelegramPort>(sp => sp.GetRequiredService<TelegramBotSender>());
        }
        else
        {
            services.AddScoped<LoggingTelegramSender>();
            services.AddScoped<ITelegramPort>(sp => sp.GetRequiredService<LoggingTelegramSender>());
        }

        var sendGridOptions = configuration.GetSection(SendGridOptions.SectionName).Get<SendGridOptions>() ?? new SendGridOptions();
        var hasSendGrid = !string.IsNullOrWhiteSpace(sendGridOptions.ApiKey) &&
                          !string.IsNullOrWhiteSpace(sendGridOptions.FromEmail);

        if (hasSendGrid)
        {
            services.AddScoped<SendGridEmailSender>();
            services.AddScoped<IEmailPort>(sp => sp.GetRequiredService<SendGridEmailSender>());
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<SendGridEmailSender>());
            services.AddScoped<IEmailHealthProbe>(sp => sp.GetRequiredService<SendGridEmailSender>());
        }
        else
        {
            services.AddScoped<LoggingEmailSender>();
            services.AddScoped<IEmailPort>(sp => sp.GetRequiredService<LoggingEmailSender>());
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<LoggingEmailSender>());
            services.AddScoped<IEmailHealthProbe>(sp => sp.GetRequiredService<LoggingEmailSender>());
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
        var requiredRoles = new[] { "User", "Buyer", "Seller", "Moderator", "Admin" };
        foreach (var roleName in requiredRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var r = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!r.Succeeded)
                throw new InvalidOperationException(string.Join(" ", r.Errors.Select(e => e.Description)));
        }
    }
}
