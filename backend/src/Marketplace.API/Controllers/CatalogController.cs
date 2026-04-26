using Marketplace.API.Extensions;
using Marketplace.Application.Inventory.Queries.GetProductAvailability;
using Marketplace.Application.Products.Queries.GetCatalogProductBySlug;
using Marketplace.Application.Products.Queries.GetCatalogProducts;
using Marketplace.Application.Products.Queries.SearchCatalogProducts;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Categories.Queries.GetCatalogCategoryById;
using Marketplace.Application.Companies.Queries.GetApprovedCompanies;
using Marketplace.Application.Companies.Queries.GetCatalogCompanyByIdOrSlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("catalog")]
[AllowAnonymous]
public sealed class CatalogController : ControllerBase
{
    private readonly ISender _sender;

    public CatalogController(ISender sender) => _sender = sender;

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        var result = await _sender.Send(new GetApprovedCompaniesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{idOrSlug}")]
    public async Task<IActionResult> GetCompanyByIdOrSlug(string idOrSlug, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCatalogCompanyByIdOrSlugQuery(idOrSlug), ct);
        return result.ToActionResult();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _sender.Send(new GetActiveCategoriesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("categories/{id:long}")]
    public async Task<IActionResult> GetCategoryById(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCatalogCategoryByIdQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{companyId:guid}/products/{productId:long}/availability")]
    public async Task<IActionResult> GetProductAvailability(Guid companyId, long productId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProductAvailabilityQuery(companyId, productId), ct);
        return result.ToActionResult();
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        var result = await _sender.Send(new GetCatalogProductsQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("products/{slug}")]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken ct)
    {
        var result = await _sender.Send(new GetCatalogProductBySlugQuery(slug), ct);
        return result.ToActionResult();
    }

    [HttpGet("products/search")]
    public async Task<IActionResult> SearchProducts([FromQuery] SearchCatalogProductsRequest request, CancellationToken ct)
    {
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
        return result.ToActionResult();
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
