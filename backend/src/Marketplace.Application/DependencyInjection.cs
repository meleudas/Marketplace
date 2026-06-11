using FluentValidation;
using Marketplace.Application.Common.Behaviors;
using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Authorization;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Reviews.Authorization;
using Marketplace.Application.Reviews.Services;
using Marketplace.Application.Users.Services;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Carts.Services;
using Marketplace.Application.Inventory.Services;
using Marketplace.Application.Chats.Policies;
using Marketplace.Application.Support.Policies;
using Marketplace.Application.Support.Services;
using Marketplace.Application.Notifications.Ports;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 1. MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // 2. Pipeline Behaviors
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 3. FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // 4. Application services (explicit controller -> service binding)
        services.AddScoped<IUserReadService, UserReadService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IInventoryAccessService, InventoryAccessService>();
        services.AddScoped<IProductAccessService, ProductAccessService>();
        services.AddScoped<IOrderAccessService, OrderAccessService>();
        services.AddScoped<IOrderCacheInvalidationService, OrderCacheInvalidationService>();
        services.AddScoped<IOrderStatusHistoryWriter, OrderStatusHistoryWriter>();
        services.AddScoped<IReviewAccessService, ReviewAccessService>();
        services.AddScoped<IReviewPurchaseVerificationService, ReviewPurchaseVerificationService>();
        services.AddScoped<IReviewRatingAggregationService, ReviewRatingAggregationService>();
        services.AddScoped<IOrderPaymentStateApplier, OrderPaymentStateApplier>();
        services.AddScoped<ICartStockWatchSyncService, CartStockWatchSyncService>();
        services.AddScoped<IRestockAvailabilityNotifier, RestockAvailabilityNotifier>();
        services.AddScoped<ChatAccessPolicy>();
        services.AddScoped<ChatAntiSpamPolicy>();
        services.AddScoped<ChatContentModerationPolicy>();
        services.AddScoped<SupportTicketAccessPolicy>();
        services.AddScoped<SupportTicketStatePolicy>();
        services.AddScoped<SupportEscalationPolicy>();
        services.AddScoped<SupportAntiAbusePolicy>();
        services.AddScoped<HelpdeskWebhookSignatureValidator>();
        services.AddScoped<SupportHelpdeskOutboxPublisher>();

        return services;
    }
}
