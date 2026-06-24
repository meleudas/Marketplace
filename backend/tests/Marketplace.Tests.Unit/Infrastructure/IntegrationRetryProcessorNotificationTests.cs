using System.Text.Json;
using Marketplace.Application.Notifications;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Jobs;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Notifications")]
public sealed class IntegrationRetryProcessorNotificationTests
{
    [Fact]
    public async Task NotificationDispatch_Redispatches_Full_Payload()
    {
        var redispatcher = new RecordingRedispatcher();
        var processor = CreateProcessor(redispatcher, maxAttempts: 10);
        var correlationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            templateKey = AppNotificationTemplateKeys.UserOrderStatus,
            correlationId,
            channels = 7,
            audience = 1,
            targetUserId = userId,
            targetCompanyId = (Guid?)null,
            payloadJson = "{\"orderId\":1}",
            channel = "Email"
        });
        var entry = new IntegrationRetryEntry(
            Guid.NewGuid(),
            IntegrationRetryKinds.NotificationDispatch,
            "AppNotification",
            $"{correlationId:N}:Email",
            payload,
            1,
            "smtp down",
            DateTime.UtcNow,
            null,
            null,
            DateTime.UtcNow);

        var resolved = await processor.TryProcessAsync(entry, CancellationToken.None);

        Assert.True(resolved);
        Assert.NotNull(redispatcher.Last);
        Assert.Equal(AppNotificationTemplateKeys.UserOrderStatus, redispatcher.Last!.Value.TemplateKey);
        Assert.Equal(correlationId, redispatcher.Last.Value.CorrelationId);
        Assert.Equal(7, redispatcher.Last.Value.Channels);
        Assert.Equal(userId, redispatcher.Last.Value.TargetUserId);
    }

    [Fact]
    public async Task NotificationDispatch_Throws_When_Attempts_Exhausted()
    {
        var processor = CreateProcessor(new RecordingRedispatcher(), maxAttempts: 3);
        var payload = JsonSerializer.Serialize(new
        {
            templateKey = AppNotificationTemplateKeys.UserOrderStatus,
            correlationId = Guid.NewGuid(),
            channels = 1,
            audience = 1
        });
        var entry = new IntegrationRetryEntry(
            Guid.NewGuid(),
            IntegrationRetryKinds.NotificationDispatch,
            "AppNotification",
            "corr:Email",
            payload,
            3,
            "failed",
            DateTime.UtcNow,
            null,
            null,
            DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.TryProcessAsync(entry, CancellationToken.None));
    }

    private static IntegrationRetryProcessor CreateProcessor(RecordingRedispatcher redispatcher, int maxAttempts) =>
        new(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            redispatcher,
            Options.Create(new IntegrationRetryOptions { MaxAttempts = maxAttempts }));

    private sealed class RecordingRedispatcher : IAppNotificationRedispatcher
    {
        public (
            string TemplateKey,
            Guid CorrelationId,
            int Channels,
            int Audience,
            Guid? TargetUserId,
            Guid? TargetCompanyId,
            string? PayloadJson)? Last { get; private set; }

        public void EnqueueDispatch(
            string templateKey,
            Guid correlationId,
            int channels,
            int audience,
            Guid? targetUserId,
            Guid? targetCompanyId,
            string? payloadJson) =>
            Last = (templateKey, correlationId, channels, audience, targetUserId, targetCompanyId, payloadJson);
    }
}
