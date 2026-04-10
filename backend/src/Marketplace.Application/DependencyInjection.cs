using FluentValidation;
using Marketplace.Application.Common.Behaviors;
using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Users.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
