using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.Application.Chats.Commands.CreateChat;
using Marketplace.Application.Chats.Commands.SendMessage;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Queries.ListMyChats;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "API")]
[Trait("Suite", "Chats")]
public sealed class ApiChatsControllerTests
{
    [Fact]
    public async Task List_Sends_Query()
    {
        var sender = new RecordingSender { NextResult = Result<ChatListDto>.Success(new ChatListDto([], 0, 1, 20)) };
        var controller = BuildController(sender);

        var result = await controller.List(1, 20, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ListMyChatsQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task Create_Sends_Command()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ChatDto>.Success(new ChatDto(Guid.NewGuid(), 2, 0, 2, null, null, null, null, 0, DateTime.UtcNow, DateTime.UtcNow))
        };
        var controller = BuildController(sender);

        var result = await controller.Create(new CreateChatRequest(2, null, 2), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<CreateChatCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task SendMessage_Sends_Command()
    {
        var sender = new RecordingSender
        {
            NextResult = Result<ChatMessageDto>.Success(new ChatMessageDto(1, Guid.NewGuid(), Guid.NewGuid(), "hi", 0, null, null, DateTime.UtcNow))
        };
        var controller = BuildController(sender);

        var result = await controller.SendMessage(Guid.NewGuid(), new SendChatMessageRequest("hello", null), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<SendMessageCommand>(sender.LastRequest);
    }

    private static ChatsController BuildController(ISender sender)
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "Buyer")],
            "test");
        return new ChatsController(sender, Options.Create(new ChatsOptions { Enabled = true }))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }
        public object? NextResult { get; set; }

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

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
