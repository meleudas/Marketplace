using Marketplace.API.Extensions;
using Marketplace.Application.Products.Commands.CreateProduct;
using Marketplace.Application.Products.Commands.DeleteProduct;
using Marketplace.Application.Products.Commands.UpdateProduct;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Queries.GetCompanyProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("companies/{companyId:guid}/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetCompanyProducts(Guid companyId, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new GetCompanyProductsQuery(companyId, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] UpsertProductRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new CreateProductCommand(
            companyId, actorId, User.IsInRole("Admin"), request.Name, request.Slug, request.Description,
            request.Price, request.OldPrice, request.MinStock, request.CategoryId, request.HasVariants,
            request.Detail, request.Images), ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(Guid companyId, long id, [FromBody] UpsertProductRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new UpdateProductCommand(
            companyId, id, actorId, User.IsInRole("Admin"), request.Name, request.Slug, request.Description,
            request.Price, request.OldPrice, request.MinStock, request.CategoryId, request.HasVariants,
            request.Detail, request.Images), ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(Guid companyId, long id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();
        var result = await _sender.Send(new DeleteProductCommand(companyId, id, actorId, User.IsInRole("Admin")), ct);
        return result.ToActionResult();
    }
}

public sealed record UpsertProductRequest(
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? OldPrice,
    int MinStock,
    long CategoryId,
    bool HasVariants,
    ProductDetailDto? Detail,
    IReadOnlyList<ProductImageDto>? Images);
