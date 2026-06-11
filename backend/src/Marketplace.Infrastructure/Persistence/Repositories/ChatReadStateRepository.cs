using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ChatReadStateRepository : IChatReadStateRepository
{
    private readonly ApplicationDbContext _context;

    public ChatReadStateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatReadState?> GetAsync(ChatId chatId, Guid userId, CancellationToken ct = default)
    {
        var row = await _context.ChatReadStates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChatId == chatId.Value && x.UserId == userId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task UpsertAsync(ChatReadState state, CancellationToken ct = default)
    {
        var row = await _context.ChatReadStates
            .FirstOrDefaultAsync(x => x.ChatId == state.ChatId.Value && x.UserId == state.UserId, ct);

        if (row is null)
        {
            await _context.ChatReadStates.AddAsync(ToRecord(state), ct);
        }
        else if (state.LastReadMessageId.Value > row.LastReadMessageId)
        {
            row.LastReadMessageId = state.LastReadMessageId.Value;
            row.UpdatedAt = state.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static ChatReadState ToDomain(ChatReadStateRecord row) =>
        ChatReadState.Reconstitute(
            ChatId.From(row.ChatId),
            row.UserId,
            MessageId.From(row.LastReadMessageId),
            row.UpdatedAt);

    private static ChatReadStateRecord ToRecord(ChatReadState entity) =>
        new()
        {
            ChatId = entity.ChatId.Value,
            UserId = entity.UserId,
            LastReadMessageId = entity.LastReadMessageId.Value,
            UpdatedAt = entity.UpdatedAt
        };
}
