using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReleaseReservation;

public sealed record ReleaseReservationCommand(
    Guid CompanyId,
    string ReservationCode,
    Guid ActorUserId,
    bool IsActorAdmin) : IRequest<Result>;
