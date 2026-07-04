using Marketplace.Application.Products.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace Marketplace.Infrastructure.External.Search;

public sealed class SearchIndexBootstrapSync
{
    private readonly IProductSearchIndexer _indexer;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<SearchIndexBootstrapSync> _logger;

    public SearchIndexBootstrapSync(
        IProductSearchIndexer indexer,
        IOptions<ElasticsearchOptions> options,
        ILogger<SearchIndexBootstrapSync> logger)
    {
        _indexer = indexer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SyncAfterDatabaseSeedAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled || !_options.SyncAfterSeed)
            return;

        const int maxAttempts = 12;
        var delay = TimeSpan.FromSeconds(3);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _indexer.FullReindexAsync(ct);
                _logger.LogInformation("Elasticsearch product index synchronized after database seed.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientStartupFailure(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Elasticsearch sync attempt {Attempt}/{MaxAttempts} failed; retrying in {DelaySeconds}s.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Elasticsearch product index sync failed after database seed. Catalog search will use DB fallback until reindex succeeds.");
                return;
            }
        }
    }

    private static bool IsTransientStartupFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException or SocketException or TaskCanceledException)
                return true;
        }

        return false;
    }
}
