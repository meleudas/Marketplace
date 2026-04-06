using Marketplace.API.Extensions;
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
}
