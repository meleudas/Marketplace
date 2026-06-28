using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ChatModerationActionRepository : IChatModerationActionRepository
{
    private readonly ApplicationDbContext _context;

    public ChatModerationActionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatModerationAction> AppendAsync(ChatModerationAction action, CancellationToken ct = default)
    {
        var row = new ChatModerationActionRecord
        {
            ChatId = action.ChatId.Value,
            MessageId = action.MessageId?.Value,
            ActionType = (short)action.ActionType,
            ModeratorUserId = action.ModeratorUserId,
            Reason = action.Reason,
            CreatedAt = action.CreatedAt
        };
        await _context.ChatModerationActions.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);

        return ChatModerationAction.Reconstitute(
            row.Id,
            ChatId.From(row.ChatId),
            row.MessageId.HasValue ? MessageId.From(row.MessageId.Value) : null,
            (ChatModerationActionType)row.ActionType,
            row.ModeratorUserId,
            row.Reason,
            row.CreatedAt);
    }

    public async Task<IReadOnlyList<ChatModerationAction>> ListByChatAsync(ChatId chatId, int limit, CancellationToken ct = default)
    {
        var rows = await _context.ChatModerationActions.AsNoTracking()
            .Where(x => x.ChatId == chatId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);

        return rows.Select(row => ChatModerationAction.Reconstitute(
            row.Id,
            ChatId.From(row.ChatId),
            row.MessageId.HasValue ? MessageId.From(row.MessageId.Value) : null,
            (ChatModerationActionType)row.ActionType,
            row.ModeratorUserId,
            row.Reason,
            row.CreatedAt)).ToList();
    }
}
