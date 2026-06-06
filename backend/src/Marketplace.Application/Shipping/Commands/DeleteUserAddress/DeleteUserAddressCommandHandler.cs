using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Commands.DeleteUserAddress;

public sealed class DeleteUserAddressCommandHandler : IRequestHandler<DeleteUserAddressCommand, Result>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public DeleteUserAddressCommandHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result> Handle(DeleteUserAddressCommand request, CancellationToken ct)
    {
        try
        {
            var existing = await _userAddressRepository.GetByIdAsync(UserAddressId.From(request.AddressId), ct);
            if (existing is null || existing.UserId != request.ActorUserId)
                return Result.Failure("Address not found");

            await _userAddressRepository.SoftDeleteAsync(existing.Id, DateTime.UtcNow, ct);
            return Result.Success();
        }
        catch
        {
            return Result.Failure("Failed to delete address");
        }
    }
}
