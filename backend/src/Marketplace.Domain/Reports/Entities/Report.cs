using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shared.Kernel;
using Marketplace.Domain.Reports.Enums;

namespace Marketplace.Domain.Reports.Entities;

public sealed class Report : AuditableSoftDeleteAggregateRoot<ReportId>
{
    private Report() { }

    public string ReporterUserId { get; private set; } = string.Empty;
    public ReportTargetType TargetType { get; private set; }
    public string TargetId { get; private set; } = string.Empty;
    public ReportReason Reason { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public JsonBlob Images { get; private set; } = JsonBlob.Empty;
    public ReportStatus Status { get; private set; }
    public string? ReviewedById { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? Resolution { get; private set; }
    public string? AssignedModeratorId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public string? ClosedById { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? LastActionReason { get; private set; }
    public ReportPriority Priority { get; private set; }

    public static Report Create(
        string reporterUserId,
        ReportTargetType targetType,
        string targetId,
        ReportReason reason,
        string description,
        JsonBlob images,
        ReportPriority priority,
        DateTime nowUtc)
    {
        return new Report
        {
            Id = ReportId.From(0),
            ReporterUserId = reporterUserId,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            Description = description,
            Images = images,
            Status = ReportStatus.New,
            Priority = priority,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
    }

    public bool CanTransitionTo(ReportStatus next)
    {
        if (Status == ReportStatus.Closed)
            return false;

        return (Status, next) switch
        {
            (ReportStatus.New, ReportStatus.InReview) => true,
            (ReportStatus.New, ReportStatus.Escalated) => true,
            (ReportStatus.InReview, ReportStatus.Actioned) => true,
            (ReportStatus.InReview, ReportStatus.Rejected) => true,
            (ReportStatus.InReview, ReportStatus.Escalated) => true,
            (ReportStatus.Actioned, ReportStatus.Closed) => true,
            (ReportStatus.Rejected, ReportStatus.Closed) => true,
            (ReportStatus.Escalated, ReportStatus.InReview) => true,
            (ReportStatus.Escalated, ReportStatus.Closed) => true,
            _ => false
        };
    }

    public Result StartReview(string moderatorId, string reason, DateTime nowUtc)
    {
        var guard = GuardModeratorAction(moderatorId, reason);
        if (guard.IsFailure)
            return guard;
        if (!CanTransitionTo(ReportStatus.InReview))
            return Result.Failure("unprocessable: invalid transition to in_review");

        ReviewedById = moderatorId;
        ReviewedAt = nowUtc;
        LastActionReason = reason;
        Status = ReportStatus.InReview;
        UpdatedAt = nowUtc;
        return Result.Success();
    }

    public Result Assign(string moderatorId, string actorId, string reason, DateTime nowUtc)
    {
        var guard = GuardModeratorAction(actorId, reason);
        if (guard.IsFailure)
            return guard;

        AssignedModeratorId = moderatorId;
        AssignedAt = nowUtc;
        LastActionReason = reason;
        if (Status == ReportStatus.New && CanTransitionTo(ReportStatus.InReview))
            Status = ReportStatus.InReview;
        UpdatedAt = nowUtc;
        return Result.Success();
    }

    public Result Resolve(string actorId, string resolution, DateTime nowUtc)
    {
        var guard = GuardModeratorAction(actorId, resolution);
        if (guard.IsFailure)
            return guard;
        if (!CanTransitionTo(ReportStatus.Actioned))
            return Result.Failure("unprocessable: invalid transition to actioned");

        Resolution = resolution;
        ReviewedById = actorId;
        ReviewedAt = nowUtc;
        LastActionReason = resolution;
        Status = ReportStatus.Actioned;
        UpdatedAt = nowUtc;
        return Result.Success();
    }

    public Result Reject(string actorId, string reason, DateTime nowUtc)
    {
        var guard = GuardModeratorAction(actorId, reason);
        if (guard.IsFailure)
            return guard;
        if (!CanTransitionTo(ReportStatus.Rejected))
            return Result.Failure("unprocessable: invalid transition to rejected");

        Resolution = reason;
        ReviewedById = actorId;
        ReviewedAt = nowUtc;
        LastActionReason = reason;
        Status = ReportStatus.Rejected;
        UpdatedAt = nowUtc;
        return Result.Success();
    }

    public Result Escalate(string actorId, string reason, DateTime nowUtc)
    {
        var guard = GuardModeratorAction(actorId, reason);
        if (guard.IsFailure)
            return guard;
        if (!CanTransitionTo(ReportStatus.Escalated))
            return Result.Failure("unprocessable: invalid transition to escalated");

        ReviewedById = actorId;
        ReviewedAt = nowUtc;
        LastActionReason = reason;
        Status = ReportStatus.Escalated;
        UpdatedAt = nowUtc;
        return Result.Success();
    }

    public Result Close(string actorId, string reason, DateTime nowUtc)
    {
        var guard = GuardModeratorAction(actorId, reason);
        if (guard.IsFailure)
            return guard;
        if (Status is not (ReportStatus.Actioned or ReportStatus.Rejected or ReportStatus.Escalated))
            return Result.Failure("unprocessable: invalid transition to closed");

        ClosedById = actorId;
        ClosedAt = nowUtc;
        LastActionReason = reason;
        Status = ReportStatus.Closed;
        UpdatedAt = nowUtc;
        return Result.Success();
    }

    private Result GuardModeratorAction(string actorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            return Result.Failure("forbidden: actor is required");
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("unprocessable: reason is required");
        if (string.Equals(actorId, ReporterUserId, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("forbidden: reporter cannot moderate own report");
        return Result.Success();
    }

    public static Report Reconstitute(
        ReportId id,
        string reporterUserId,
        ReportTargetType targetType,
        string targetId,
        ReportReason reason,
        string description,
        JsonBlob images,
        ReportStatus status,
        string? reviewedById,
        DateTime? reviewedAt,
        string? resolution,
        string? assignedModeratorId,
        DateTime? assignedAt,
        string? closedById,
        DateTime? closedAt,
        string? lastActionReason,
        ReportPriority priority,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            ReporterUserId = reporterUserId,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            Description = description,
            Images = images,
            Status = status,
            ReviewedById = reviewedById,
            ReviewedAt = reviewedAt,
            Resolution = resolution,
            AssignedModeratorId = assignedModeratorId,
            AssignedAt = assignedAt,
            ClosedById = closedById,
            ClosedAt = closedAt,
            LastActionReason = lastActionReason,
            Priority = priority,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
