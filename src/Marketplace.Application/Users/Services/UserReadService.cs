using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Users.Queries.GetAllUsers;
using Marketplace.Application.Users.Queries.GetUserProfile;
using Marketplace.Application.Users.Queries.GetUsersByUserName;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Services;

public class UserReadService : IUserReadService
{
    private readonly ISender _sender;

    public UserReadService(ISender sender)
    {
        _sender = sender;
    }

    public Task<Result<UserDto>> GetMeAsync(Guid identityUserId, CancellationToken ct = default) =>
        _sender.Send(new GetUserProfileQuery(identityUserId), ct);

    public Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default) =>
        _sender.Send(new GetAllUsersQuery(), ct);

    public Task<Result<IReadOnlyList<UserDto>>> SearchByUserNameAsync(string userName, CancellationToken ct = default) =>
        _sender.Send(new GetUsersByUserNameQuery(userName), ct);
}
