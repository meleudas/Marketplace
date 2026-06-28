using Marketplace.Application.Chats.Policies;
using Marketplace.Domain.Common.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Marketplace.API.Hubs;

[Authorize(Roles = "User,Buyer,Moderator,Admin,Support")]
public sealed class ChatHub : Hub
{
    private readonly ChatAccessPolicy _accessPolicy;

    public ChatHub(ChatAccessPolicy accessPolicy)
    {
        _accessPolicy = accessPolicy;
    }

    public async Task JoinChat(Guid chatId)
    {
        if (!TryGetUserId(out var userId))
            throw new HubException("Unauthorized");

        var isStaff = Context.User?.IsInRole("Moderator") == true
            || Context.User?.IsInRole("Admin") == true
            || Context.User?.IsInRole("Support") == true;

        if (!await _accessPolicy.CanAccessAsync(ChatId.From(chatId), userId, isStaff))
            throw new HubException("Forbidden");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatId));
    }

    public Task LeaveChat(Guid chatId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(chatId));

    internal static string GroupName(Guid chatId) => $"chat:{chatId}";

    private bool TryGetUserId(out Guid userId)
    {
        userId = default;
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
