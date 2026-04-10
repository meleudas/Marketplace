using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Companies.Commands.AssignCompanyMemberRole;
using Marketplace.Application.Companies.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

public class ApiCompanyMembersControllerTests
{
    [Fact]
    public async Task AssignRole_Sends_AssignCompanyMemberRoleCommand()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<CompanyMemberDto>.Success(
                new CompanyMemberDto(Guid.NewGuid(), Guid.NewGuid(), "Seller", false, DateTime.UtcNow, DateTime.UtcNow))
        };
        var controller = BuildControllerWithUser(sender, isAdmin: true);

        var response = await controller.AssignRole(Guid.NewGuid(), Guid.NewGuid(), new CompanyMemberRoleRequest("Seller"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        Assert.IsType<AssignCompanyMemberRoleCommand>(sender.LastRequest);
    }

    private static CompanyMembersController BuildControllerWithUser(ISender sender, bool isAdmin)
    {
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()) };
        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, "test");
        return new CompanyMembersController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
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
            return Task.FromResult((TResponse)NextResult!);
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
            => Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
