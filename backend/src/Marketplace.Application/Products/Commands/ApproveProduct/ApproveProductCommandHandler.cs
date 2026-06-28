using System.Text.Json;
using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.ApproveProduct;

public sealed class ApproveProductCommandHandler : IRequestHandler<ApproveProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IProductSearchIndexDispatcher _searchIndexDispatcher;
    private readonly IAppNotificationScheduler _appNotifications;

    public ApproveProductCommandHandler(
        IProductRepository productRepository,
        IAppCachePort cache,
        IProductSearchIndexDispatcher searchIndexDispatcher,
        IAppNotificationScheduler appNotifications)
    {
        _productRepository = productRepository;
        _cache = cache;
        _searchIndexDispatcher = searchIndexDispatcher;
        _appNotifications = appNotifications;
    }

    public async Task<Result> Handle(ApproveProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is null)
                return Result.Failure("Product not found");

            try
            {
                product.Approve();
            }
            catch (DomainException ex)
            {
                return Result.Failure(ex.Message);
            }

            await _productRepository.UpdateAsync(product, ct);

            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + product.Slug, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.SimilarProductsPrefix + product.Id.Value, ct);
            await _searchIndexDispatcher.EnqueueUpsertProductAsync(product.Id.Value, ct);

            if (product.SubmittedByUserId is { } authorId)
            {
                await _appNotifications.ScheduleAsync(
                    new AppNotificationRequest
                    {
                        TemplateKey = AppNotificationTemplateKeys.UserProductApproved,
                        CorrelationId = AppNotificationCorrelationIds.ProductApprovedForUser(product.Id.Value, authorId),
                        Channels = AppNotificationChannelKind.Push | AppNotificationChannelKind.InApp,
                        Audience = AppNotificationAudienceKind.User,
                        TargetUserId = authorId,
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            productId = product.Id.Value,
                            companyId = product.CompanyId.Value,
                            name = product.Name,
                            slug = product.Slug
                        })
                    },
                    ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to approve product: {ex.Message}");
        }
    }
}
