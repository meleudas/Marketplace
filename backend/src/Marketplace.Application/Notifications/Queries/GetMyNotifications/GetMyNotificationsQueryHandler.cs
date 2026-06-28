using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<PagedInAppNotificationsDto>>
{
    private readonly IInAppNotificationRepository _notifications;

    public GetMyNotificationsQueryHandler(IInAppNotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<PagedInAppNotificationsDto>> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        if (request.Page < 1)
            return Result<PagedInAppNotificationsDto>.Failure("Page must be at least 1.");
        if (request.PageSize is < 1 or > 100)
            return Result<PagedInAppNotificationsDto>.Failure("PageSize must be between 1 and 100.");

        var page = await _notifications.ListForUserAsync(request.ActorUserId, request.Page, request.PageSize, ct);
        return Result<PagedInAppNotificationsDto>.Success(page);
    }
}
