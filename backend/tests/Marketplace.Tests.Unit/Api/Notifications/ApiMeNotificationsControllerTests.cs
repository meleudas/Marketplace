using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Notifications.Commands.MarkNotificationRead;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Notifications.Queries.GetMyNotifications;
using Marketplace.Domain.Notifications.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Notifications")]
public sealed class ApiMeNotificationsControllerTests
{
    [Fact]
    public async Task List_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new MeNotificationsController(new RecordingSender())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.List(ct: CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task List_Sends_Query_And_Returns_Payload()
    {
        var userId = Guid.NewGuid();
        var sender = new RecordingSender
        {
            NextResult = Result<PagedInAppNotificationsDto>.Success(
                new PagedInAppNotificationsDto(
                    [
                        new InAppNotificationListItemDto(
                            7,
                            "UserOrderStatus",
                            Guid.NewGuid(),
                            NotificationKind.Order,
                            "Статус оновлено",
                            "Ваше замовлення відправлене",
                            "/orders/7",
                            false,
                            null,
                            DateTime.UnixEpoch,
                            "{\"foo\":\"bar\"}")
                    ],
                    1,
                    1,
                    20))
        };
        var controller = BuildController(sender, userId);

        var result = await controller.List(1, 20, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var query = Assert.IsType<GetMyNotificationsQuery>(sender.LastRequest);
        Assert.Equal(userId, query.ActorUserId);
        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
    }

    [Fact]
    public async Task MarkRead_Returns_NotFound_When_Handler_Returns_NotFound()
    {
        var sender = new RecordingSender { NextResult = Result.Failure("Notification not found.") };
        var controller = BuildController(sender, Guid.NewGuid());

        var result = await controller.MarkRead(123, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }

    [Fact]
    public async Task MarkRead_Sends_Command_For_Authenticated_User()
    {
        var userId = Guid.NewGuid();
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(sender, userId);

        var result = await controller.MarkRead(77, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        var command = Assert.IsType<MarkNotificationReadCommand>(sender.LastRequest);
        Assert.Equal(userId, command.ActorUserId);
        Assert.Equal(77, command.NotificationId);
    }

    private static MeNotificationsController BuildController(ISender sender, Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Role, "Buyer")
        ], "test");
        return new MeNotificationsController(sender)
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
        public object? NextResult { get; set; } = Result.Success();

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
