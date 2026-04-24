using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Categories.Commands.ActivateCategory;
using Marketplace.Application.Categories.Commands.CreateCategory;
using Marketplace.Application.Categories.DTOs;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

public class ApiAdminCatalogControllerTests
{
    [Fact]
    public async Task ApproveCompany_Returns_Unauthorized_When_UserId_Claim_Missing()
    {
        var sender = new RecordingSender();
        var controller = new AdminCatalogController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var response = await controller.ApproveCompany(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response);
        Assert.Null(sender.LastRequest);
    }

    [Fact]
    public async Task CreateCompany_Sends_CreateCompanyCommand()
    {
        var sender = new RecordingSender();
        sender.NextResult = Result<CompanyDto>.Success(
            new CompanyDto(
                Guid.NewGuid(), "Company", "company", "Description", null, "mail@company.com", "+380",
                new CompanyAddressDto("Street", "City", "State", "00000", "UA"),
                false, null, null, null, 0, 0, null, DateTime.UtcNow, DateTime.UtcNow, false, null));
        var controller = BuildControllerWithUser(sender);

        var response = await controller.CreateCompany(
            new CreateCompanyRequest(
                "Company", "company", "Description", null, "mail@company.com", "+380",
                new CompanyAddressRequest("Street", "City", "State", "00000", "UA"),
                new CompanyLegalProfileRequest("Company LLC", "llc", "12345678", null, null, true, 12m),
                null),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<CreateCompanyCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task CreateCategory_Sends_CreateCategoryCommand()
    {
        var sender = new RecordingSender();
        sender.NextResult = Result<CategoryDto>.Success(
            new CategoryDto(1, "Cat", "cat", null, null, null, null, 0, true, 0, DateTime.UtcNow, DateTime.UtcNow, false, null));
        var controller = BuildControllerWithUser(sender);

        var response = await controller.CreateCategory(
            new CreateCategoryRequest("Cat", "cat", null, null, null, null, 0, true),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<CreateCategoryCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ActivateCategory_Sends_ActivateCategoryCommand()
    {
        var sender = new RecordingSender();
        sender.NextResult = Result.Success();
        var controller = BuildControllerWithUser(sender);

        var response = await controller.ActivateCategory(10, CancellationToken.None);

        Assert.IsType<OkResult>(response);
        Assert.IsType<ActivateCategoryCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ApproveCompany_Sends_Command_When_UserId_Present()
    {
        var sender = new RecordingSender();
        sender.NextResult = Result.Success();
        var controller = BuildControllerWithUser(sender);

        var response = await controller.ApproveCompany(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<OkResult>(response);
        Assert.IsType<ApproveCompanyCommand>(sender.LastRequest);
    }

    private static AdminCatalogController BuildControllerWithUser(ISender sender)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString())
        }, "test");

        return new AdminCatalogController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }
        public object? NextResult { get; set; }

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
