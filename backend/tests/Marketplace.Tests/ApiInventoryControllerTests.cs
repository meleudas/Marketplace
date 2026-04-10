using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Inventory.Commands.CreateWarehouse;
using Marketplace.Application.Inventory.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

public class ApiInventoryControllerTests
{
    [Fact]
    public async Task CreateWarehouse_Sends_CreateWarehouseCommand()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<WarehouseDto>.Success(new WarehouseDto(
                1, Guid.NewGuid(), "Main", "MAIN", "St", "City", "State", "00000", "UA", "UTC", 0, true, DateTime.UtcNow, DateTime.UtcNow))
        };
        var controller = BuildController(sender);

        var result = await controller.CreateWarehouse(Guid.NewGuid(),
            new CreateWarehouseRequest("Main", "MAIN", "St", "City", "State", "00000", "UA", "UTC", 0),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<CreateWarehouseCommand>(sender.LastRequest);
    }

    private static InventoryController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test");
        return new InventoryController(sender)
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

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(NextResult);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
