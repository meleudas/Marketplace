using Marketplace.Application.Behavior.Commands.TrackCatalogInteraction;
using Marketplace.Domain.Behavior.Enums;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Behavior.Commands.TrackProductView;

public sealed class TrackProductViewCommandHandler : IRequestHandler<TrackProductViewCommand, Result>
{
    private readonly ISender _sender;

    public TrackProductViewCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task<Result> Handle(TrackProductViewCommand request, CancellationToken ct)
        => _sender.Send(
            new TrackCatalogInteractionCommand(
                request.ActorUserId,
                request.SessionId,
                (short)BehaviorEventType.ProductView,
                request.Source,
                request.Payload,
                request.ConsentGranted),
            ct);
}
