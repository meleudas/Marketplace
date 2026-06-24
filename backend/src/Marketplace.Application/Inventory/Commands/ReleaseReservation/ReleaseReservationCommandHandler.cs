using Marketplace.Application.Inventory.Authorization;
using Marketplace.Application.Inventory.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Inventory.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Inventory.Commands.ReleaseReservation;

public sealed class ReleaseReservationCommandHandler : IRequestHandler<ReleaseReservationCommand, Result>
{
    private readonly IInventoryAccessService _access;
    private readonly IInventoryReservationRepository _reservationRepository;
    private readonly IInventoryReservationReleaseService _releaseService;

    public ReleaseReservationCommandHandler(
        IInventoryAccessService access,
        IInventoryReservationRepository reservationRepository,
        IInventoryReservationReleaseService releaseService)
    {
        _access = access;
        _reservationRepository = reservationRepository;
        _releaseService = releaseService;
    }

    public async Task<Result> Handle(ReleaseReservationCommand request, CancellationToken ct)
    {
        try
        {
            if (!await _access.HasAccessAsync(request.CompanyId, request.ActorUserId, request.IsActorAdmin, InventoryPermission.WriteStock, ct))
                return Result.Failure("Forbidden");

            var companyId = CompanyId.From(request.CompanyId);
            var reservation = await _reservationRepository.GetByCodeAsync(companyId, request.ReservationCode, ct);
            if (reservation is null)
                return Result.Failure("Reservation not found");
            if (reservation.Status != Domain.Inventory.Enums.InventoryReservationStatus.Active)
                return Result.Success();

            await _releaseService.ReleaseAsync(reservation, request.ActorUserId, "manual-release", expired: false, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to release reservation: {ex.Message}");
        }
    }
}
