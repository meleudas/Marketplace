using Marketplace.Application.Reports.DTOs;
using Marketplace.Domain.Reports.Entities;

namespace Marketplace.Application.Reports;

internal static class ReportsMappers
{
    public static ReportDto ToDto(this Report entity) =>
        new(
            entity.Id.Value,
            entity.ReporterUserId,
            (short)entity.TargetType,
            entity.TargetId,
            (short)entity.Reason,
            entity.Description,
            (short)entity.Status,
            (short)entity.Priority,
            entity.AssignedModeratorId,
            entity.ReviewedById,
            entity.Resolution,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.ClosedAt);
}
