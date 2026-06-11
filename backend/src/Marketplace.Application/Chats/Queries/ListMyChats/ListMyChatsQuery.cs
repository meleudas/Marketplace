using FluentValidation;
using Marketplace.Application.Chats;
using Marketplace.Application.Chats.DTOs;
using Marketplace.Application.Chats.Options;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Chats.Queries.ListMyChats;

public sealed record ListMyChatsQuery(Guid ActorUserId, int Page, int Size) : IRequest<Result<ChatListDto>>;

public sealed class ListMyChatsQueryValidator : AbstractValidator<ListMyChatsQuery>
{
    public ListMyChatsQueryValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}

public sealed class ListMyChatsQueryHandler : IRequestHandler<ListMyChatsQuery, Result<ChatListDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IChatReadStateRepository _readStateRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ChatsOptions _options;

    public ListMyChatsQueryHandler(
        IChatRepository chatRepository,
        IChatReadStateRepository readStateRepository,
        IMessageRepository messageRepository,
        IOptions<ChatsOptions> options)
    {
        _chatRepository = chatRepository;
        _readStateRepository = readStateRepository;
        _messageRepository = messageRepository;
        _options = options.Value;
    }

    public async Task<Result<ChatListDto>> Handle(ListMyChatsQuery request, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Result<ChatListDto>.Failure("conflict: chats are disabled");

        var skip = (request.Page - 1) * request.Size;
        var total = await _chatRepository.CountForParticipantAsync(request.ActorUserId, ct);
        var chats = await _chatRepository.ListForParticipantAsync(request.ActorUserId, skip, request.Size, ct);

        var items = new List<ChatDto>(chats.Count);
        foreach (var chat in chats)
        {
            var readState = await _readStateRepository.GetAsync(chat.Id, request.ActorUserId, ct);
            var unread = await _messageRepository.CountUnreadForUserAsync(
                chat.Id,
                request.ActorUserId,
                readState?.LastReadMessageId,
                ct);
            items.Add(chat.ToDto(unread));
        }

        return Result<ChatListDto>.Success(new ChatListDto(items, total, request.Page, request.Size));
    }
}
