using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Notifications")]
public sealed class ApiPushNotificationsControllerTests
{
    [Fact]
    public void GetVapidPublicKey_Returns_Configured_Values()
    {
        var controller = new PushNotificationsController(
            new CapturingPushSubscriptionRepository(),
            new StaticOptionsMonitor<WebPushOptions>(new WebPushOptions
            {
                PublicKey = "pub-key",
                Subject = "mailto:test@example.com"
            }));

        var result = controller.GetVapidPublicKey();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PushNotificationsController.VapidPublicKeyResponse>(ok.Value);
        Assert.Equal("pub-key", payload.PublicKey);
        Assert.Equal("mailto:test@example.com", payload.Subject);
    }

    [Fact]
    public async Task RegisterSubscription_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new PushNotificationsController(
            new CapturingPushSubscriptionRepository(),
            new StaticOptionsMonitor<WebPushOptions>(new WebPushOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.RegisterSubscription(
            new PushNotificationsController.RegisterPushSubscriptionRequest
            {
                Endpoint = "https://ep",
                P256dh = "p",
                Auth = "a"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task RegisterSubscription_Returns_BadRequest_When_Required_Fields_Missing()
    {
        var controller = BuildController(
            new CapturingPushSubscriptionRepository(),
            Guid.NewGuid(),
            "Buyer");

        var result = await controller.RegisterSubscription(
            new PushNotificationsController.RegisterPushSubscriptionRequest
            {
                Endpoint = " ",
                P256dh = " ",
                Auth = " "
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RegisterSubscription_Rejects_Admin_Channel_For_NonAdmin()
    {
        var repo = new CapturingPushSubscriptionRepository();
        var controller = BuildController(repo, Guid.NewGuid(), "Buyer");

        var result = await controller.RegisterSubscription(
            new PushNotificationsController.RegisterPushSubscriptionRequest
            {
                Endpoint = "https://ep",
                P256dh = "p",
                Auth = "a",
                IncludeUserChannel = false,
                IncludeAdminChannel = true
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(repo.LastUpsert);
    }

    [Fact]
    public async Task RegisterSubscription_Stores_Trimmed_Values_And_Audience_Flags()
    {
        var userId = Guid.NewGuid();
        var repo = new CapturingPushSubscriptionRepository();
        var controller = BuildController(repo, userId, "Admin");
        controller.ControllerContext.HttpContext.Request.Headers.UserAgent = "test-agent";

        var result = await controller.RegisterSubscription(
            new PushNotificationsController.RegisterPushSubscriptionRequest
            {
                Endpoint = "  https://ep  ",
                P256dh = "  p256  ",
                Auth = "  auth  ",
                IncludeUserChannel = true,
                IncludeAdminChannel = true
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(repo.LastUpsert);
        Assert.Equal(userId, repo.LastUpsert!.Value.UserId);
        Assert.Equal("https://ep", repo.LastUpsert.Value.Endpoint);
        Assert.Equal("p256", repo.LastUpsert.Value.P256dh);
        Assert.Equal("auth", repo.LastUpsert.Value.Auth);
        Assert.Equal(PushSubscriptionAudienceFlags.UserWebPush | PushSubscriptionAudienceFlags.AdminWebPush, repo.LastUpsert.Value.Flags);
    }

    [Fact]
    public async Task DeleteSubscription_Returns_BadRequest_When_Endpoint_Missing()
    {
        var controller = BuildController(new CapturingPushSubscriptionRepository(), Guid.NewGuid(), "Buyer");

        var result = await controller.DeleteSubscription(" ", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSubscription_Deletes_By_User_And_Endpoint()
    {
        var userId = Guid.NewGuid();
        var repo = new CapturingPushSubscriptionRepository();
        var controller = BuildController(repo, userId, "Buyer");

        var result = await controller.DeleteSubscription("https://ep", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((userId, "https://ep"), repo.LastDelete);
    }

    private static PushNotificationsController BuildController(IPushSubscriptionRepository repo, Guid userId, string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "test");

        return new PushNotificationsController(repo, new StaticOptionsMonitor<WebPushOptions>(new WebPushOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class CapturingPushSubscriptionRepository : IPushSubscriptionRepository
    {
        public (Guid UserId, string Endpoint, string P256dh, string Auth, PushSubscriptionAudienceFlags Flags, string? UserAgent)? LastUpsert { get; private set; }
        public (Guid UserId, string Endpoint)? LastDelete { get; private set; }

        public Task UpsertAsync(Guid userId, string endpoint, string p256dh, string auth, PushSubscriptionAudienceFlags audienceFlags, string? userAgent, CancellationToken ct = default)
        {
            LastUpsert = (userId, endpoint, p256dh, auth, audienceFlags, userAgent);
            return Task.CompletedTask;
        }

        public Task DeleteByUserAndEndpointAsync(Guid userId, string endpoint, CancellationToken ct = default)
        {
            LastDelete = (userId, endpoint);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PushSubscriptionDto>> ListByUserAndAudienceAsync(Guid userId, PushSubscriptionAudienceFlags requiredFlags, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PushSubscriptionDto>>([]);

        public Task<IReadOnlyList<PushSubscriptionDto>> ListByAudienceFlagAsync(PushSubscriptionAudienceFlags requiredFlag, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PushSubscriptionDto>>([]);

        public Task DeleteByIdAsync(long id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        private readonly T _value;
        public StaticOptionsMonitor(T value) => _value = value;
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable OnChange(Action<T, string?> listener) => EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();
        public void Dispose() { }
    }
}
