using FluentValidation;
using Marketplace.Application.Chats;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Policies;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Queries.GetChatMessages;

public sealed record GetChatMessagesQuery(
    Guid ActorUserId,
    Guid ChatId,
    int Page,
    int Size,
    bool IsPlatformStaff) : IRequest<Result<ChatMessagesDto>>;

public sealed class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}

public sealed class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, Result<ChatMessagesDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ChatAccessPolicy _accessPolicy;
    private readonly ChatsOptions _options;

    public GetChatMessagesQueryHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        ChatAccessPolicy accessPolicy,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _accessPolicy = accessPolicy;
        _options = options.Value;
    }

    public async Task<Result<ChatMessagesDto>> Handle(GetChatMessagesQuery request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ChatMessagesDto>.Failure("conflict: chats are disabled");

        var chatId = ChatId.From(request.ChatId);
        if (!await _accessPolicy.CanAccessAsync(chatId, request.ActorUserId, request.IsPlatformStaff, ct))
            return Result<ChatMessagesDto>.Failure("forbidden: not a chat participant");

        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat is null)
            return Result<ChatMessagesDto>.Failure("Chat not found");

        var skip = (request.Page - 1) * request.Size;
        var total = await _messageRepository.CountByChatAsync(chatId, ct);
        var messages = await _messageRepository.ListByChatAsync(chatId, skip, request.Size, ct);

        return Result<ChatMessagesDto>.Success(new ChatMessagesDto(
            messages.Select(x => x.ToDto()).ToList(),
            total,
            request.Page,
            request.Size));
    }
}
