using System.Text.Json;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class CompanyMemberSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.CompanyMembers.AnyAsync(ct))
            return;

        var sellers = await context.MarketplaceUsers.Where(u => u.Role == 2).ToListAsync(ct);
        var companies = await context.Companies.ToListAsync(ct);
        var now = DateTime.UtcNow;

        var members = companies.Select((c, i) => new CompanyMemberRecord
        {
            CompanyId = c.Id,
            UserId = sellers[i % sellers.Count].Id,
            IsOwner = i < sellers.Count,
            Role = i < sellers.Count ? (short)1 : (short)2,
            PermissionsRaw = "{\"products\": {\"create\": true, \"edit\": true, \"delete\": false}, \"orders\": {\"view\": true}}",
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        context.CompanyMembers.AddRange(members);
        await context.SaveChangesAsync(ct);
    }
}
