using Marketplace.API.Controllers;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

public class ApiCatalogControllerTests
{
    [Fact]
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

        var controller = new CatalogController(sender);
        var response = await controller.GetCompanies(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
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

        var controller = new CatalogController(sender);
        var response = await controller.GetCategories(CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
    }

    private sealed class RecordingSender : ISender
    {
        public object? NextResult { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (NextResult is TResponse typed)
                return Task.FromResult(typed);

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult(NextResult);

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
