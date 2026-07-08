using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class BookCategorySeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Categories.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var categories = BookCatalogCategorySeedData.All
            .Select(def => new CategoryRecord
            {
                Id = def.Id,
                Name = def.Name,
                Slug = def.Slug,
                ParentId = def.ParentId,
                Description = def.Description,
                MetaRaw = def.MetaRaw,
                SortOrder = def.SortOrder,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync(ct);
    }
}
