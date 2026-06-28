using Marketplace.Application.Common.Ports;

namespace Marketplace.Infrastructure.External.Storage;

public sealed class DisabledObjectStorage : IObjectStorage
{
    private static InvalidOperationException Disabled() => new("Storage is disabled");

    public Task EnsureBucketExistsAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default) => Task.FromException(Disabled());
    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) => Task.FromException<Stream>(Disabled());
    public Task DeleteAsync(string objectKey, CancellationToken ct = default) => Task.FromException(Disabled());
    public Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default) => Task.FromException<IReadOnlyList<string>>(Disabled());
    public string GetPublicUrl(string objectKey) => throw Disabled();
    public Task<string> GetPresignedGetUrlAsync(string objectKey, CancellationToken ct = default) => Task.FromException<string>(Disabled());
}
