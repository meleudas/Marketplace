using Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Commands.TrackSearchQuery;

public sealed class TrackSearchQueryCommandHandler : IRequestHandler<TrackSearchQueryCommand, Result>
{
    private readonly ISender _sender;

    public TrackSearchQueryCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task<Result> Handle(TrackSearchQueryCommand request, CancellationToken ct)
        => _sender.Send(
            new TrackCatalogInteractionCommand(
                request.ActorUserId,
                request.SessionId,
                (short)BehaviorEventType.SearchQuery,
                "search",
                $$"""{"query":"{{request.Query}}","payload":{{request.Payload}}}""",
                request.ConsentGranted),
            ct);
}
