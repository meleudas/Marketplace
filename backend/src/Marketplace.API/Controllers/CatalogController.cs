using Marketplace.API.Extensions;
using Marketplace.Application.Inventory.Queries.GetProductAvailability;
using Marketplace.Application.Products.Queries.GetCatalogProductBySlug;
using Marketplace.Application.Products.Queries.GetCatalogProducts;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Categories.Queries.GetCatalogCategoryById;
using Marketplace.Application.Companies.Queries.GetApprovedCompanies;
using Marketplace.Application.Companies.Queries.GetCatalogCompanyByIdOrSlug;
using Marketplace.Application.Common.Observability;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("catalog")]
[AllowAnonymous]
public sealed class CatalogController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ISender sender, ILogger<CatalogController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_companies"));
        var result = await _sender.Send(new GetApprovedCompaniesQuery(), ct);
        RecordCatalogResult("get_companies", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("companies/{idOrSlug}")]
    public async Task<IActionResult> GetCompanyByIdOrSlug(string idOrSlug, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_company"));
        var result = await _sender.Send(new GetCatalogCompanyByIdOrSlugQuery(idOrSlug), ct);
        RecordCatalogResult("get_company", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_categories"));
        var result = await _sender.Send(new GetActiveCategoriesQuery(), ct);
        RecordCatalogResult("get_categories", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("categories/{id:long}")]
    public async Task<IActionResult> GetCategoryById(long id, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_category_by_id"));
        var result = await _sender.Send(new GetCatalogCategoryByIdQuery(id), ct);
        RecordCatalogResult("get_category_by_id", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/products/{productId:long}/availability")]
    public async Task<IActionResult> GetProductAvailability(Guid companyId, long productId, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_product_availability"));
        var result = await _sender.Send(new GetProductAvailabilityQuery(companyId, productId), ct);
        RecordCatalogResult("get_product_availability", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_products"));
        var result = await _sender.Send(new GetCatalogProductsQuery(), ct);
        RecordCatalogResult("get_products", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct)
    {
        using var timer = MarketplaceMetrics.StartTimer(MarketplaceMetrics.CatalogLatencyMs, new KeyValuePair<string, object?>("operation", "get_product_by_slug"));
        var result = await _sender.Send(new GetCatalogProductBySlugQuery(slug), ct);
        RecordCatalogResult("get_product_by_slug", result.IsSuccess, result.Error);
        return result.ToActionResult();
    }

    [HttpGet("products/search")]
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
            request.Sort,
            request.Page ?? 1,
            request.PageSize ?? 20,
            request.SearchAfter), ct);
        RecordCatalogResult("search_products", result.IsSuccess, result.Error);
        if (result.IsSuccess && string.IsNullOrWhiteSpace(result.Value?.NextSearchAfter) && !string.IsNullOrWhiteSpace(searchName))
            MarketplaceMetrics.CatalogSearchFallbacks.Add(1, [new KeyValuePair<string, object?>("reason", "possible_db_fallback")]);
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
}

public sealed record SearchCatalogProductsRequest(
    string? Name,
    string? Query,
    List<long>? CategoryIds,
    Guid? CompanyId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? AvailabilityStatus,
    string? Sort,
    int? Page,
    int? PageSize,
    string? SearchAfter);
