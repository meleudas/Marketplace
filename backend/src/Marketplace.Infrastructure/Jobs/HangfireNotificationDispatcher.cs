using Hangfire;
using Marketplace.Application.Auth.Ports;

namespace Marketplace.Infrastructure.Jobs;

public sealed class HangfireNotificationDispatcher : INotificationDispatcher
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireNotificationDispatcher(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public Task EnqueueConfirmationEmailAsync(string to, string token, CancellationToken ct = default)
    {
        _jobs.Enqueue<NotificationJobs>(x => x.SendConfirmationEmailAsync(to, token));
        return Task.CompletedTask;
    }

    public Task EnqueuePasswordResetEmailAsync(string to, string token, CancellationToken ct = default)
    {
        _jobs.Enqueue<NotificationJobs>(x => x.SendPasswordResetEmailAsync(to, token));
        return Task.CompletedTask;
    }

    public Task EnqueueTwoFactorEmailAsync(string to, string code, CancellationToken ct = default)
    {
        _jobs.Enqueue<NotificationJobs>(x => x.SendTwoFactorEmailAsync(to, code));
        return Task.CompletedTask;
    }

    public Task EnqueueTelegramMessageAsync(string chatId, string message, CancellationToken ct = default)
    {
        _jobs.Enqueue<NotificationJobs>(x => x.SendTelegramMessageAsync(chatId, message));
        return Task.CompletedTask;
    }

    public Task EnqueueSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _jobs.Enqueue<NotificationJobs>(x => x.SendSmsAsync(phoneNumber, message));
        return Task.CompletedTask;
    }
}
