using System.Security.Claims;
using System.Text.Json;
using Marketplace.API.Controllers;
using Marketplace.Application.Auth.Ports;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Notifications.Enums;
using Marketplace.Infrastructure;
using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Notifications")]
public sealed class ApplicationPushNotificationsTests
{
    [Fact]
    public void AppNotificationPayloadBuilder_AdminNewOrder_Builds_ActionUrl()
    {
        var frontend = new OptionsWrapper<FrontendOptions>(new FrontendOptions { BaseUrl = "https://shop.example" });
        var monitor = new StaticOptionsMonitor<FrontendOptions>(frontend.Value);
        var sut = new AppNotificationPayloadBuilder(monitor);
        var request = new AppNotificationRequest
        {
            TemplateKey = AppNotificationTemplateKeys.AdminNewOrder,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Push,
            Audience = AppNotificationAudienceKind.Admins,
            PayloadJson = JsonSerializer.Serialize(new { orderId = 99L, orderNumber = "A-1", companyId = Guid.Empty })
        };

        var envelope = sut.Build(request);

        Assert.Equal("Нове замовлення", envelope.Title);
        Assert.Contains("A-1", envelope.Body, StringComparison.Ordinal);
        Assert.Equal("https://shop.example/admin/orders/99", envelope.ActionUrl);
    }

    [Fact]
    public void AppNotificationPayloadBuilder_UserOrderStatus_Builds_ActionUrl()
    {
        var monitor = new StaticOptionsMonitor<FrontendOptions>(new FrontendOptions { BaseUrl = "https://shop.example/" });
        var sut = new AppNotificationPayloadBuilder(monitor);
        var request = new AppNotificationRequest
        {
            TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Push,
            Audience = AppNotificationAudienceKind.User,
            TargetUserId = Guid.NewGuid(),
            PayloadJson = JsonSerializer.Serialize(new { orderId = 5L, orderNumber = "B-2", status = "Shipped" })
        };

        var envelope = sut.Build(request);

        Assert.Contains("Shipped", envelope.Body, StringComparison.Ordinal);
        Assert.Equal("https://shop.example/orders/5", envelope.ActionUrl);
        Assert.Equal(AppNotificationTemplateVersions.GetVersion(AppNotificationTemplateKeys.UserOrderStatus), envelope.TemplateVersion);
    }

    [Fact]
    public void AppNotificationPayloadBuilder_CompanyNewOrder_Builds_Company_Orders_ActionUrl()
    {
        var companyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var monitor = new StaticOptionsMonitor<FrontendOptions>(new FrontendOptions { BaseUrl = "https://shop.example" });
        var sut = new AppNotificationPayloadBuilder(monitor);
        var request = new AppNotificationRequest
        {
            TemplateKey = AppNotificationTemplateKeys.CompanyNewOrder,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.InApp,
            Audience = AppNotificationAudienceKind.CompanyStakeholders,
            TargetCompanyId = companyId,
            PayloadJson = JsonSerializer.Serialize(new { orderId = 12L, orderNumber = "C-9", companyId })
        };

        var envelope = sut.Build(request);

        Assert.Contains("C-9", envelope.Body, StringComparison.Ordinal);
        Assert.Equal($"https://shop.example/companies/{companyId:D}/orders/12", envelope.ActionUrl);
    }

    [Fact]
    public void AppNotificationPayloadBuilder_CartProductBackInStock_Uses_Product_Slug_In_ActionUrl()
    {
        var monitor = new StaticOptionsMonitor<FrontendOptions>(new FrontendOptions { BaseUrl = "https://shop.example" });
        var sut = new AppNotificationPayloadBuilder(monitor);
        var request = new AppNotificationRequest
        {
            TemplateKey = AppNotificationTemplateKeys.CartProductBackInStock,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Push,
            Audience = AppNotificationAudienceKind.User,
            TargetUserId = Guid.NewGuid(),
            PayloadJson = JsonSerializer.Serialize(new
            {
                productId = 42L,
                productName = "Кава",
                slug = "kava-1",
                companyId = Guid.Empty
            })
        };

        var envelope = sut.Build(request);

        Assert.Contains("Кава", envelope.Body, StringComparison.Ordinal);
        Assert.Equal("https://shop.example/products/kava-1", envelope.ActionUrl);
    }

