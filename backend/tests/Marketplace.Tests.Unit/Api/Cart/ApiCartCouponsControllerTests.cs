using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Coupons.Commands.ApplyCouponToCart;
using Marketplace.Application.Coupons.Commands.RemoveCouponFromCart;
using Marketplace.Application.Coupons.Commands.ValidateCouponForCart;
using Marketplace.Application.Coupons.DTOs;
using Marketplace.Application.Coupons.Options;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "CartCheckout")]
public sealed class ApiCartCouponsControllerTests
{
    [Fact]
    public async Task Validate_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result<CouponValidationResultDto>.Success(new CouponValidationResultDto(true, null, "OK", "SAVE10", 100, 10, 90)) };
        var controller = BuildController(sender);

        var result = await controller.Validate(new CouponCodeRequest("SAVE10"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ValidateCouponForCartCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Apply_Sends_Command()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<CartCouponDto>.Success(new CartCouponDto(1, 2, "SAVE10", DateTime.UtcNow, null))
        };
        var controller = BuildController(sender);

        var result = await controller.Apply(new CouponCodeRequest("SAVE10"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ApplyCouponToCartCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task Remove_Sends_Command()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender);

        var result = await controller.Remove("SAVE10", CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.IsType<RemoveCouponFromCartCommand>(sender.LastRequest);
    }

    private static CartCouponsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test");
        return new CartCouponsController(sender, Options.Create(new CouponsOptions { ReadEnabled = true }))
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
