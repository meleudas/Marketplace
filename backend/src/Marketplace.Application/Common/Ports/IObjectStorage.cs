namespace Marketplace.Application.Common.Ports;

public interface IObjectStorage
{
    Task EnsureBucketExistsAsync(CancellationToken ct = default);
    Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken ct = default);
    string GetPublicUrl(string objectKey);
    Task<string> GetPresignedGetUrlAsync(string objectKey, CancellationToken ct = default);
}
