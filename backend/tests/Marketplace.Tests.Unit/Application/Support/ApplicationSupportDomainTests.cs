using System.Security.Cryptography;
using System.Text;
using Marketplace.Application.Support.Options;
using Marketplace.Application.Support.Policies;
using Marketplace.Application.Support.Services;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Entities;
using Marketplace.Domain.Support.Enums;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Support")]
public sealed class ApplicationSupportDomainTests
{
    [Theory]
    [InlineData(SupportTicketStatus.Open, SupportTicketStatus.Assigned, true)]
    [InlineData(SupportTicketStatus.Open, SupportTicketStatus.Closed, true)]
    [InlineData(SupportTicketStatus.Closed, SupportTicketStatus.Open, false)]
    [InlineData(SupportTicketStatus.Resolved, SupportTicketStatus.Closed, true)]
    [InlineData(SupportTicketStatus.Resolved, SupportTicketStatus.Escalated, false)]
    public void SupportTicket_StateMachine_Allows_Expected_Transitions(
        SupportTicketStatus from,
        SupportTicketStatus to,
        bool expected)
    {
        var ticket = SupportTicket.Reconstitute(
            SupportTicketId.From(1),
            "SUP-TEST",
            Guid.NewGuid().ToString(),
            null,
            null,
            "subject",
            "message",
            from,
            SupportTicketPriority.Normal,
            null,
            null,
            DateTime.UtcNow,
            null,
            null,
            null,
            DateTime.UtcNow.AddHours(24),
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            null);

        Assert.Equal(expected, ticket.CanTransitionTo(to));
    }

    [Fact]
    public void SupportEscalationPolicy_Urgent_Escalates_After_Half_Sla()
    {
        var policy = new SupportEscalationPolicy(Options.Create(new SupportOptions { SlaHoursP1 = 4 }));
        var created = DateTime.UtcNow.AddHours(-3);
        Assert.True(policy.ShouldEscalate(SupportTicketPriority.Urgent, created, DateTime.UtcNow));
    }

    [Fact]
    public void HelpdeskWebhookSignatureValidator_Rejects_Invalid_Signature()
    {
        var validator = new HelpdeskWebhookSignatureValidator(Options.Create(new SupportOptions
        {
            WebhookSigningSecret = "secret"
        }));

        var payload = "{\"eventId\":\"1\"}";
        var key = Encoding.UTF8.GetBytes("secret");
        using var hmac = new HMACSHA256(key);
        var valid = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        Assert.True(validator.IsValid(payload, valid));
        Assert.False(validator.IsValid(payload, "deadbeef"));
    }

    [Fact]
    public void SupportExternalLink_Ignores_Out_Of_Order_Webhook()
    {
        var link = SupportExternalLink.CreatePending(SupportTicketId.From(1), "logging", DateTime.UtcNow);
        link.MarkSynced("ext-1", DateTime.UtcNow.AddMinutes(-5), 100, DateTime.UtcNow);

        Assert.False(link.ShouldApplyExternalUpdate(DateTime.UtcNow.AddMinutes(-10), 99));
        Assert.True(link.ShouldApplyExternalUpdate(DateTime.UtcNow, 101));
    }

    [Fact]
    public void HelpdeskStatusMapper_Maps_Common_Statuses()
    {
        Assert.Equal(SupportTicketStatus.Resolved, HelpdeskStatusMapper.Map("solved"));
        Assert.Equal(SupportTicketStatus.PendingCustomer, HelpdeskStatusMapper.Map("waiting_customer"));
        Assert.Null(HelpdeskStatusMapper.Map("unknown-status"));
    }
}
