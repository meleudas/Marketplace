using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;

namespace Marketplace.Application.Users.Services;

public interface IUserReadService
{
    Task<Result<UserDto>> GetMeAsync(Guid identityUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserDto>>> SearchByUserNameAsync(string userName, CancellationToken ct = default);
}
