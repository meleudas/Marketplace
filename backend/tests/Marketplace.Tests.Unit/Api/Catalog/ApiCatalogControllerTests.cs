using Marketplace.API.Controllers;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Application.Companies.Queries.GetCatalogCompanyByIdOrSlug;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Application.Products.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Marketplace.Tests;

public class ApiCatalogControllerTests
{
    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CompaniesWorkspace")]
    public async Task GetCompanies_Returns_Ok_When_Query_Succeeds()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<CompanyDto>>.Success(
                new List<CompanyDto>
                {
                    new(
                        Guid.NewGuid(), "Company", "company", "Description", null, "mail@company.com", "+380",
                        new CompanyAddressDto("Street", "City", "State", "00000", "UA"),
                        true, DateTime.UtcNow, "admin", null, 0, 0, null, DateTime.UtcNow, DateTime.UtcNow, false, null)
                })
        };

        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);
        var response = await controller.GetCompanies(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CompaniesWorkspace")]
    public async Task GetCompanyByIdOrSlug_Sends_Query()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<CompanyDto>.Success(
                new CompanyDto(
                    Guid.NewGuid(), "Company", "company", "Description", null, "mail@company.com", "+380",
                    new CompanyAddressDto("Street", "City", "State", "00000", "UA"),
                    true, DateTime.UtcNow, "admin", null, 0, 0, null, DateTime.UtcNow, DateTime.UtcNow, false, null))
        };
        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);

        var response = await controller.GetCompanyByIdOrSlug("company", CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<GetCatalogCompanyByIdOrSlugQuery>(sender.LastRequest);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CatalogCategories")]
    public async Task GetCategories_Returns_Ok_When_Query_Succeeds()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<CategoryDto>>.Success(
                new List<CategoryDto>
                {
                    new(1, "Category", "category", null, null, null, null, 0, true, 0, DateTime.UtcNow, DateTime.UtcNow, false, null)
                })
        };

        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);
        var response = await controller.GetCategories(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CatalogCategories")]
    public async Task GetCategoryById_Returns_NotFound_When_Query_Fails()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<CategoryDto>.Failure("Category not found")
        };

        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);
        var response = await controller.GetCategoryById(777, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CatalogCategories")]
    public async Task GetProducts_Returns_Ok_When_Query_Succeeds()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<IReadOnlyList<ProductListItemDto>>.Success(
                [new ProductListItemDto(1, Guid.NewGuid(), "Product", "product", "Desc", 100, null, null, 1, "active", false, 10, 1, 10, "in_stock", DateTime.UtcNow, DateTime.UtcNow, [])])
        };

        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);
        var response = await controller.GetProducts(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CatalogCategories")]
    public async Task SearchProducts_Maps_Request_And_Returns_Ok()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ProductSearchResultDto>.Success(new ProductSearchResultDto([], 0, 1, 20))
        };

        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);
        var response = await controller.SearchProducts(
            new SearchCatalogProductsRequest("Keyboard", null, [1, 2], null, 10, 1000, "in_stock", null, null, null, null, "price_asc", 2, 25, "cursor"),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(sender.LastRequest);
    }

    [Fact]
    [Trait("Suite", "API")]
    [Trait("Suite", "CatalogCategories")]
    public async Task GetProductAvailability_Returns_Ok_When_Query_Succeeds()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ProductAvailabilityDto>.Success(new ProductAvailabilityDto(10, 3, "low_stock"))
        };
        var controller = new CatalogController(sender, NullLogger<CatalogController>.Instance);

        var response = await controller.GetProductAvailability(Guid.NewGuid(), 10, CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    private sealed class RecordingSender : ISender
    {
        public object? NextResult { get; set; }
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            if (NextResult is TResponse typed)
                return Task.FromResult(typed);

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(NextResult);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => EmptyStream<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => EmptyStream<object?>();

        private static async IAsyncEnumerable<T> EmptyStream<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
