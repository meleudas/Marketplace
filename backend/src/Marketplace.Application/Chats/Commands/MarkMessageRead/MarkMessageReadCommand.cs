using FluentValidation;
using Marketplace.Application.Chats;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Application.Chats.Policies;
using Marketplace.Application.Chats.Ports;
using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Commands.MarkMessageRead;

public sealed record MarkMessageReadCommand(
    Guid ActorUserId,
    Guid ChatId,
    long MessageId,
    bool IsPlatformStaff) : IRequest<Result<ChatMessageDto>>;

public sealed class MarkMessageReadCommandValidator : AbstractValidator<MarkMessageReadCommand>
{
    public MarkMessageReadCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.MessageId).GreaterThan(0);
    }
}

public sealed class MarkMessageReadCommandHandler : IRequestHandler<MarkMessageReadCommand, Result<ChatMessageDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IChatReadStateRepository _readStateRepository;
    private readonly ChatAccessPolicy _accessPolicy;
    private readonly IChatRealtimeNotifier _realtime;
    private readonly ChatsOptions _options;

    public MarkMessageReadCommandHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IChatReadStateRepository readStateRepository,
        ChatAccessPolicy accessPolicy,
        IChatRealtimeNotifier realtime,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _readStateRepository = readStateRepository;
        _accessPolicy = accessPolicy;
        _realtime = realtime;
        _options = options.Value;
    }

    public async Task<Result<ChatMessageDto>> Handle(MarkMessageReadCommand request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ChatMessageDto>.Failure("conflict: chats are disabled");

        var chatId = ChatId.From(request.ChatId);
        if (!await _accessPolicy.CanAccessAsync(chatId, request.ActorUserId, request.IsPlatformStaff, ct))
            return Result<ChatMessageDto>.Failure("forbidden: not a chat participant");

        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat is null)
            return Result<ChatMessageDto>.Failure("Chat not found");

        var messageId = MessageId.From(request.MessageId);
        var message = await _messageRepository.GetByIdAsync(messageId, ct);
        if (message is null || message.ChatId.Value != chatId.Value)
            return Result<ChatMessageDto>.Failure("Message not found");

        var now = DateTime.UtcNow;
        var existing = await _readStateRepository.GetAsync(chatId, request.ActorUserId, ct);
        if (existing is null)
        {
            await _readStateRepository.UpsertAsync(
                ChatReadState.Create(chatId, request.ActorUserId, messageId, now),
                ct);
        }
        else
        {
            existing.AdvanceTo(messageId, now);
            await _readStateRepository.UpsertAsync(existing, ct);
        }

        if (message.SenderId != request.ActorUserId)
        {
            message.MarkRead(now);
            await _messageRepository.UpdateAsync(message, ct);
        }

        if (_options.RealtimeEnabled)
        {
            await _realtime.NotifyMessageReadAsync(chatId.Value, request.ActorUserId, messageId.Value, ct);
        }

        return Result<ChatMessageDto>.Success(message.ToDto());
    }
}
