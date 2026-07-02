namespace Marketplace.Infrastructure.Persistence.Seeders;

public interface IDbSeeder
{
    Task SeedAsync(ApplicationDbContext context, IServiceProvider serviceProvider, CancellationToken ct = default);
}
