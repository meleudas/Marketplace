using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;

namespace Marketplace.Application.Users.Queries.GetUsersByUserName;

public record GetUsersByUserNameQuery(string UserName) : IRequest<Result<IReadOnlyList<UserDto>>>;
