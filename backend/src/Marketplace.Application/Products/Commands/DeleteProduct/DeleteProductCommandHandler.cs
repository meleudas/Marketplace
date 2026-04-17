using Marketplace.Application.Catalog.Cache;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Products.Authorization;
using Marketplace.Application.Products.Ports;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductAccessService _access;
    private readonly IProductRepository _productRepository;
    private readonly IAppCachePort _cache;
    private readonly IProductSearchIndexDispatcher _searchIndexDispatcher;

    public DeleteProductCommandHandler(IProductAccessService access, IProductRepository productRepository, IAppCachePort cache, IProductSearchIndexDispatcher searchIndexDispatcher)
    {
        _access = access;
        _productRepository = productRepository;
        _cache = cache;
        _searchIndexDispatcher = searchIndexDispatcher;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, ProductPermission.WriteProduct, ct))
                return Result.Failure("Forbidden");

            var product = await _productRepository.GetByIdAsync(ProductId.From(request.ProductId), ct);
            if (product is null || product.CompanyId.Value != request.CompanyId)
                return Result.Failure("Product not found");

            var oldSlug = product.Slug;
            product.SoftDelete();
            await _productRepository.UpdateAsync(product, ct);

            await _cache.RemoveAsync(CatalogCacheKeys.ProductList, ct);
            await _cache.RemoveAsync(CatalogCacheKeys.ProductDetailPrefix + oldSlug, ct);
            await _searchIndexDispatcher.EnqueueDeleteProductAsync(product.Id.Value, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete product: {ex.Message}");
        }
    }
}
