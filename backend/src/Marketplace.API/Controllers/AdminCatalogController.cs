using Marketplace.API.Extensions;
using Marketplace.Application.Categories.Commands.ActivateCategory;
using Marketplace.Application.Categories.Commands.CreateCategory;
using Marketplace.Application.Categories.Commands.DeactivateCategory;
using Marketplace.Application.Categories.Commands.DeleteCategory;
using Marketplace.Application.Categories.Commands.UpdateCategory;
using Marketplace.Application.Categories.Queries.GetActiveCategories;
using Marketplace.Application.Categories.Queries.GetAdminCategoryById;
using Marketplace.Application.Categories.Queries.GetAllCategories;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.Commands.DeleteCompany;
using Marketplace.Application.Companies.Commands.RevokeCompanyApproval;
using Marketplace.Application.Companies.Commands.SetCompanyCommissionRate;
using Marketplace.Application.Companies.Commands.UpdateCompany;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Queries.GetAdminCompanyById;
using Marketplace.Application.Companies.Queries.GetAllCompanies;
using Marketplace.Application.Companies.Queries.GetPendingCompanies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminCatalogController : ControllerBase
{
    private readonly ISender _sender;

    public AdminCatalogController(ISender sender) => _sender = sender;

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        var result = await _sender.Send(new GetAllCompaniesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/pending")]
    public async Task<IActionResult> GetPendingCompanies(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingCompaniesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("companies/{id:guid}")]
    public async Task<IActionResult> GetCompany(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminCompanyByIdQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var command = new CreateCompanyCommand(
            request.Name,
            request.Slug,
            request.Description,
            request.ImageUrl,
            request.ContactEmail,
            request.ContactPhone,
            request.Address.ToDto(),
            request.LegalProfile.ToDto(),
            request.MetaRaw);

        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPut("companies/{id:guid}")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var command = new UpdateCompanyCommand(
            id,
            request.Name,
            request.Slug,
            request.Description,
            request.ImageUrl,
            request.ContactEmail,
            request.ContactPhone,
            request.Address.ToDto(),
            request.MetaRaw);

        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("companies/{id:guid}/approve")]
    public async Task<IActionResult> ApproveCompany(Guid id, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var adminUserId))
            return Unauthorized();

        var result = await _sender.Send(new ApproveCompanyCommand(id, adminUserId), ct);
        return result.ToActionResult();
    }

    [HttpPost("companies/{id:guid}/revoke-approval")]
    public async Task<IActionResult> RevokeCompanyApproval(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new RevokeCompanyApprovalCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("companies/{id:guid}/commission-rates")]
    public async Task<IActionResult> SetCompanyCommissionRate(Guid id, [FromBody] SetCompanyCommissionRateRequest request, CancellationToken ct)
    {
        if (!User.TryGetUserId(out var adminUserId))
            return Unauthorized();

        var result = await _sender.Send(
            new SetCompanyCommissionRateCommand(id, request.CommissionPercent, request.EffectiveFrom, request.Reason, adminUserId),
            ct);
        return result.ToActionResult();
    }

    [HttpDelete("companies/{id:guid}")]
    public async Task<IActionResult> DeleteCompany(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteCompanyCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _sender.Send(new GetAllCategoriesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("categories/active")]
    public async Task<IActionResult> GetActiveCategories(CancellationToken ct)
    {
        var result = await _sender.Send(new GetActiveCategoriesQuery(), ct);
        return result.ToActionResult();
    }

    [HttpGet("categories/{id:long}")]
    public async Task<IActionResult> GetCategory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminCategoryByIdQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.Slug,
            request.ImageUrl,
            request.ParentCategoryId,
            request.Description,
            request.MetaRaw,
            request.SortOrder,
            request.IsActive);

        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPut("categories/{id:long}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var command = new UpdateCategoryCommand(
            id,
            request.Name,
            request.Slug,
            request.ImageUrl,
            request.ParentCategoryId,
            request.Description,
            request.MetaRaw,
            request.SortOrder);

        var result = await _sender.Send(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("categories/{id:long}/activate")]
    public async Task<IActionResult> ActivateCategory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new ActivateCategoryCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpPost("categories/{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateCategory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeactivateCategoryCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpDelete("categories/{id:long}")]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken ct)
    {
        var result = await _sender.Send(new DeleteCategoryCommand(id), ct);
        return result.ToActionResult();
    }
}

public sealed record CompanyAddressRequest(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country)
{
    public CompanyAddressDto ToDto() => new(
        Street,
        City,
        State,
        PostalCode,
        Country);
}

public sealed record CreateCompanyRequest(
    string Name,
    string Slug,
    string Description,
    string? ImageUrl,
    string ContactEmail,
    string ContactPhone,
    CompanyAddressRequest Address,
    CompanyLegalProfileRequest LegalProfile,
    string? MetaRaw);

public sealed record CompanyLegalProfileRequest(
    string LegalName,
    string LegalType,
    string? Edrpou,
    string? Ipn,
    string? CertificateNumber,
    bool IsVatPayer,
    decimal? InitialCommissionPercent)
{
    public CompanyLegalProfileDto ToDto() => new(
        LegalName,
        LegalType,
        Edrpou,
        Ipn,
        CertificateNumber,
        IsVatPayer,
        InitialCommissionPercent);
}

public sealed record UpdateCompanyRequest(
    string Name,
    string Slug,
    string Description,
    string? ImageUrl,
    string ContactEmail,
    string ContactPhone,
    CompanyAddressRequest Address,
    string? MetaRaw);

public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    string? ImageUrl,
    long? ParentCategoryId,
    string? Description,
    string? MetaRaw,
    int SortOrder,
    bool IsActive);

public sealed record UpdateCategoryRequest(
    string Name,
    string Slug,
    string? ImageUrl,
    long? ParentCategoryId,
    string? Description,
    string? MetaRaw,
    int SortOrder);

public sealed record SetCompanyCommissionRateRequest(
    decimal CommissionPercent,
    DateTime EffectiveFrom,
    string? Reason);
