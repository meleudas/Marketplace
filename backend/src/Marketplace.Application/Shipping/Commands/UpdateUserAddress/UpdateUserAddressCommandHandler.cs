using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.UpdateUserAddress;

public sealed class UpdateUserAddressCommandHandler : IRequestHandler<UpdateUserAddressCommand, Result<UserAddressDto>>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public UpdateUserAddressCommandHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result<UserAddressDto>> Handle(UpdateUserAddressCommand request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<UserAddressType>(request.Type, true, out var type))
                return Result<UserAddressDto>.Failure("Invalid address type");

            var existing = await _userAddressRepository.GetByIdAsync(UserAddressId.From(request.AddressId), ct);
            if (existing is null || existing.UserId != request.ActorUserId)
                return Result<UserAddressDto>.Failure("Address not found");

            if (request.IsDefault)
                await _userAddressRepository.ClearDefaultAsync(request.ActorUserId, ct);

            var now = DateTime.UtcNow;
            var updated = Domain.Shipping.Entities.UserAddress.Reconstitute(
                existing.Id,
                existing.UserId,
                type,
                request.IsDefault,
                ContactPerson.Create(request.FirstName, request.LastName, request.Phone),
                Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country),
                existing.CreatedAt,
                now,
                existing.IsDeleted,
                existing.DeletedAt);

            await _userAddressRepository.UpdateAsync(updated, ct);
            return Result<UserAddressDto>.Success(updated.ToDto());
        }
        catch
        {
            return Result<UserAddressDto>.Failure("Failed to update address");
        }
    }
}
