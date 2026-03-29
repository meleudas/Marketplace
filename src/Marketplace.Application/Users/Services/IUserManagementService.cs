using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Users.Services;

public interface IUserManagementService
{
    Task<Result> DeleteAccountAsync(Guid identityUserId, CancellationToken ct = default);
}
