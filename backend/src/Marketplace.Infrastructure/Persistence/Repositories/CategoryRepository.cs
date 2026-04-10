using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context) => _context = context;

    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default)
    {
        var row = await _context.Categories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row == null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _context.Categories
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct = default)
    {
        var rows = await _context.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
    {
        var row = ToRecord(category);
        await _context.Categories.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        var row = await _context.Categories.FirstOrDefaultAsync(x => x.Id == category.Id.Value, ct)
            ?? throw new InvalidOperationException($"Category '{category.Id.Value}' was not found.");

        MapToRecord(category, row);
        await _context.SaveChangesAsync(ct);
    }

    private static Category ToDomain(CategoryRecord r)
    {
        return Category.Reconstitute(
            CategoryId.From(r.Id),
            r.Name,
            r.Slug,
            r.ImageUrl,
            r.ParentId.HasValue ? CategoryId.From(r.ParentId.Value) : null,
            r.Description,
            new JsonBlob(r.MetaRaw),
            r.SortOrder,
            r.IsActive,
            0,
            r.CreatedAt,
            r.UpdatedAt,
            r.IsDeleted,
            r.DeletedAt);
    }

    private static CategoryRecord ToRecord(Category category) =>
        new()
        {
            Id = category.Id.Value,
            Name = category.Name,
            Slug = category.Slug,
            ImageUrl = category.ImageUrl,
            ParentId = category.ParentId?.Value,
            Description = category.Description,
            MetaRaw = category.Meta.Raw,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            IsDeleted = category.IsDeleted,
            DeletedAt = category.DeletedAt
        };

    private static void MapToRecord(Category category, CategoryRecord row)
    {
        row.Name = category.Name;
        row.Slug = category.Slug;
        row.ImageUrl = category.ImageUrl;
        row.ParentId = category.ParentId?.Value;
        row.Description = category.Description;
        row.MetaRaw = category.Meta.Raw;
        row.SortOrder = category.SortOrder;
        row.IsActive = category.IsActive;
        row.UpdatedAt = category.UpdatedAt;
        row.IsDeleted = category.IsDeleted;
        row.DeletedAt = category.DeletedAt;
    }
}
