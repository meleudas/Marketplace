using Marketplace.Application.Users.Commands.DeleteUser;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ISender _sender;

    public UserManagementService(ISender sender)
    {
        _sender = sender;
    }

    public Task<Result> DeleteAccountAsync(Guid identityUserId, CancellationToken ct = default) =>
        _sender.Send(new DeleteUserCommand(identityUserId), ct);
}
