using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(Guid ActorUserId, int Page, int PageSize)
    : IRequest<Result<PagedInAppNotificationsDto>>;