    [Fact]
    public void AppNotificationPayloadBuilder_UserPaymentStatus_Includes_Order_Status_In_Body()
    {
        var monitor = new StaticOptionsMonitor<FrontendOptions>(new FrontendOptions { BaseUrl = "https://shop.example" });
        var sut = new AppNotificationPayloadBuilder(monitor);
        var request = new AppNotificationRequest
        {
            TemplateKey = AppNotificationTemplateKeys.UserPaymentStatus,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Push,
            Audience = AppNotificationAudienceKind.User,
            TargetUserId = Guid.NewGuid(),
            PayloadJson = JsonSerializer.Serialize(new
            {
                orderId = 3L,
                orderNumber = "P-1",
                paymentStatus = "Completed",
                orderStatus = "Paid"
            })
        };

        var envelope = sut.Build(request);

        Assert.Contains("Completed", envelope.Body, StringComparison.Ordinal);
        Assert.Contains("Paid", envelope.Body, StringComparison.Ordinal);
        Assert.Equal("https://shop.example/orders/3", envelope.ActionUrl);
    }

    [Fact]
    public async Task WebPushNotificationChannel_Sends_When_Enabled()
    {
        var subs = new FakePushSubscriptionRepository();
        var adminUserId = Guid.NewGuid();
        subs.Subscriptions.Add(new PushSubscriptionDto(1, adminUserId, "https://push.example/ep", "p256", "auth",
            PushSubscriptionAudienceFlags.AdminWebPush));
        var delivery = new RecordingPushDeliveryClient();
        var webPushOpts = new StaticOptionsMonitor<WebPushOptions>(new WebPushOptions
        {
            Enabled = true,
            PublicKey = "x",
            PrivateKey = "y",
            Subject = "mailto:t@t"
        });
        var channel = new WebPushNotificationChannel(subs, delivery, webPushOpts, new FixedAdminRecipientIds([adminUserId]),
            new EmptyCompanyRecipientIds(),
            NullLogger<WebPushNotificationChannel>.Instance);

        var envelope = new AppNotificationEnvelope
        {
            TemplateKey = AppNotificationTemplateKeys.AdminNewOrder,
            CorrelationId = Guid.NewGuid(),
            Channels = AppNotificationChannelKind.Push,
            Audience = AppNotificationAudienceKind.Admins,
            Title = "T",
            Body = "B",
            ActionUrl = "https://x",
            PayloadJson = "{}"
        };

        await channel.DeliverAsync(envelope, CancellationToken.None);

        Assert.Single(delivery.Requests);
        Assert.Equal("https://push.example/ep", delivery.Requests[0].Endpoint);
    }

