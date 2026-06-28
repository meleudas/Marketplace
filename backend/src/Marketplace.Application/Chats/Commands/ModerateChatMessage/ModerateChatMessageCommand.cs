using FluentValidation;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Commands.ModerateChatMessage;

public sealed record ModerateChatMessageCommand(
    Guid ModeratorUserId,
    Guid ChatId,
    long? MessageId,
    short ActionType,
    string Reason) : IRequest<Result<ChatModerationResultDto>>;

public sealed class ModerateChatMessageCommandValidator : AbstractValidator<ModerateChatMessageCommand>
{
    public ModerateChatMessageCommandValidator()
    {
        RuleFor(x => x.ModeratorUserId).NotEmpty();
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.ActionType).InclusiveBetween((short)0, (short)2);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ModerateChatMessageCommandHandler : IRequestHandler<ModerateChatMessageCommand, Result<ChatModerationResultDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IChatModerationActionRepository _moderationRepository;
    private readonly ChatsOptions _options;

    public ModerateChatMessageCommandHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IChatModerationActionRepository moderationRepository,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _moderationRepository = moderationRepository;
        _options = options.Value;
    }

    public async Task<Result<ChatModerationResultDto>> Handle(ModerateChatMessageCommand request, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.ModerationEnabled)
            return Result<ChatModerationResultDto>.Failure("conflict: chat moderation is disabled");

        var chatId = ChatId.From(request.ChatId);
        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat is null)
            return Result<ChatModerationResultDto>.Failure("Chat not found");

        var now = DateTime.UtcNow;
        var actionType = (ChatModerationActionType)request.ActionType;
        MessageId? messageId = request.MessageId.HasValue ? MessageId.From(request.MessageId.Value) : null;

        if (messageId is not null && actionType == ChatModerationActionType.Hide)
        {
            var message = await _messageRepository.GetByIdAsync(messageId, ct);
            if (message is null || message.ChatId.Value != chatId.Value)
                return Result<ChatModerationResultDto>.Failure("Message not found");

            message.MarkDeletedForPolicy(request.ModeratorUserId, request.Reason, now);
            await _messageRepository.UpdateAsync(message, ct);
        }

        if (actionType == ChatModerationActionType.BlockChat)
            chat.Block(request.Reason, now);

        await _chatRepository.UpdateAsync(chat, ct);

        var action = await _moderationRepository.AppendAsync(
            ChatModerationAction.Create(
                chatId,
                messageId,
                actionType,
                request.ModeratorUserId,
                request.Reason,
                now),
            ct);

        return Result<ChatModerationResultDto>.Success(new ChatModerationResultDto(
            action.Id,
            chatId.Value,
            messageId?.Value,
            (short)actionType,
            (short)chat.Status));
    }
}
