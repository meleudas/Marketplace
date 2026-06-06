using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Commands.TrackProductView;

public sealed record TrackProductViewCommand(
    Guid? ActorUserId,
    string SessionId,
    long ProductId,
    string Source,
    string Payload,
    bool? ConsentGranted) : IRequest<Result>;