    [Fact]
    public async Task WebPushNotificationChannel_NoSend_When_Disabled()
    {
        var subs = new FakePushSubscriptionRepository();
        var adminUserId = Guid.NewGuid();
        subs.Subscriptions.Add(new PushSubscriptionDto(1, adminUserId, "https://push.example/ep", "p256", "auth",
            PushSubscriptionAudienceFlags.AdminWebPush));
        var delivery = new RecordingPushDeliveryClient();
        var webPushOpts = new StaticOptionsMonitor<WebPushOptions>(new WebPushOptions { Enabled = false });
        var channel = new WebPushNotificationChannel(subs, delivery, webPushOpts, new FixedAdminRecipientIds([adminUserId]),
            new EmptyCompanyRecipientIds(),
            NullLogger<WebPushNotificationChannel>.Instance);

        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.AdminNewOrder,
                CorrelationId = Guid.NewGuid(),
                Channels = AppNotificationChannelKind.Push,
                Audience = AppNotificationAudienceKind.Admins,
                Title = "T",
                Body = "B",
                PayloadJson = "{}"
            },
            CancellationToken.None);

        Assert.Empty(delivery.Requests);
    }

    [Fact]
    public async Task InAppNotificationChannel_Persists_For_User_Audience()
    {
        var repo = new CapturingInAppNotificationRepository();
        var admins = new FixedAdminRecipientIds([]);
        var inAppOpts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { InAppDefaultTtlDays = 0 });
        var channel = new InAppNotificationChannel(repo, admins, new EmptyCompanyRecipientIds(), inAppOpts, NullLogger<InAppNotificationChannel>.Instance);
        var corr = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                CorrelationId = corr,
                Channels = AppNotificationChannelKind.InApp,
                Audience = AppNotificationAudienceKind.User,
                TargetUserId = userId,
                Title = "t",
                Body = "b",
                PayloadJson = """{"orderId":1}"""
            },
            CancellationToken.None);

        Assert.Single(repo.Inserts);
        Assert.Equal(userId, repo.Inserts[0].UserId);
        Assert.Equal(corr, repo.Inserts[0].CorrelationId);
        Assert.Equal(NotificationKind.Order, repo.Inserts[0].Kind);
        Assert.Contains("jobCorrelationId", repo.Inserts[0].DataJson, StringComparison.Ordinal);
        Assert.Contains(corr.ToString("N"), repo.Inserts[0].DataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InAppNotificationChannel_FanOut_To_Admins()
    {
        var repo = new CapturingInAppNotificationRepository();
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var admins = new FixedAdminRecipientIds([a1, a2]);
        var inAppOpts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { InAppDefaultTtlDays = 0 });
        var channel = new InAppNotificationChannel(repo, admins, new EmptyCompanyRecipientIds(), inAppOpts, NullLogger<InAppNotificationChannel>.Instance);
        var jobCorr = Guid.NewGuid();
        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.AdminNewOrder,
                CorrelationId = jobCorr,
                Channels = AppNotificationChannelKind.InApp,
                Audience = AppNotificationAudienceKind.Admins,
                Title = "t",
                Body = "b",
                PayloadJson = "{}"
            },
            CancellationToken.None);

        Assert.Equal(2, repo.Inserts.Count);
        Assert.Contains(repo.Inserts, x => x.UserId == a1);
        Assert.Contains(repo.Inserts, x => x.UserId == a2);
        Assert.NotEqual(repo.Inserts[0].CorrelationId, repo.Inserts[1].CorrelationId);
    }

    [Fact]
    public async Task InAppNotificationChannel_FanOut_To_CompanyStakeholders()
    {
        var companyId = Guid.NewGuid();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var repo = new CapturingInAppNotificationRepository();
        var admins = new FixedAdminRecipientIds([]);
        var companyRecipients = new FixedCompanyRecipientIds(companyId, [u1, u2]);
        var inAppOpts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { InAppDefaultTtlDays = 0 });
        var channel = new InAppNotificationChannel(repo, admins, companyRecipients, inAppOpts, NullLogger<InAppNotificationChannel>.Instance);
        var jobCorr = Guid.NewGuid();
        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.CompanyNewOrder,
                CorrelationId = jobCorr,
                Channels = AppNotificationChannelKind.InApp,
                Audience = AppNotificationAudienceKind.CompanyStakeholders,
                TargetCompanyId = companyId,
                Title = "t",
                Body = "b",
                PayloadJson = "{}"
            },
            CancellationToken.None);

        Assert.Equal(2, repo.Inserts.Count);
        Assert.Contains(repo.Inserts, x => x.UserId == u1);
        Assert.Contains(repo.Inserts, x => x.UserId == u2);
        Assert.NotEqual(repo.Inserts[0].CorrelationId, repo.Inserts[1].CorrelationId);
    }

    [Fact]
    public async Task InAppNotificationChannel_SetsExpiresAt_WhenTtlDaysPositive()
    {
        var repo = new CapturingInAppNotificationRepository();
        var inAppOpts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { InAppDefaultTtlDays = 30 });
        var channel = new InAppNotificationChannel(repo, new FixedAdminRecipientIds([]), new EmptyCompanyRecipientIds(), inAppOpts, NullLogger<InAppNotificationChannel>.Instance);
        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                CorrelationId = Guid.NewGuid(),
                Channels = AppNotificationChannelKind.InApp,
                Audience = AppNotificationAudienceKind.User,
                TargetUserId = Guid.NewGuid(),
                Title = "t",
                Body = "b",
                PayloadJson = "{}"
            },
            CancellationToken.None);

        Assert.Single(repo.Inserts);
        Assert.NotNull(repo.Inserts[0].ExpiresAtUtc);
        Assert.True(repo.Inserts[0].ExpiresAtUtc > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task EmailNotificationChannel_Sends_For_User_WithConfirmedEmail()
    {
        var emails = new RecordingEmailPort();
        var uid = Guid.NewGuid();
        var contacts = new FixedContactReader(new AppNotificationUserContact("a@b.c", true, null, true, true, null, false));
        var opts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { EmailEnabled = true });
        var channel = new EmailNotificationChannel(
            emails,
            contacts,
            new FixedAdminRecipientIds([]),
            new EmptyCompanyRecipientIds(),
            opts,
            NullLogger<EmailNotificationChannel>.Instance);

        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                CorrelationId = Guid.NewGuid(),
                Channels = AppNotificationChannelKind.Email,
                Audience = AppNotificationAudienceKind.User,
                TargetUserId = uid,
                Title = "Статус",
                Body = "Відправлено",
                ActionUrl = "https://x/o/1",
                PayloadJson = "{}"
            },
            CancellationToken.None);

        Assert.Single(emails.Sends);
        Assert.Equal("a@b.c", emails.Sends[0].To);
        Assert.Contains("Статус", emails.Sends[0].Subject, StringComparison.Ordinal);
        Assert.Contains("Відправлено", emails.Sends[0].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TelegramNotificationChannel_Skips_When_NoChatId()
    {
        var tg = new RecordingTelegramPort();
        var contacts = new FixedContactReader(new AppNotificationUserContact("a@b.c", true, null, true, true, null, false));
        var opts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { TelegramEnabled = true });
        var channel = new TelegramAppChannel(
            tg,
            contacts,
            new FixedAdminRecipientIds([]),
            new EmptyCompanyRecipientIds(),
            opts,
            NullLogger<TelegramAppChannel>.Instance);

        await channel.DeliverAsync(
            new AppNotificationEnvelope
            {
                TemplateKey = AppNotificationTemplateKeys.UserOrderStatus,
                CorrelationId = Guid.NewGuid(),
                Channels = AppNotificationChannelKind.Telegram,
                Audience = AppNotificationAudienceKind.User,
                TargetUserId = Guid.NewGuid(),
                Title = "t",
                Body = "b",
                PayloadJson = "{}"
            },
            CancellationToken.None);

        Assert.Empty(tg.Sends);
    }

    [Fact]
    public async Task AppNotificationJobs_Prune_DoesNothing_WhenPruneDisabled()
    {
        var repo = new PruneCapturingInAppRepository();
        var opts = new StaticOptionsMonitor<AppNotificationOptions>(new AppNotificationOptions { PruneExpiredInAppEnabled = false });
        var jobs = new AppNotificationJobs(
            Array.Empty<INotificationChannel>(),
            new AppNotificationPayloadBuilder(new StaticOptionsMonitor<FrontendOptions>(new FrontendOptions())),
            repo,
            opts,
            new NoopIntegrationRetryStore(),
            Options.Create(new IntegrationRetryOptions()),
            NullLogger<AppNotificationJobs>.Instance);

        await jobs.PruneExpiredInAppNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, repo.DeleteCalls);
    }

    private sealed class PruneCapturingInAppRepository : IInAppNotificationRepository
    {
        public int DeleteCalls { get; private set; }

        public Task<int> DeleteExpiredBeforeAsync(DateTime utcNow, CancellationToken ct = default)
        {
            DeleteCalls++;
            return Task.FromResult(0);
        }

        public Task<bool> TryInsertAsync(Guid userId, NotificationKind kind, string title, string message, string dataJson, string? actionUrl, Guid? correlationId, DateTime? expiresAtUtc, string? rawPayload, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<PagedInAppNotificationsDto> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<bool> MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class RecordingEmailPort : IEmailPort
    {
        public List<(string To, string Subject, string Body)> Sends { get; } = [];

        public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            Sends.Add((to, subject, body));
            return Task.CompletedTask;
        }

        public Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class RecordingTelegramPort : ITelegramPort
    {
        public List<(string ChatId, string Message)> Sends { get; } = [];

        public Task SendMessageAsync(string chatId, string message, CancellationToken ct = default)
        {
            Sends.Add((chatId, message));
            return Task.CompletedTask;
        }
    }

    private sealed class FixedContactReader : IAppNotificationUserContactReader
    {
        private readonly AppNotificationUserContact _c;

        public FixedContactReader(AppNotificationUserContact c) => _c = c;

        public Task<AppNotificationUserContact?> GetAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<AppNotificationUserContact?>(_c);
    }

    private sealed class CapturingInAppNotificationRepository : IInAppNotificationRepository
    {
        public List<InsertCall> Inserts { get; } = [];

        public Task<bool> TryInsertAsync(
            Guid userId,
            NotificationKind kind,
            string title,
            string message,
            string dataJson,
            string? actionUrl,
            Guid? correlationId,
            DateTime? expiresAtUtc,
            string? rawPayload,
            CancellationToken ct = default)
        {
            Inserts.Add(new InsertCall(userId, kind, title, message, dataJson, actionUrl, correlationId, expiresAtUtc, rawPayload));
            return Task.FromResult(true);
        }

        public Task<PagedInAppNotificationsDto> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<bool> MarkReadAsync(Guid userId, long notificationId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<int> DeleteExpiredBeforeAsync(DateTime utcNow, CancellationToken ct = default) =>
            Task.FromResult(0);

        public sealed record InsertCall(
            Guid UserId,
            NotificationKind Kind,
            string Title,
            string Message,
            string DataJson,
            string? ActionUrl,
            Guid? CorrelationId,
            DateTime? ExpiresAtUtc,
            string? RawPayload);
    }

    private sealed class FixedAdminRecipientIds : IAdminNotificationRecipientIds
    {
        private readonly IReadOnlyList<Guid> _ids;

        public FixedAdminRecipientIds(IReadOnlyList<Guid> ids) => _ids = ids;

        public Task<IReadOnlyList<Guid>> ListAdminUserIdsAsync(CancellationToken ct = default) => Task.FromResult(_ids);
    }

    private sealed class EmptyCompanyRecipientIds : ICompanyOrderNotificationRecipientIds
    {
        public Task<IReadOnlyList<Guid>> ListOwnerAndManagerUserIdsAsync(Guid companyId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class FixedCompanyRecipientIds : ICompanyOrderNotificationRecipientIds
    {
        private readonly Guid _companyId;
        private readonly IReadOnlyList<Guid> _userIds;

        public FixedCompanyRecipientIds(Guid companyId, IReadOnlyList<Guid> userIds)
        {
            _companyId = companyId;
            _userIds = userIds;
        }

        public Task<IReadOnlyList<Guid>> ListOwnerAndManagerUserIdsAsync(Guid companyId, CancellationToken ct = default) =>
            Task.FromResult(companyId == _companyId ? _userIds : Array.Empty<Guid>());
    }

    [Fact]
    public async Task PushNotificationsController_Register_Rejects_Admin_Only_For_NonAdmin()
    {
        var repo = new CapturingPushSubscriptionRepository();
        var webPush = new StaticOptionsMonitor<WebPushOptions>(new WebPushOptions());
        var controller = new PushNotificationsController(repo, webPush)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("sub", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Buyer")
                    ],
                    "test"))
                }
            }
        };

        var result = await controller.RegisterSubscription(
            new PushNotificationsController.RegisterPushSubscriptionRequest
            {
                Endpoint = "https://ep",
                P256dh = "dh",
                Auth = "au",
                IncludeUserChannel = false,
                IncludeAdminChannel = true
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(repo.LastFlags);
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        private readonly T _value;

        public StaticOptionsMonitor(T value) => _value = value;

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }

    private sealed class FakePushSubscriptionRepository : IPushSubscriptionRepository
    {
        public List<PushSubscriptionDto> Subscriptions { get; } = [];

        public Task UpsertAsync(Guid userId, string endpoint, string p256dh, string auth,
            PushSubscriptionAudienceFlags audienceFlags, string? userAgent, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task DeleteByUserAndEndpointAsync(Guid userId, string endpoint, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<PushSubscriptionDto>> ListByUserAndAudienceAsync(Guid userId,
            PushSubscriptionAudienceFlags requiredFlags, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PushSubscriptionDto>>(Subscriptions
                .Where(s => s.UserId == userId && (s.AudienceFlags & requiredFlags) == requiredFlags).ToList());

        public Task<IReadOnlyList<PushSubscriptionDto>> ListByAudienceFlagAsync(
            PushSubscriptionAudienceFlags requiredFlag, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PushSubscriptionDto>>(Subscriptions
                .Where(s => (s.AudienceFlags & requiredFlag) != 0).ToList());

        public Task DeleteByIdAsync(long id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class RecordingPushDeliveryClient : IPushDeliveryClient
    {
        public List<PushDeliveryRequest> Requests { get; } = [];

        public Task SendAsync(PushDeliveryRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPushSubscriptionRepository : IPushSubscriptionRepository
    {
        public PushSubscriptionAudienceFlags? LastFlags { get; private set; }

        public Task UpsertAsync(Guid userId, string endpoint, string p256dh, string auth,
            PushSubscriptionAudienceFlags audienceFlags, string? userAgent, CancellationToken ct = default)
        {
            LastFlags = audienceFlags;
            return Task.CompletedTask;
        }

        public Task DeleteByUserAndEndpointAsync(Guid userId, string endpoint, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<PushSubscriptionDto>> ListByUserAndAudienceAsync(Guid userId,
            PushSubscriptionAudienceFlags requiredFlags, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PushSubscriptionDto>>([]);

        public Task<IReadOnlyList<PushSubscriptionDto>> ListByAudienceFlagAsync(
            PushSubscriptionAudienceFlags requiredFlag, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PushSubscriptionDto>>([]);

        public Task DeleteByIdAsync(long id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopIntegrationRetryStore : IIntegrationRetryStore
    {
        public Task UpsertAsync(IntegrationRetryUpsert request, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<IntegrationRetryEntry>> ListDueAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IntegrationRetryEntry>>([]);
        public Task MarkResolvedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
    }
}
