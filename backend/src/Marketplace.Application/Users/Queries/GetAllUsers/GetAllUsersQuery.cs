using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<Result<IReadOnlyList<UserDto>>>;
