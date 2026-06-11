using Marketplace.Domain.Support.Enums;

namespace Marketplace.Application.Support.Services;

public static class HelpdeskStatusMapper
{
    public static SupportTicketStatus? Map(string? externalStatus)
    {
        if (string.IsNullOrWhiteSpace(externalStatus))
            return null;

        return externalStatus.Trim().ToLowerInvariant() switch
        {
            "open" or "new" => SupportTicketStatus.Open,
            "assigned" or "in_progress" or "inprogress" => SupportTicketStatus.Assigned,
            "pending" or "pending_customer" or "waiting_customer" => SupportTicketStatus.PendingCustomer,
            "resolved" or "solved" => SupportTicketStatus.Resolved,
            "closed" => SupportTicketStatus.Closed,
            "escalated" => SupportTicketStatus.Escalated,
            _ => null
        };
    }
}
