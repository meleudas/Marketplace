using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Auth.DTOs;
using Marketplace.Application.Users.Commands.AssignUserRole;
using Marketplace.Application.Users.Commands.UpdateMyProfile;
using Marketplace.Application.Users.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "IdentityAccess")]
public class ApiUsersControllerTests
{
    [Fact]
    public async Task Me_Returns_Unauthorized_Without_Sub()
    {
        var controller = BuildController(new StubUserReadService(), new StubUserManagementService(), new RecordingSender());
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var response = await controller.Me(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response);
    }

    [Fact]
    public async Task AssignRole_Returns_BadRequest_For_Invalid_Role()
    {
        var controller = BuildController(new StubUserReadService(), new StubUserManagementService(), new RecordingSender());

        var response = await controller.AssignRole(Guid.NewGuid(), new AssignUserRoleRequest("nope"), CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task AssignRole_Sends_Command_For_Valid_Role()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(new StubUserReadService(), new StubUserManagementService(), sender);

        var response = await controller.AssignRole(Guid.NewGuid(), new AssignUserRoleRequest("Admin"), CancellationToken.None);

        Assert.IsType<OkResult>(response);
        Assert.IsType<AssignUserRoleCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task UpdateMyProfile_Returns_Unauthorized_Without_Sub()
    {
        var controller = BuildController(new StubUserReadService(), new StubUserManagementService(), new RecordingSender());
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var response = await controller.UpdateMyProfile(new UpdateMyProfileRequest("new_user_name", "+380501234567"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(response);
    }

    [Fact]
    public async Task UpdateMyProfile_Sends_Command_For_Valid_Request()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildController(new StubUserReadService(), new StubUserManagementService(), sender);

        var response = await controller.UpdateMyProfile(new UpdateMyProfileRequest("new_user_name", "+380501234567"), CancellationToken.None);

        Assert.IsType<OkResult>(response);
        Assert.IsType<UpdateMyProfileCommand>(sender.LastRequest);
    }

    private static UsersController BuildController(IUserReadService readService, IUserManagementService managementService, ISender sender)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");

        return new UsersController(readService, managementService, sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class StubUserReadService : IUserReadService
    {
        public Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Result<IReadOnlyList<UserDto>>.Success([]));

        public Task<Result<UserDto>> GetMeAsync(Guid identityUserId, CancellationToken ct = default)
            => Task.FromResult(Result<UserDto>.Success(new UserDto(identityUserId, "First", "Last", null, "buyer", "user@example.com", "+380501234567", null, null, true, null, null, DateTime.UtcNow, DateTime.UtcNow, false, null, [])));

        public Task<Result<IReadOnlyList<UserDto>>> SearchByUserNameAsync(string userName, CancellationToken ct = default)
            => Task.FromResult(Result<IReadOnlyList<UserDto>>.Success([]));
    }

    private sealed class StubUserManagementService : IUserManagementService
    {
        public Task<Result> DeleteAccountAsync(Guid identityUserId, CancellationToken ct = default)
            => Task.FromResult(Result.Success());
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }
        public object? NextResult { get; set; } = Result.Success();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)NextResult!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(NextResult);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
