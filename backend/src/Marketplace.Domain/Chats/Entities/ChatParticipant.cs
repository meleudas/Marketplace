using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Chats.Entities;

/// <summary>Учасник чату (PK chat_id + user_id у БД).</summary>
public sealed class ChatParticipant : Entity
{
    private ChatParticipant() { }

    public ChatId ChatId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public ChatParticipantRole Role { get; private set; }
    public CompanyId? CompanyId { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static ChatParticipant Reconstitute(
        ChatId chatId,
        Guid userId,
        ChatParticipantRole role,
        CompanyId? companyId,
        DateTime joinedAt,
        DateTime? leftAt,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            ChatId = chatId,
            UserId = userId,
            Role = role,
            CompanyId = companyId,
            JoinedAt = joinedAt,
            LeftAt = leftAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
