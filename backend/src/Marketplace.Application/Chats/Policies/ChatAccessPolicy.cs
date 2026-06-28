using Marketplace.Domain.Chats.Enums;
using Marketplace.Domain.Chats.Repositories;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Application.Chats.Policies;

public sealed class ChatAccessPolicy
{
    private readonly IChatParticipantRepository _participants;
    private readonly IChatRepository _chats;

    public ChatAccessPolicy(IChatParticipantRepository participants, IChatRepository chats)
    {
        _participants = participants;
        _chats = chats;
    }

    public async Task<bool> CanAccessAsync(ChatId chatId, Guid userId, bool isPlatformStaff, CancellationToken ct = default)
    {
        if (await _participants.IsActiveParticipantAsync(chatId, userId, ct))
            return true;

        if (!isPlatformStaff)
            return false;

        var chat = await _chats.GetByIdAsync(chatId, ct);
        return chat?.Type == ChatType.Support;
    }
}
