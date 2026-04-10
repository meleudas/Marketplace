using Marketplace.API.Extensions;
using Marketplace.Application.Inventory.Queries.GetProductAvailability;
using Marketplace.Application.Products.Queries.GetCatalogProductBySlug;
using Marketplace.Application.Products.Queries.GetCatalogProducts;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Companies.Queries.GetApprovedCompanies;
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

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _sender.Send(new GetActiveCategoriesQuery(), ct);
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
}
