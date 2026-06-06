using Marketplace.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests.Common.Fixtures;

public static class SqliteDbFixture
{
    public static async Task<ApplicationDbContext> CreateContextAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        return context;
    }
}
