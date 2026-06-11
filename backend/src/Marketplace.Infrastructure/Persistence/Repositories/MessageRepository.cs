using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly ApplicationDbContext _context;

    public MessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct = default)
    {
        var row = await _context.ChatMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Message>> ListByChatAsync(ChatId chatId, int skip, int take, CancellationToken ct = default)
    {
        var rows = await _context.ChatMessages.AsNoTracking()
            .Where(x => x.ChatId == chatId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);
        rows.Reverse();
        return rows.Select(ToDomain).ToList();
    }

    public Task<int> CountByChatAsync(ChatId chatId, CancellationToken ct = default) =>
        _context.ChatMessages.AsNoTracking().CountAsync(x => x.ChatId == chatId.Value, ct);

    public Task<int> CountRecentBySenderAsync(ChatId chatId, Guid senderId, DateTime sinceUtc, CancellationToken ct = default) =>
        _context.ChatMessages.AsNoTracking().CountAsync(
            x => x.ChatId == chatId.Value && x.SenderId == senderId && x.CreatedAt >= sinceUtc,
            ct);

    public async Task<Message?> FindRecentDuplicateAsync(
        ChatId chatId,
        Guid senderId,
        string normalizedText,
        DateTime sinceUtc,
        CancellationToken ct = default)
    {
        var rows = await _context.ChatMessages.AsNoTracking()
            .Where(x => x.ChatId == chatId.Value
                && x.SenderId == senderId
                && x.CreatedAt >= sinceUtc)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        return rows
            .Select(ToDomain)
            .FirstOrDefault(x => string.Equals(x.Text.Trim(), normalizedText, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CountUnreadForUserAsync(
        ChatId chatId,
        Guid userId,
        MessageId? afterMessageId,
        CancellationToken ct = default)
    {
        var lastReadId = afterMessageId?.Value ?? 0L;
        return await _context.ChatMessages.AsNoTracking().CountAsync(
            x => x.ChatId == chatId.Value
                && x.SenderId != userId
                && x.Id > lastReadId
                && x.Status != (short)MessageStatus.DeletedForPolicy,
            ct);
    }

    public async Task<Message> AddAsync(Message entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.ChatMessages.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Message entity, CancellationToken ct = default)
    {
        var row = await _context.ChatMessages.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Message '{entity.Id.Value}' was not found.");

        row.Text = entity.Text;
        row.Status = (short)entity.Status;
        row.ReadAt = entity.ReadAt;
        row.DeletedBy = entity.DeletedBy.Raw ?? "{}";
        row.UpdatedAt = entity.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    private static Message ToDomain(ChatMessageRecord row) =>
        Message.Reconstitute(
            MessageId.From(row.Id),
            ChatId.From(row.ChatId),
            row.SenderId,
            row.Text,
            new JsonBlob(row.Attachments),
            (MessageStatus)row.Status,
            row.ReadAt,
            new JsonBlob(row.DeletedBy),
            row.ReplyToMessageId.HasValue ? MessageId.From(row.ReplyToMessageId.Value) : null,
            row.RawPayload,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ChatMessageRecord ToRecord(Message entity) =>
        new()
        {
            Id = entity.Id.Value,
            ChatId = entity.ChatId.Value,
            SenderId = entity.SenderId,
            Text = entity.Text,
            Attachments = entity.Attachments.Raw ?? "[]",
            Status = (short)entity.Status,
            ReadAt = entity.ReadAt,
            DeletedBy = entity.DeletedBy.Raw ?? "{}",
            ReplyToMessageId = entity.ReplyToMessageId?.Value,
            RawPayload = entity.RawPayload,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
