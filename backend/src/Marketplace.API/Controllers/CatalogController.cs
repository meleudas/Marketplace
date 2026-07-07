using Marketplace.API.Extensions;
using Marketplace.Application.Inventory.Queries.GetProductAvailability;
using Marketplace.Application.Behavior.Commands.TrackProductView;
using Marketplace.Application.Behavior.Commands.TrackSearchQuery;
using Marketplace.Application.Behavior.Options;
using Marketplace.Application.Products.Queries.GetCatalogProductBySlug;
using Marketplace.Application.Products.Queries.GetCatalogProducts;
using Marketplace.Application.Products.Queries.ListCatalogNewProducts;
using Marketplace.Application.Products.Queries.ListCatalogOnSaleProducts;
using Marketplace.Application.Products.Queries.ListCatalogPopularProducts;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Application.Products.Queries.GetSimilarProductsById;
using Marketplace.Application.Products.Queries.GetSimilarProductsBySlug;
using Marketplace.Application.Products.Queries.GetPersonalizedRecommendations;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Categories.Queries.GetCatalogCategoryById;
using Marketplace.Application.Companies.Queries.GetApprovedCompanies;
using Marketplace.Application.Companies.Queries.GetCatalogCompanyByIdOrSlug;
using Marketplace.Application.Common.Observability;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Marketplace.API.Controllers;

