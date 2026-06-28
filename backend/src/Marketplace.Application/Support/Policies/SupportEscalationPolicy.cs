using Marketplace.Application.Support.Options;
using Marketplace.Domain.Support.Enums;
using Microsoft.Extensions.Options;

namespace Marketplace.Application.Support.Policies;

public sealed class SupportEscalationPolicy
{
    private readonly SupportOptions _options;

    public SupportEscalationPolicy(IOptions<SupportOptions> options) => _options = options.Value;

    public bool ShouldEscalate(SupportTicketPriority priority, DateTime createdAtUtc, DateTime nowUtc)
    {
        var ageHours = (nowUtc - createdAtUtc).TotalHours;
        return priority switch
        {
            SupportTicketPriority.Urgent => ageHours >= _options.SlaHoursP1 / 2.0,
            SupportTicketPriority.High => ageHours >= _options.SlaHoursP2 / 2.0,
            _ => false
        };
    }

    public DateTime ComputeSlaDueAt(SupportTicketPriority priority, DateTime nowUtc) =>
        priority switch
        {
            SupportTicketPriority.Urgent => nowUtc.AddHours(Math.Max(1, _options.SlaHoursP1)),
            SupportTicketPriority.High => nowUtc.AddHours(Math.Max(1, _options.SlaHoursP2)),
            _ => nowUtc.AddHours(Math.Max(1, _options.SlaHoursP2) * 2)
        };
}
