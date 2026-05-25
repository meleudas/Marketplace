using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Catalog.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Products.Queries.GetPendingProducts;

public sealed class GetPendingProductsQueryHandler : IRequestHandler<GetPendingProductsQuery, Result<IReadOnlyList<PendingProductModerationDto>>>
{
    private readonly IProductRepository _productRepository;

    public GetPendingProductsQueryHandler(IProductRepository productRepository) =>
        _productRepository = productRepository;

    public async Task<Result<IReadOnlyList<PendingProductModerationDto>>> Handle(GetPendingProductsQuery request, CancellationToken ct)
    {
        try
        {
            var rows = await _productRepository.ListPendingReviewAsync(ct);
            var list = rows
                .Select(p => new PendingProductModerationDto(
                    p.Id.Value,
                    p.CompanyId.Value,
                    p.Name,
                    p.Slug,
                    p.SubmittedByUserId,
                    p.CreatedAt))
                .ToList();
            return Result<IReadOnlyList<PendingProductModerationDto>>.Success(list);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<PendingProductModerationDto>>.Failure($"Failed to list pending products: {ex.Message}");
        }
    }
}
