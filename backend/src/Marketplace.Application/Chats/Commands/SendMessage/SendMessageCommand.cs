using FluentValidation;
using Marketplace.Application.Chats;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Policies;
using Marketplace.Application.Chats.Ports;
using Marketplace.Application.Notifications;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Marketplace.Application.Chats.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ActorUserId,
    Guid ChatId,
    string Text,
    long? ReplyToMessageId,
    bool IsPlatformStaff) : IRequest<Result<ChatMessageDto>>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator(IOptions<ChatsOptions> options)
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(options.Value.MaxMessageLength);
    }
}

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<ChatMessageDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly ChatAccessPolicy _accessPolicy;
    private readonly ChatAntiSpamPolicy _antiSpamPolicy;
    private readonly ChatContentModerationPolicy _contentPolicy;
    private readonly IAppNotificationScheduler _notifications;
    private readonly IChatRealtimeNotifier _realtime;
    private readonly ChatsOptions _options;

    public SendMessageCommandHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IChatParticipantRepository participantRepository,
        ChatAccessPolicy accessPolicy,
        ChatAntiSpamPolicy antiSpamPolicy,
        ChatContentModerationPolicy contentPolicy,
        IAppNotificationScheduler notifications,
        IChatRealtimeNotifier realtime,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _participantRepository = participantRepository;
        _accessPolicy = accessPolicy;
        _antiSpamPolicy = antiSpamPolicy;
        _contentPolicy = contentPolicy;
        _notifications = notifications;
        _realtime = realtime;
        _options = options.Value;
    }

    public async Task<Result<ChatMessageDto>> Handle(SendMessageCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ChatMessageDto>.Failure("conflict: chats are disabled");

        var chatId = ChatId.From(request.ChatId);
        if (!await _accessPolicy.CanAccessAsync(chatId, request.ActorUserId, request.IsPlatformStaff, ct))
            return Result<ChatMessageDto>.Failure("forbidden: not a chat participant");

        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat is null)
            return Result<ChatMessageDto>.Failure("Chat not found");
        if (!chat.CanAcceptMessage())
            return Result<ChatMessageDto>.Failure("conflict: chat cannot accept messages");

        if (!request.IsPlatformStaff)
        {
            var participant = await _participantRepository.GetAsync(chatId, request.ActorUserId, ct);
            if (participant is null || !participant.IsActive)
                return Result<ChatMessageDto>.Failure("forbidden: sender is not an active participant");
        }

        var moderation = _contentPolicy.Evaluate(request.Text);
        if (!moderation.Allowed)
        {
            if (_options.RejectOnProhibitedContent)
                return Result<ChatMessageDto>.Failure("unprocessable: prohibited chat content");
        }

        var spam = await _antiSpamPolicy.EvaluateAsync(chatId, request.ActorUserId, request.Text, ct);
        if (!spam.Allowed)
            return Result<ChatMessageDto>.Failure(spam.Reason ?? "rate exceeded");

        var now = DateTime.UtcNow;
        MessageId? replyTo = request.ReplyToMessageId.HasValue
            ? MessageId.From(request.ReplyToMessageId.Value)
            : null;

        var message = Message.Send(
            chatId,
            request.ActorUserId,
            request.Text,
            JsonBlob.Empty,
            replyTo,
            now);

        if (!moderation.Allowed && !_options.RejectOnProhibitedContent)
            message.MarkDeletedForPolicy(Guid.Empty, moderation.MatchedPattern ?? "prohibited", now);

        var saved = await _messageRepository.AddAsync(message, ct);
        chat.RecordLastMessage(saved.Text, saved.SenderId, saved.CreatedAt);
        await _chatRepository.UpdateAsync(chat, ct);

        var recipients = await _participantRepository.ListActiveByChatAsync(chatId, ct);
        foreach (var recipient in recipients.Where(x => x.UserId != request.ActorUserId))
        {
            await _notifications.ScheduleAsync(
                new AppNotificationRequest
                {
                    TemplateKey = AppNotificationTemplateKeys.ChatMessageReceived,
                    CorrelationId = CreateMessageCorrelationId(chatId.Value, saved.Id.Value),
                    Channels = AppNotificationChannelKind.InApp | AppNotificationChannelKind.Push,
                    Audience = AppNotificationAudienceKind.User,
                    TargetUserId = recipient.UserId,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        chatId = chatId.Value,
                        messageId = saved.Id.Value,
                        senderId = saved.SenderId,
                        preview = saved.Text.Length > 120 ? saved.Text[..120] : saved.Text
                    })
                },
                ct);
        }

        if (_options.RealtimeEnabled)
        {
            await _realtime.NotifyMessageReceivedAsync(
                chatId.Value,
                saved.Id.Value,
                saved.SenderId,
                saved.Text,
                ct);
        }

        return Result<ChatMessageDto>.Success(saved.ToDto());
    }

    private static Guid CreateMessageCorrelationId(Guid chatId, long messageId)
    {
        Span<byte> bytes = stackalloc byte[16];
        chatId.TryWriteBytes(bytes);
        BitConverter.TryWriteBytes(bytes[8..], messageId);
        return new Guid(bytes);
    }
}
