using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid ActorUserId, long NotificationId) : IRequest<Result>;
