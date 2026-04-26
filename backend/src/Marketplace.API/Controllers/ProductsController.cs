using Marketplace.API.Extensions;
using Marketplace.Application.Products.Commands.CreateProduct;
using Marketplace.Application.Products.Commands.DeleteProduct;
using Marketplace.Application.Products.Commands.UploadProductImage;
using Marketplace.Application.Products.Commands.UpdateProduct;
using Marketplace.Application.Products.DTOs;
using Marketplace.Application.Products.Queries.GetCompanyProducts;
using Marketplace.Infrastructure.External.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("companies/{companyId:guid}/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly StorageOptions _storageOptions;

    public ProductsController(ISender sender, IOptions<StorageOptions> storageOptions)
    {
        _sender = sender;
        _storageOptions = storageOptions.Value;
    }

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

    [HttpPost("{id:long}/images:upload")]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> UploadImage(Guid companyId, long id, IFormFile file, [FromForm] string? altText, [FromForm] int? sortOrder, [FromForm] bool? isMain, CancellationToken ct)
    {
        if (!_storageOptions.Enabled)
            return BadRequest(new { message = "Storage is disabled" });

        if (file is null || file.Length <= 0)
            return BadRequest(new { message = "File is required" });

        if (file.Length > _storageOptions.MaxUploadBytes)
            return BadRequest(new { message = $"File is too large. Max {_storageOptions.MaxUploadBytes} bytes." });

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Unsupported file type. Allowed: image/jpeg, image/png, image/webp." });

        if (!User.TryGetUserId(out var actorId))
            return Unauthorized();

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var result = await _sender.Send(new UploadProductImageCommand(
            companyId,
            id,
            actorId,
            User.IsInRole("Admin"),
            ms.ToArray(),
            file.FileName,
            file.ContentType,
            altText?.Trim() ?? string.Empty,
            sortOrder ?? 0,
            isMain ?? false), ct);

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
