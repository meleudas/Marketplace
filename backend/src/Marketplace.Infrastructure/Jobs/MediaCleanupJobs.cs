using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.External.Storage;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Marketplace.Infrastructure.Jobs;

public sealed class MediaCleanupJobs
{
    private readonly ApplicationDbContext _context;
    private readonly IObjectStorage _storage;
    private readonly StorageOptions _options;

    public MediaCleanupJobs(
        ApplicationDbContext context,
        IObjectStorage storage,
        IOptions<StorageOptions> options)
    {
        _context = context;
        _storage = storage;
        _options = options.Value;
    }

    public async Task CleanupOrphansAsync(CancellationToken ct = default)
    {
        var dbKeys = await _context.ProductImages
            .AsNoTracking()
            .Select(x => new { x.OriginalObjectKey, x.ImageObjectKey, x.ThumbnailObjectKey })
            .ToListAsync(ct);

        var keep = dbKeys
            .SelectMany(x => new[] { x.OriginalObjectKey, x.ImageObjectKey, x.ThumbnailObjectKey })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var all = await _storage.ListKeysAsync(_options.MediaPrefix, ct);
        foreach (var key in all)
        {
            if (keep.Contains(key))
                continue;
            try
            {
                await _storage.DeleteAsync(key, ct);
            }
            catch
            {
                // Best effort cleanup; transient errors are retried by scheduler.
            }
        }
    }
}
