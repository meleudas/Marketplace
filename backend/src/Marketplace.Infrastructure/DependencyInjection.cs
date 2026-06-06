using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Elastic.Clients.Elasticsearch;
using Marketplace.Application.Auth.Options;
using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Carts.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Coupons.Options;
using Marketplace.Application.Coupons.Services;
using Marketplace.Application.Products.Ports;
using Marketplace.Application.Payments.Ports;
using Marketplace.Application.Shipping.Ports;
using Marketplace.Application.Shipping.Options;
using Marketplace.Application.Reports.Options;
using Marketplace.Application.Behavior.Options;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Cart.Repositories;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Domain.Coupons.Repositories;
using Marketplace.Domain.Favorites.Repositories;
using Marketplace.Domain.Users.Repositories;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Orders.Repositories;
using Marketplace.Domain.Payments.Repositories;
using Marketplace.Domain.Reviews.Repositories;
using Marketplace.Domain.Reports.Repositories;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Domain.Behavior.Repositories;
using Marketplace.Infrastructure.Caching;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.External.OAuth;
using Marketplace.Infrastructure.External.Search;
using Marketplace.Infrastructure.External.Sms;
using Marketplace.Infrastructure.External.Telegram;
using Marketplace.Infrastructure.External.Storage;
using Marketplace.Infrastructure.External.Payments;
using Marketplace.Infrastructure.External.Shipping;
using Marketplace.Infrastructure.Identity;
using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Identity.Managers;
using Marketplace.Infrastructure.Identity.Services;
using Lib.Net.Http.WebPush;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Notifications;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Interceptors;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        services.Configure<LiqPayOptions>(configuration.GetSection(LiqPayOptions.SectionName));
        services.Configure<ShippingOptions>(configuration.GetSection(ShippingOptions.SectionName));
        services.Configure<CouponsOptions>(configuration.GetSection(CouponsOptions.SectionName));
        services.Configure<ReportsOptions>(configuration.GetSection(ReportsOptions.SectionName));
        services.Configure<BehaviorAnalyticsOptions>(configuration.GetSection(BehaviorAnalyticsOptions.SectionName));
        services.Configure<NovaPoshtaOptions>(configuration.GetSection(NovaPoshtaOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<WebPushOptions>(configuration.GetSection(WebPushOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .AddInterceptors(
                    sp.GetRequiredService<SoftDeleteInterceptor>(),
                    sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedEmail = configuration.GetValue<bool?>("Identity:RequireConfirmedEmail") ?? true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddUserManager<CustomUserManager>();

        var isTestingEnvironment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"],
            "Testing",
            StringComparison.OrdinalIgnoreCase);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((jwtBearerOptions, jwtOptionsAccessor) =>
            {
                var jwt = jwtOptionsAccessor.Value;
                if (string.IsNullOrWhiteSpace(jwt.SecretKey))
                    throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}:SecretKey' is missing.");

                _ = JwtParameterHelper.CreateSigningKey(jwt);
                jwtBearerOptions.TokenValidationParameters = JwtParameterHelper.CreateValidationParameters(jwt);
            });

        var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !isTestingEnvironment;
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
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
            services.AddSingleton<RedisCacheService>();
            services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<RedisCacheService>());
        }
        else
        {
            services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
        }

        services.AddHangfire(config =>
        {
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();
            if (!string.IsNullOrWhiteSpace(connectionString))
                config.UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString));
            else
                config.UseMemoryStorage();
        });
        services.AddHangfireServer();

        services.AddScoped<IdentityUserService>();
        services.AddScoped<ITokenPort, TokenService>();
        services.AddScoped<IAuthenticationPort, IdentityAuthService>();
        services.AddScoped<INotificationDispatcher, HangfireNotificationDispatcher>();
        services.AddScoped<NotificationJobs>();
        services.AddSingleton<PushServiceClient>();
        services.AddScoped<IPushDeliveryClient, LibWebPushDeliveryClient>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.Configure<AppNotificationOptions>(configuration.GetSection(AppNotificationOptions.SectionName));
        services.AddScoped<AppNotificationPayloadBuilder>();
        services.AddScoped<IAppNotificationUserContactReader, AppNotificationUserDirectory>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<INotificationChannel, WebPushNotificationChannel>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<INotificationChannel, InAppNotificationChannel>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<INotificationChannel, EmailNotificationChannel>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<INotificationChannel, TelegramAppChannel>());
        services.AddScoped<IInAppNotificationRepository, InAppNotificationRepository>();
        services.AddScoped<IAdminNotificationRecipientIds, AdminNotificationRecipientIds>();
        services.AddScoped<ICompanyOrderNotificationRecipientIds, CompanyOrderNotificationRecipientIds>();
        services.AddScoped<IAppNotificationScheduler, HangfireAppNotificationScheduler>();
        services.AddScoped<AppNotificationJobs>();
        services.AddScoped<InventoryJobs>();
        services.AddScoped<SearchIndexJobs>();
        services.AddScoped<PaymentJobs>();
        services.AddScoped<OutboxDispatcherJobs>();
        services.AddScoped<ProductImageJobs>();
        services.AddScoped<MediaCleanupJobs>();
        services.AddScoped<IOutboxEventProcessor, OutboxEventProcessor>();
        services.AddScoped<IAppCachePort, AppCachePort>();
        services.AddScoped<IOutboxWriter, OutboxRepository>();
        services.AddScoped<IInboxDeduplicator, InboxDeduplicator>();
        services.AddScoped<IHttpIdempotencyStore, HttpIdempotencyStore>();
        services.AddScoped<IAppTransactionPort, AppTransactionPort>();
        services.AddHttpClient<ILiqPayPort, LiqPayClient>();
        services.AddHttpClient<INovaPoshtaPort, NovaPoshtaClient>();
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
        services.AddScoped<IProductImageProcessingDispatcher, HangfireProductImageProcessingDispatcher>();
        services.AddScoped<IImageCompressionPolicy, AdaptiveImageCompressionPolicy>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyMemberRepository, CompanyMemberRepository>();
        services.AddScoped<ICompanyLegalProfileRepository, CompanyLegalProfileRepository>();
        services.AddScoped<ICompanyContractRepository, CompanyContractRepository>();
        services.AddScoped<ICompanyCommissionRateRepository, CompanyCommissionRateRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductDetailRepository, ProductDetailRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();
        services.AddScoped<ICartStockWatchRepository, CartStockWatchRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<ICouponUsageRepository, CouponUsageRepository>();
        services.AddScoped<ICartCouponLinkRepository, CartCouponLinkRepository>();
        services.AddScoped<CouponCartValidationService>();
        services.AddScoped<ICouponCheckoutService, CouponCheckoutService>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IOrderAddressSnapshotRepository, OrderAddressSnapshotRepository>();
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShippingQuoteRepository, ShippingQuoteRepository>();
        services.AddScoped<IShippingEventRepository, ShippingEventRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        services.AddScoped<IProductReviewRepository, ProductReviewRepository>();
        services.AddScoped<ICompanyReviewRepository, CompanyReviewRepository>();
        services.AddScoped<IReviewReplyRepository, ReviewReplyRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportActionAuditRepository, ReportActionAuditRepository>();
        services.AddScoped<IBehaviorEventRepository, BehaviorEventRepository>();
        services.AddScoped<IUserBehaviorDailyRepository, UserBehaviorDailyRepository>();
        services.AddScoped<ISearchQueryAggregateRepository, SearchQueryAggregateRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IInventoryReservationRepository, InventoryReservationRepository>();
        services.AddScoped<GoogleOAuthService>();
        services.AddScoped<IGoogleOAuthPort>(sp => sp.GetRequiredService<GoogleOAuthService>());
        services.AddScoped<ITelegramLinkCodeStore, TelegramLinkCodeStore>();
        services.AddHttpClient<TelegramBotSender>();
        services.AddScoped<Marketplace.Application.Behavior.Services.BehaviorPayloadRedactionService>();
        services.AddScoped<BehaviorAggregationJobs>();

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

        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        if (storageOptions.Enabled)
            services.AddSingleton<IObjectStorage, MinioObjectStorage>();
        else
            services.AddSingleton<IObjectStorage, DisabledObjectStorage>();

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
