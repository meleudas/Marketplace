using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.CreateUserAddress;

public sealed class CreateUserAddressCommandHandler : IRequestHandler<CreateUserAddressCommand, Result<UserAddressDto>>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public CreateUserAddressCommandHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result<UserAddressDto>> Handle(CreateUserAddressCommand request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<UserAddressType>(request.Type, true, out var type))
                return Result<UserAddressDto>.Failure("Invalid address type");

            if (request.IsDefault)
                await _userAddressRepository.ClearDefaultAsync(request.ActorUserId, ct);

            var now = DateTime.UtcNow;
            var created = UserAddress.Reconstitute(
                UserAddressId.From(0),
                request.ActorUserId,
                type,
                request.IsDefault,
                ContactPerson.Create(request.FirstName, request.LastName, request.Phone),
                Address.Create(request.Street, request.City, request.State, request.PostalCode, request.Country),
                now,
                now,
                false,
                null);

            var saved = await _userAddressRepository.AddAsync(created, ct);
            return Result<UserAddressDto>.Success(saved.ToDto());
        }
        catch
        {
            return Result<UserAddressDto>.Failure("Failed to create address");
        }
    }
}
