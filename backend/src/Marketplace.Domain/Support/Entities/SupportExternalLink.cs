using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Support.Enums;

namespace Marketplace.Domain.Support.Entities;

public sealed class SupportExternalLink : Entity
{
    private SupportExternalLink() { }

    public long Id { get; private set; }
    public SupportTicketId TicketId { get; private set; } = null!;
    public string Provider { get; private set; } = string.Empty;
    public string ExternalTicketId { get; private set; } = string.Empty;
    public SupportExternalSyncState SyncState { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }
    public DateTime? ExternalUpdatedAt { get; private set; }
    public long ExternalSequence { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static SupportExternalLink CreatePending(
        SupportTicketId ticketId,
        string provider,
        DateTime nowUtc) =>
        new()
        {
            TicketId = ticketId,
            Provider = provider,
            ExternalTicketId = string.Empty,
            SyncState = SupportExternalSyncState.Pending,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

    public void MarkSynced(string externalTicketId, DateTime? externalUpdatedAt, long externalSequence, DateTime nowUtc)
    {
        ExternalTicketId = externalTicketId;
        SyncState = SupportExternalSyncState.Synced;
        LastSyncedAt = nowUtc;
        ExternalUpdatedAt = externalUpdatedAt;
        ExternalSequence = externalSequence;
        UpdatedAt = nowUtc;
    }

    public void MarkFailed(DateTime nowUtc)
    {
        SyncState = SupportExternalSyncState.Failed;
        UpdatedAt = nowUtc;
    }

    public bool ShouldApplyExternalUpdate(DateTime? externalUpdatedAt, long externalSequence) =>
        externalSequence > ExternalSequence
        || (externalSequence == ExternalSequence && externalUpdatedAt > (ExternalUpdatedAt ?? DateTime.MinValue));

    public static SupportExternalLink Reconstitute(
        long id,
        SupportTicketId ticketId,
        string provider,
        string externalTicketId,
        SupportExternalSyncState syncState,
        DateTime? lastSyncedAt,
        DateTime? externalUpdatedAt,
        long externalSequence,
        DateTime createdAt,
        DateTime updatedAt) =>
        new()
        {
            Id = id,
            TicketId = ticketId,
            Provider = provider,
            ExternalTicketId = externalTicketId,
            SyncState = syncState,
            LastSyncedAt = lastSyncedAt,
            ExternalUpdatedAt = externalUpdatedAt,
            ExternalSequence = externalSequence,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}
