namespace Marketplace.Domain.Common.Models;

/// <summary>
/// Агрегат з полями аудиту та soft delete, узгодженими з глобальним правилом схеми БД.
/// </summary>
public abstract class AuditableSoftDeleteAggregateRoot<TId> : AggregateRoot<TId>
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

    protected void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    protected void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }
}