[ApiController]
[Tags("Catalog")]
[Route("catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CatalogController> _logger;
    private readonly BehaviorAnalyticsOptions _behaviorOptions;

    public CatalogController(
        ISender sender,
        ILogger<CatalogController> logger,
        IOptions<BehaviorAnalyticsOptions>? behaviorOptions = null)
    {
        _sender = sender;
        _logger = logger;
        _behaviorOptions = (behaviorOptions ?? Microsoft.Extensions.Options.Options.Create(new BehaviorAnalyticsOptions())).Value;
    }

    [HttpGet("companies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_companies"));
        var result = await _sender.Send(new GetApprovedCompaniesQuery(), ct);
        RecordCatalogResult("get_companies", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("companies/{idOrSlug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyByIdOrSlug(string idOrSlug, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_company"));
        var result = await _sender.Send(new GetCatalogCompanyByIdOrSlugQuery(idOrSlug), ct);
        RecordCatalogResult("get_company", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_categories"));
        var result = await _sender.Send(new GetActiveCategoriesQuery(), ct);
        RecordCatalogResult("get_categories", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("categories/{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(long id, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_category_by_id"));
        var result = await _sender.Send(new GetCatalogCategoryByIdQuery(id), ct);
        RecordCatalogResult("get_category_by_id", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/products/{productId:long}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductAvailability(Guid companyId, long productId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_product_availability"));
        var result = await _sender.Send(new GetProductAvailabilityQuery(companyId, productId), ct);
        RecordCatalogResult("get_product_availability", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_products"));
        var result = await _sender.Send(new GetCatalogProductsQuery(), ct);
        RecordCatalogResult("get_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/on-sale")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOnSaleProducts([FromQuery] ListCatalogOnSaleProductsRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_on_sale_products"));
        var result = await _sender.Send(new ListCatalogOnSaleProductsQuery(
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.MinDiscountPercent,
            request.AvailabilityStatus,
            request.Sort,
            request.Page ?? 1,
            request.PageSize ?? 20,
            request.SearchAfter), ct);
        RecordCatalogResult("get_on_sale_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/new")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNewProducts([FromQuery] ListCatalogBrowsableProductsRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_new_products"));
        var result = await _sender.Send(new ListCatalogNewProductsQuery(
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.AvailabilityStatus,
            request.Page ?? 1,
            request.PageSize ?? 20,
            request.SearchAfter), ct);
        RecordCatalogResult("get_new_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/popular")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopularProducts([FromQuery] ListCatalogBrowsableProductsRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_popular_products"));
        var result = await _sender.Send(new ListCatalogPopularProductsQuery(
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.AvailabilityStatus,
            request.Page ?? 1,
            request.PageSize ?? 20,
            request.SearchAfter), ct);
        RecordCatalogResult("get_popular_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/{id:long}/similar")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSimilarProductsById(long id, [FromQuery] int limit = 12, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_similar_products"));
        var result = await _sender.Send(new GetSimilarProductsByIdQuery(id, limit), ct);
        RecordCatalogResult("get_similar_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/{slug}/similar")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSimilarProductsBySlug(string slug, [FromQuery] int limit = 12, CancellationToken ct = default)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_similar_products"));
        var result = await _sender.Send(new GetSimilarProductsBySlugQuery(slug, limit), ct);
        RecordCatalogResult("get_similar_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("recommendations/me")]
    [Authorize]
    public async Task<IActionResult> GetPersonalizedRecommendations([FromQuery] int limit = 12, CancellationToken ct = default)
    {
        if (!User.TryGetUserId(out var userId))
            return Unauthorized();

        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.RecommendationInferenceLatencyMs);
        var result = await _sender.Send(new GetPersonalizedRecommendationsQuery(userId, limit), ct);
        return result.ToActionResult();
    }

    [HttpGet("products/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_product_by_slug"));
        var result = await _sender.Send(new GetCatalogProductBySlugQuery(slug), ct);
        await TryTrackProductViewAsync(result, "catalog:product_slug", ct);
        RecordCatalogResult("get_product_by_slug", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchProducts([FromQuery] SearchCatalogProductsRequest request, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "search_products"));
        var searchName = string.IsNullOrWhiteSpace(request.Name) ? request.Query : request.Name;
        var result = await _sender.Send(new SearchCatalogProductsQuery(
            searchName,
            request.Query,
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.AvailabilityStatus,
            request.Authors,
            request.Format,
            request.Genres,
            request.Tags,
            request.Sort,
            request.Page ?? 1,
            request.PageSize ?? 20,
            request.SearchAfter), ct);
        RecordCatalogResult("search_products", result.IsSuccess, result.Error);
        if (result.IsSuccess && string.IsNullOrWhiteSpace(result.Value?.NextSearchAfter) && !string.IsNullOrWhiteSpace(searchName))
            MarketplaceMetrics.CatalogSearchFallbacks.Add(1, [new KeyValuePair<string, object?>("reason", "possible_db_fallback")]);
        await TryTrackSearchQueryAsync(searchName ?? string.Empty, request, ct);
        return result.ToActionResult();
    }

    private void RecordCatalogResult(string operation, bool success, string? error)
    {
        if (success)
        {
            MarketplaceMetrics.CatalogOps.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", "success")]);
            return;
        }

        MarketplaceMetrics.CatalogErrors.Add(1, [new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("reason", "application_failure")]);
        _logger.LogWarning("Catalog operation {Operation} failed. Error: {Error}", operation, error);
    }

    private async Task TryTrackProductViewAsync(Result<ProductDto> result, string source, CancellationToken ct)
    {
        if (!_behaviorOptions.BehaviorTrackingEnabled || !result.IsSuccess || result.Value is null)
            return;

        var actorId = User.TryGetUserId(out var uid) ? uid : (Guid?)null;
        var payload = JsonSerializer.Serialize(new { productId = result.Value.Product.Id, slug = result.Value.Product.Slug });
        var track = await _sender.Send(
            new TrackProductViewCommand(
                actorId,
                HttpContext.TraceIdentifier,
                result.Value.Product.Id,
                source,
                payload,
                null),
            ct);
        if (!track.IsSuccess)
            _logger.LogDebug("Product view tracking skipped: {Error}", track.Error);
    }

    private async Task TryTrackSearchQueryAsync(string query, SearchCatalogProductsRequest request, CancellationToken ct)
    {
        if (!_behaviorOptions.BehaviorTrackingEnabled || string.IsNullOrWhiteSpace(query))
            return;

        var actorId = User.TryGetUserId(out var uid) ? uid : (Guid?)null;
        var payload = JsonSerializer.Serialize(new
        {
            query,
            request.CategoryIds,
            request.CompanyId,
            request.MinPrice,
            request.MaxPrice,
            request.Sort,
            request.Page,
            request.PageSize
        });
        var track = await _sender.Send(
            new TrackSearchQueryCommand(actorId, HttpContext.TraceIdentifier, query, payload, null),
            ct);
        if (!track.IsSuccess)
            _logger.LogDebug("Search tracking skipped: {Error}", track.Error);
    }
}

public sealed record ListCatalogBrowsableProductsRequest(
    List<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    int? Page,
    int? PageSize,
    string? SearchAfter);

public sealed record ListCatalogOnSaleProductsRequest(
    List<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinDiscountPercent,
    string? AvailabilityStatus,
    string? Sort,
    int? Page,
    int? PageSize,
    string? SearchAfter);

public sealed record SearchCatalogProductsRequest(
    string? Name,
    string? Query,
    List<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    List<string>? Authors,
    string? Format,
    List<string>? Genres,
    List<string>? Tags,
    string? Sort,
    int? Page,
    int? PageSize,
    string? SearchAfter);
