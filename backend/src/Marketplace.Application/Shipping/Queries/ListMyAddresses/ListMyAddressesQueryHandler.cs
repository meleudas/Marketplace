using Marketplace.Application.Shipping.DTOs;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Shipping.Repositories;
using MediatR;

namespace Marketplace.Application.Shipping.Queries.ListMyAddresses;

public sealed class ListMyAddressesQueryHandler : IRequestHandler<ListMyAddressesQuery, Result<IReadOnlyList<UserAddressDto>>>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public ListMyAddressesQueryHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result<IReadOnlyList<UserAddressDto>>> Handle(ListMyAddressesQuery request, CancellationToken ct)
    {
        try
        {
            var items = await _userAddressRepository.ListByUserAsync(request.ActorUserId, ct);
            return Result<IReadOnlyList<UserAddressDto>>.Success(items.Select(x => x.ToDto()).ToList());
        }
        catch
        {
            return Result<IReadOnlyList<UserAddressDto>>.Failure("Failed to list addresses");
        }
    }
}
