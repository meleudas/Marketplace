namespace Marketplace.Application.Common.Ports;

public enum HttpIdempotencyBeginState
{
    Started = 0,
    InProgress = 1,
    Completed = 2,
    RequestMismatch = 3
}

public sealed record HttpIdempotencyStoredResponse(int StatusCode, string? ResponseBodyJson);

public sealed record HttpIdempotencyBeginResult(
    HttpIdempotencyBeginState State,
    HttpIdempotencyStoredResponse? StoredResponse);

public interface IHttpIdempotencyStore
{
    Task<HttpIdempotencyBeginResult> TryBeginAsync(
        string scope,
        string idempotencyKey,
        string requestHash,
        TimeSpan ttl,
        CancellationToken ct = default);

    Task CompleteAsync(
        string scope,
        string idempotencyKey,
        string requestHash,
        int statusCode,
        string? responseBodyJson,
        CancellationToken ct = default);
}
