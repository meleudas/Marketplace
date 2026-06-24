using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Notifications")]
public sealed class SmsNotificationChannelTests
{
    [Fact]
    public async Task DeliverAsync_Skips_When_Sms_Disabled()
    {
        var sms = new RecordingSmsPort();
        var sut = new SmsNotificationChannel(
            sms,
            new FixedContactReader(new AppNotificationUserContact(null, false, null, false, false, "+380501234567", true)),
            new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { SmsEnabled = false }),
            NullLogger<SmsNotificationChannel>.Instance);

        await sut.DeliverAsync(new AppNotificationEnvelope
        {
            TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
            TemplateVersion = 2,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Sms,
            Audience = AppNotificationAudienceKind.User,
            TargetUserId = Guid.NewGuid(),
            Title = "Замовлення",
            Body = "Відправлено"
        });

        Assert.Null(sms.LastPhone);
    }

    [Fact]
    public async Task DeliverAsync_Skips_When_No_Confirmed_Phone()
    {
        var sms = new RecordingSmsPort();
        var sut = new SmsNotificationChannel(
            sms,
            new FixedContactReader(new AppNotificationUserContact(null, false, null, false, false, "+380501234567", false)),
            new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { SmsEnabled = true }),
            NullLogger<SmsNotificationChannel>.Instance);

        await sut.DeliverAsync(new AppNotificationEnvelope
        {
            TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
            TemplateVersion = 2,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Sms,
            Audience = AppNotificationAudienceKind.User,
            TargetUserId = Guid.NewGuid(),
            Title = "Замовлення",
            Body = "Відправлено"
        });

        Assert.Null(sms.LastPhone);
    }

    [Fact]
    public async Task DeliverAsync_Sends_When_Enabled_And_Phone_Confirmed()
    {
        var sms = new RecordingSmsPort();
        var sut = new SmsNotificationChannel(
            sms,
            new FixedContactReader(new AppNotificationUserContact(null, false, null, false, false, "+380501234567", true)),
            new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { SmsEnabled = true }),
            NullLogger<SmsNotificationChannel>.Instance);

        await sut.DeliverAsync(new AppNotificationEnvelope
        {
            TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
            TemplateVersion = 2,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Sms,
            Audience = AppNotificationAudienceKind.User,
            TargetUserId = Guid.NewGuid(),
            Title = "Замовлення",
            Body = "Відправлено"
        });

        Assert.Equal("+380501234567", sms.LastPhone);
    }

    private sealed class RecordingSmsPort : ISmsPort
    {
        public string? LastPhone { get; private set; }

        public Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            LastPhone = phoneNumber;
            return Task.CompletedTask;
        }

        public Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private sealed class FixedContactReader(AppNotificationUserContact contact) : IAppNotificationUserContactReader
    {
        public Task<AppNotificationUserContact?> GetAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<AppNotificationUserContact?>(contact);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
