using System.Text.Json;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.RejectProduct;

public sealed class RejectProductCommandHandler : IRequestHandler<RejectProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IAppNotificationScheduler _appNotifications;

    public RejectProductCommandHandler(
        IProductRepository productRepository,
        IAppCachePort cache,
        IAppNotificationScheduler appNotifications)
    {
        _productRepository = productRepository;
        _cache = cache;
        _appNotifications = appNotifications;
    }

    public async Task<Result> Handle(RejectProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is null)
                return Result.Failure("Product not found");

            try
            {
                product.Reject(request.Reason);
            }
            catch (DomainException ex)
            {
                return Result.Failure(ex.Message);
            }

            await _productRepository.UpdateAsync(product, ct);

            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);

            if (product.SubmittedByUserId is { } authorId)
            {
                await _appNotifications.ScheduleAsync(
                    new AppNotificationRequest
                    {
                        TemplateKey = AppNotificationTemplateKeys.UserProductRejected,
                        CorrelationId = AppNotificationCorrelationIds.ProductRejectedForUser(product.Id.Value, authorId),
                        Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                        Audience = AppNotificationAudienceKind.User,
                        TargetUserId = authorId,
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            productId = product.Id.Value,
                            companyId = product.CompanyId.Value,
                            name = product.Name,
                            slug = product.Slug,
                            reason = product.ModerationRejectionReason
                        })
                    },
                    ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to reject product: {ex.Message}");
        }
    }
}
