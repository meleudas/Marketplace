using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Common.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Platform")]
public sealed class ApiAdminOutboxControllerTests
{
    [Fact]
    public async Task Requeue_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminOutboxController(new SpyOutboxWriter())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Requeue(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Requeue_Returns_Forbid_When_User_Is_Not_Admin()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Seller")
        ], "test");
        var controller = new AdminOutboxController(new SpyOutboxWriter())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };

        var result = await controller.Requeue(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Requeue_Calls_Outbox_For_Admin()
    {
        var outbox = new SpyOutboxWriter();
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");
        var controller = new AdminOutboxController(outbox)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        var messageId = Guid.NewGuid();

        var result = await controller.Requeue(messageId, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(messageId, outbox.LastRequeuedId);
    }

    private sealed class SpyOutboxWriter : IOutboxWriter
    {
        public Guid? LastRequeuedId { get; private set; }

        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default)
        {
            LastRequeuedId = id;
            return Task.CompletedTask;
        }
    }
}
