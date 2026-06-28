using FluentValidation;
using Marketplace.Application.Chats;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Policies;
using Marketplace.Application.Chats.Ports;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Commands.ArchiveChat;

public sealed record ArchiveChatCommand(
    Guid ActorUserId,
    Guid ChatId,
    bool IsPlatformStaff) : IRequest<Result<ChatDto>>;

public sealed class ArchiveChatCommandValidator : AbstractValidator<ArchiveChatCommand>
{
    public ArchiveChatCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ChatId).NotEmpty();
    }
}

public sealed class ArchiveChatCommandHandler : IRequestHandler<ArchiveChatCommand, Result<ChatDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IChatReadStateRepository _readStateRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ChatAccessPolicy _accessPolicy;
    private readonly IChatRealtimeNotifier _realtime;
    private readonly ChatsOptions _options;

    public ArchiveChatCommandHandler(
        IChatRepository chatRepository,
        IChatReadStateRepository readStateRepository,
        IMessageRepository messageRepository,
        ChatAccessPolicy accessPolicy,
        IChatRealtimeNotifier realtime,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _readStateRepository = readStateRepository;
        _messageRepository = messageRepository;
        _accessPolicy = accessPolicy;
        _realtime = realtime;
        _options = options.Value;
    }

    public async Task<Result<ChatDto>> Handle(ArchiveChatCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ChatDto>.Failure("conflict: chats are disabled");

        var chatId = ChatId.From(request.ChatId);
        if (!await _accessPolicy.CanAccessAsync(chatId, request.ActorUserId, request.IsPlatformStaff, ct))
            return Result<ChatDto>.Failure("forbidden: not a chat participant");

        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat is null)
            return Result<ChatDto>.Failure("Chat not found");

        chat.Archive(request.ActorUserId, DateTime.UtcNow);
        await _chatRepository.UpdateAsync(chat, ct);

        if (_options.RealtimeEnabled)
            await _realtime.NotifyChatArchivedAsync(chatId.Value, ct);

        var readState = await _readStateRepository.GetAsync(chatId, request.ActorUserId, ct);
        var unread = await _messageRepository.CountUnreadForUserAsync(
            chatId,
            request.ActorUserId,
            readState?.LastReadMessageId,
            ct);

        return Result<ChatDto>.Success(chat.ToDto(unread));
    }
}
