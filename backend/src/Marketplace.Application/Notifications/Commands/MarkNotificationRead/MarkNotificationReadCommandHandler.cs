using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IInAppNotificationRepository _notifications;

    public MarkNotificationReadCommandHandler(IInAppNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var ok = await _notifications.MarkReadAsync(request.ActorUserId, request.NotificationId, ct);
        return ok ? Result.Success() : Result.Failure("Notification not found.");
    }
}
