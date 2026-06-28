namespace Marketplace.Application.Reports.DTOs;

public sealed record ReportDto(
    long Id,
    string ReporterUserId,
    short TargetType,
    string TargetId,
    short Reason,
    string Description,
    short Status,
    short Priority,
    string? AssignedModeratorId,
    string? ReviewedById,
    string? Resolution,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt);

public sealed record ReportActionDto(
    long Id,
    long ReportId,
    short ActionType,
    string ActorUserId,
    string Reason,
    DateTime CreatedAt);
