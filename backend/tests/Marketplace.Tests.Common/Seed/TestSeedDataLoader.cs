using Npgsql;

namespace Marketplace.Tests.Common.Seed;

public static class TestSeedDataLoader
{
    public static async Task ApplyAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var scriptPath = ResolveScriptPath();
        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        sql = FilterPsqlDirectives(sql);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static string ResolveScriptPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Seed", "seed-test-data.sql"),
            Path.Combine(AppContext.BaseDirectory, "seed-test-data.sql"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var fromRepo = Path.Combine(dir.FullName, "backend", "scripts", "seed-test-data.sql");
            if (File.Exists(fromRepo))
                return fromRepo;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("seed-test-data.sql was not found for test seeding.");
    }

    internal static string FilterPsqlDirectives(string sql)
    {
        var lines = sql.Split('\n');
        var filtered = lines
            .Where(line => !line.TrimStart().StartsWith('\\'))
            .ToArray();
        return string.Join('\n', filtered);
    }
}
