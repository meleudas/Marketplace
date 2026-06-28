using Marketplace.Domain.Chats.Entities;
using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ChatParticipantRepository : IChatParticipantRepository
{
    private readonly ApplicationDbContext _context;

    public ChatParticipantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatParticipant?> GetAsync(ChatId chatId, Guid userId, CancellationToken ct = default)
    {
        var row = await _context.ChatParticipants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChatId == chatId.Value && x.UserId == userId, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<ChatParticipant>> ListByChatAsync(ChatId chatId, CancellationToken ct = default)
    {
        var rows = await _context.ChatParticipants.AsNoTracking()
            .Where(x => x.ChatId == chatId.Value)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ChatParticipant>> ListActiveByChatAsync(ChatId chatId, CancellationToken ct = default)
    {
        var rows = await _context.ChatParticipants.AsNoTracking()
            .Where(x => x.ChatId == chatId.Value && !x.IsDeleted && x.LeftAt == null)
            .ToListAsync(ct);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<bool> IsActiveParticipantAsync(ChatId chatId, Guid userId, CancellationToken ct = default) =>
        await _context.ChatParticipants.AsNoTracking()
            .AnyAsync(x => x.ChatId == chatId.Value && x.UserId == userId && !x.IsDeleted && x.LeftAt == null, ct);

    public async Task AddAsync(ChatParticipant participant, CancellationToken ct = default)
    {
        await _context.ChatParticipants.AddAsync(ToRecord(participant), ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IReadOnlyList<ChatParticipant> participants, CancellationToken ct = default)
    {
        await _context.ChatParticipants.AddRangeAsync(participants.Select(ToRecord), ct);
        await _context.SaveChangesAsync(ct);
    }

    private static ChatParticipant ToDomain(ChatParticipantRecord row) =>
        ChatParticipant.Reconstitute(
            ChatId.From(row.ChatId),
            row.UserId,
            (ChatParticipantRole)row.Role,
            row.CompanyId.HasValue ? CompanyId.From(row.CompanyId.Value) : null,
            row.JoinedAt,
            row.LeftAt,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ChatParticipantRecord ToRecord(ChatParticipant entity) =>
        new()
        {
            ChatId = entity.ChatId.Value,
            UserId = entity.UserId,
            Role = (short)entity.Role,
            CompanyId = entity.CompanyId?.Value,
            JoinedAt = entity.JoinedAt,
            LeftAt = entity.LeftAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
