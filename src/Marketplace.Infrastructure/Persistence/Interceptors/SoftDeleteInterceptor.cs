using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Marketplace.Infrastructure.Persistence.Interceptors;

/// <summary>Перетворює фізичне видалення на soft delete для підтримуваних сутностей.</summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplySoftDelete(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted)
                continue;

            switch (entry.Entity)
            {
                case ApplicationUser user:
                    entry.State = EntityState.Modified;
                    user.IsDeleted = true;
                    break;
                case MarketplaceUserRecord row:
                    entry.State = EntityState.Modified;
                    row.IsDeleted = true;
                    row.DeletedAt ??= DateTime.UtcNow;
                    break;
            }
        }
    }
}
