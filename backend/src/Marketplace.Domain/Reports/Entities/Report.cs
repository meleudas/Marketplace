using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;
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
    public ReportPriority Priority { get; private set; }

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
            Priority = priority,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
