using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    Guid IdentityUserId,
    string UserName,
    string? PhoneNumber) : IRequest<Result>;
