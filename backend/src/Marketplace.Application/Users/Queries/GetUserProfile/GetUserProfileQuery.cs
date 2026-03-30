using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;


namespace Marketplace.Application.Users.Queries.GetUserProfile
{
    /// <param name="IdentityUserId">Значення claim <c>sub</c> у JWT (ASP.NET Identity user id).</param>
    public record GetUserProfileQuery(Guid IdentityUserId) : IRequest<Result<UserDto>>;
}
