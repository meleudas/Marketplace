using Marketplace.Application.Common.Ports;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class HttpIdempotencyStore : IHttpIdempotencyStore
{
    private readonly ApplicationDbContext _context;

    public HttpIdempotencyStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HttpIdempotencyBeginResult> TryBeginAsync(
        string scope,
        string idempotencyKey,
        string requestHash,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var row = await _context.HttpIdempotencyRequests
            .FirstOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == idempotencyKey, ct);

        if (row is null)
        {
            var created = new HttpIdempotencyRequestRecord
            {
                Scope = scope,
                IdempotencyKey = idempotencyKey,
                RequestHash = requestHash,
                Status = "in_progress",
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(ttl)
            };
            _context.HttpIdempotencyRequests.Add(created);
            await _context.SaveChangesAsync(ct);
            return new HttpIdempotencyBeginResult(HttpIdempotencyBeginState.Started, null);
        }

        if (row.ExpiresAtUtc <= now)
        {
            row.RequestHash = requestHash;
            row.Status = "in_progress";
            row.ResponseStatusCode = null;
            row.ResponseBodyJson = null;
            row.CreatedAtUtc = now;
            row.CompletedAtUtc = null;
            row.ExpiresAtUtc = now.Add(ttl);
            await _context.SaveChangesAsync(ct);
            return new HttpIdempotencyBeginResult(HttpIdempotencyBeginState.Started, null);
        }

        if (!string.Equals(row.RequestHash, requestHash, StringComparison.Ordinal))
            return new HttpIdempotencyBeginResult(HttpIdempotencyBeginState.RequestMismatch, null);

        if (string.Equals(row.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpIdempotencyBeginResult(
                HttpIdempotencyBeginState.Completed,
                new HttpIdempotencyStoredResponse(row.ResponseStatusCode ?? 200, row.ResponseBodyJson));
        }

        return new HttpIdempotencyBeginResult(HttpIdempotencyBeginState.InProgress, null);
    }

    public async Task CompleteAsync(
        string scope,
        string idempotencyKey,
        string requestHash,
        int statusCode,
        string? responseBodyJson,
        CancellationToken ct = default)
    {
        var row = await _context.HttpIdempotencyRequests
            .FirstOrDefaultAsync(x => x.Scope == scope && x.IdempotencyKey == idempotencyKey, ct);
        if (row is null)
            return;

        if (!string.Equals(row.RequestHash, requestHash, StringComparison.Ordinal))
            return;

        row.Status = "completed";
        row.ResponseStatusCode = statusCode;
        row.ResponseBodyJson = responseBodyJson;
        row.CompletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }
}
