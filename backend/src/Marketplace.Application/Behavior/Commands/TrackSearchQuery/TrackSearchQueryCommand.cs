using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Commands.TrackSearchQuery;

public sealed record TrackSearchQueryCommand(
    Guid? ActorUserId,
    string SessionId,
    string Query,
    string Payload,
    bool? ConsentGranted) : IRequest<Result>;
