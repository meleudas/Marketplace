using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.SetDefaultUserAddress;

public sealed class SetDefaultUserAddressCommandHandler : IRequestHandler<SetDefaultUserAddressCommand, Result>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public SetDefaultUserAddressCommandHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result> Handle(SetDefaultUserAddressCommand request, CancellationToken ct)
    {
        try
        {
            var existing = await _userAddressRepository.GetByIdAsync(UserAddressId.From(request.AddressId), ct);
            if (existing is null || existing.UserId != request.ActorUserId)
                return Result.Failure("Address not found");

            await _userAddressRepository.ClearDefaultAsync(request.ActorUserId, ct);
            var now = DateTime.UtcNow;
            var updated = UserAddress.Reconstitute(
                existing.Id,
                existing.UserId,
                existing.Type,
                true,
                existing.Contact,
                existing.Address,
                existing.CreatedAt,
                now,
                existing.IsDeleted,
                existing.DeletedAt);
            await _userAddressRepository.UpdateAsync(updated, ct);
            return Result.Success();
        }
        catch
        {
            return Result.Failure("Failed to set default address");
        }
    }
}
