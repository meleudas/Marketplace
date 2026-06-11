using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Application.Support.Policies;

public sealed class SupportTicketStatePolicy
{
    public Result ValidateTransition(SupportTicket ticket, SupportTicketStatus next) =>
        ticket.CanTransitionTo(next)
            ? Result.Success()
            : Result.Failure("conflict: invalid ticket status transition");
}
