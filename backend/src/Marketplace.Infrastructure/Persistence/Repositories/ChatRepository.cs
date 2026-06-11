using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct = default)
    {
        var row = await _context.Chats.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public Task<Chat?> FindActiveDirectAsync(ProductId productId, Guid buyerId, CancellationToken ct = default) =>
        FindActiveAsync(ChatType.Direct, buyerId, productId: productId, orderId: null, ct);

    public Task<Chat?> FindActiveOrderRelatedAsync(OrderId orderId, Guid buyerId, CancellationToken ct = default) =>
        FindActiveAsync(ChatType.OrderRelated, buyerId, productId: null, orderId: orderId, ct);

    public Task<Chat?> FindActiveSupportAsync(Guid buyerId, CancellationToken ct = default) =>
        FindActiveAsync(ChatType.Support, buyerId, productId: null, orderId: null, ct);

    public async Task<IReadOnlyList<Chat>> ListForParticipantAsync(Guid userId, int skip, int take, CancellationToken ct = default)
    {
        var chatIds = _context.ChatParticipants.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ChatId);

        var rows = await _context.Chats.AsNoTracking()
            .Where(x => chatIds.Contains(x.Id))
            .OrderByDescending(x => x.LastMessageCreatedAt ?? x.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<int> CountForParticipantAsync(Guid userId, CancellationToken ct = default)
    {
        var chatIds = _context.ChatParticipants.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ChatId);

        return await _context.Chats.AsNoTracking().CountAsync(x => chatIds.Contains(x.Id), ct);
    }

    public async Task<Chat> AddAsync(Chat entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.Chats.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Chat entity, CancellationToken ct = default)
    {
        var row = await _context.Chats.FirstOrDefaultAsync(x => x.Id == entity.Id.Value, ct)
            ?? throw new InvalidOperationException($"Chat '{entity.Id.Value}' was not found.");

        row.Status = (short)entity.Status;
        row.IsActive = entity.IsActive;
        row.LastMessageText = entity.LastMessageText;
        row.LastMessageSenderId = entity.LastMessageSenderId;
        row.LastMessageCreatedAt = entity.LastMessageCreatedAt;
        row.Meta = entity.Meta.Raw ?? "{}";
        row.UpdatedAt = entity.UpdatedAt;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<Chat?> FindActiveAsync(
        ChatType type,
        Guid buyerId,
        ProductId? productId,
        OrderId? orderId,
        CancellationToken ct)
    {
        var query = _context.Chats.AsNoTracking()
            .Where(x => x.Type == (short)type
                && x.InitiatorUserId == buyerId
                && x.Status == (short)ChatStatus.Active);

        if (productId is not null)
            query = query.Where(x => x.ProductId == productId.Value);
        if (orderId is not null)
            query = query.Where(x => x.OrderId == orderId.Value);

        var row = await query.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        return row is null ? null : ToDomain(row);
    }

    private static Chat ToDomain(ChatRecord row) =>
        Chat.Reconstitute(
            ChatId.From(row.Id),
            (ChatType)row.Type,
            (ChatStatus)row.Status,
            row.InitiatorUserId,
            row.OrderId.HasValue ? OrderId.From(row.OrderId.Value) : null,
            row.ProductId.HasValue ? ProductId.From(row.ProductId.Value) : null,
            row.LastMessageText,
            row.LastMessageSenderId,
            row.LastMessageCreatedAt,
            new JsonBlob(row.Meta),
            string.IsNullOrWhiteSpace(row.ParticipantsSnapshot) ? null : new JsonBlob(row.ParticipantsSnapshot),
            row.RawPayload,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ChatRecord ToRecord(Chat entity) =>
        new()
        {
            Id = entity.Id.Value,
            Type = (short)entity.Type,
            Status = (short)entity.Status,
            InitiatorUserId = entity.InitiatorUserId,
            OrderId = entity.OrderId?.Value,
            ProductId = entity.ProductId?.Value,
            LastMessageText = entity.LastMessageText,
            LastMessageSenderId = entity.LastMessageSenderId,
            LastMessageCreatedAt = entity.LastMessageCreatedAt,
            IsActive = entity.IsActive,
            Meta = entity.Meta.Raw ?? "{}",
            ParticipantsSnapshot = entity.ParticipantsSnapshot?.Raw,
            RawPayload = entity.RawPayload,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
